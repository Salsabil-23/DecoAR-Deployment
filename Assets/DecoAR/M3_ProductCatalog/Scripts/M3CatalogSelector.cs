using System.Collections.Generic;
using DecoAR.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DecoAR.M3
{
    // Ecran du rayon M3 : zones, etagere, liste des produits, fiche produit (F3.5),
    // variantes (F3.4), "Voir en AR" / "Ajouter au panier" (F3.6), recherche vocale et texte (F3.7, F3.9),
    // secousse pour reposer (F3.8) et vibrations legeres (F3.10).
    public class M3CatalogSelector : MonoBehaviour
    {
        [Header("Donnees et 3D")]
        [SerializeField] private M3ProductCatalog catalog;
        [SerializeField] private M3Shelf shelf;
        [SerializeField] private M3Inspector inspector;
        [SerializeField] private M3ShakeDetector shake;
        [SerializeField] private M3VoiceSearch voice;

        [Header("Liste des produits")]
        [SerializeField] private Transform productRoot;
        [SerializeField] private GameObject productButtonPrefab;
        [SerializeField] private TMP_Text selectedLabel;

        [Header("Zones")]
        [SerializeField] private Button[] zoneButtons;     // dans l'ordre de M3Categories.All
        [SerializeField] private TMP_Text zoneTitle;

        [Header("Fiche produit")]
        [SerializeField] private GameObject sheetPanel;
        [SerializeField] private TMP_Text sheetName;
        [SerializeField] private TMP_Text sheetPrice;
        [SerializeField] private TMP_Text sheetDimensions;
        [SerializeField] private TMP_Text sheetDescription;
        [SerializeField] private Transform variantRoot;
        [SerializeField] private GameObject variantButtonPrefab;
        [SerializeField] private Button viewInARButton;
        [SerializeField] private Button addToCartButton;
        [SerializeField] private Button putBackButton;

        [Header("Recherche")]
        [SerializeField] private TMP_InputField searchField;
        [SerializeField] private Button searchButton;
        [SerializeField] private Button micButton;
        [SerializeField] private TMP_Text micLabel;
        [SerializeField] private TMP_Text heardText;

        [Header("Navigation et retours")]
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text cartLabel;
        [SerializeField] private TMP_Text toastText;

        private static readonly Color ZoneOn = new Color(0.80f, 0.63f, 0.30f);
        private static readonly Color ZoneOff = new Color(0.98f, 0.95f, 0.89f);
        private static readonly Color VariantOn = new Color(0.80f, 0.63f, 0.30f);

        private const string MicExplainedKey = "DecoAR.M3.MicExplained";

        private readonly List<GameObject> spawnedButtons = new List<GameObject>();
        private readonly List<Button> variantButtons = new List<Button>();
        private string category;
        private ProductOption current;
        private int variantIndex;
        private float toastUntil;

        private void Start()
        {
            if (catalog == null)
            {
                Debug.LogError("[M3] M3ProductCatalog est manquant dans M3CatalogSelector.");
                return;
            }

            for (int i = 0; i < zoneButtons.Length && i < M3Categories.All.Length; i++)
            {
                string zone = M3Categories.All[i];
                zoneButtons[i].onClick.AddListener(() => ShowZone(zone));
            }

            if (viewInARButton != null) viewInARButton.onClick.AddListener(ViewInAR);
            if (addToCartButton != null) addToCartButton.onClick.AddListener(AddToCart);
            if (putBackButton != null) putBackButton.onClick.AddListener(PutBack);
            if (backButton != null) backButton.onClick.AddListener(BackToShop);
            if (searchButton != null) searchButton.onClick.AddListener(() => Search(searchField != null ? searchField.text : ""));
            if (searchField != null) searchField.onSubmit.AddListener(Search);
            if (micButton != null) micButton.onClick.AddListener(ToggleVoice);

            if (inspector != null)
            {
                inspector.ItemTapped += item => SelectProduct(item.Product);
                inspector.Returned += () => Debug.Log("[M3] Produit repose sur l'etagere");
            }
            if (shake != null) shake.Shaken += OnShake;
            if (voice != null)
            {
                voice.Partial += text => SetHeard($"« {text} »");
                voice.Result += OnVoiceResult;
                voice.Failed += OnVoiceFailed;
                voice.ListeningChanged += listening => { if (micLabel != null) micLabel.text = listening ? "Stop" : "Micro"; };
            }

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.CartCountChanged += RefreshCart;
                RefreshCart(gm.CartCount);
            }

            if (sheetPanel != null) sheetPanel.SetActive(false);
            SetHeard("");
            if (toastText != null) toastText.text = "";

            string start = gm != null && System.Array.IndexOf(M3Categories.All, gm.CurrentCategory) >= 0
                ? gm.CurrentCategory
                : M3Categories.All[0];
            ShowZone(start);

            // Retour depuis l'AR : on reprend le produit et la variante choisis
            if (gm != null && gm.HasSelectedProduct)
            {
                var previous = catalog.Get(gm.SelectedProductId);
                if (previous != null && previous.category == category)
                    SelectProduct(previous, gm.SelectedVariantIndex);
            }
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.CartCountChanged -= RefreshCart;
        }

        private void Update()
        {
            if (toastText != null && toastText.text != "" && Time.time > toastUntil) toastText.text = "";
        }

        // ------------------------------------------------------------ zones

        public void ShowZone(string zone)
        {
            if (current != null) CloseSheet(true);
            category = zone;
            var gm = GameManager.Instance;
            if (gm != null) gm.CurrentCategory = zone;

            var products = catalog.GetByCategory(zone);
            if (inspector != null) inspector.Release();
            if (shelf != null) shelf.Fill(products);
            BuildCatalog(products);

            if (zoneTitle != null) zoneTitle.text = zone;
            for (int i = 0; i < zoneButtons.Length && i < M3Categories.All.Length; i++)
            {
                var img = zoneButtons[i].GetComponent<Image>();
                if (img != null) img.color = M3Categories.All[i] == zone ? ZoneOn : ZoneOff;
            }
            if (selectedLabel != null) selectedLabel.text = "Touchez un produit pour l'inspecter";
            Debug.Log($"[M3] Zone {zone} : {products.Count} produits");
        }

        // Liste des produits sous l'etagere : alternative tactile au tap sur les modeles 3D
        private void BuildCatalog(List<ProductOption> products)
        {
            foreach (var button in spawnedButtons)
                if (button != null) Destroy(button);
            spawnedButtons.Clear();

            if (productRoot == null || productButtonPrefab == null)
            {
                Debug.LogError("[M3] Product Root ou Product Button Prefab manquant dans M3CatalogSelector.");
                return;
            }

            foreach (var product in products)
            {
                var button = Instantiate(productButtonPrefab, productRoot);
                button.name = $"ProductButton_{product.id}";
                spawnedButtons.Add(button);

                var text = button.GetComponentInChildren<TMP_Text>();
                if (text != null) text.text = product.displayName + "\n<b>" + product.PriceLabel + "</b>";

                var btn = button.GetComponent<Button>();
                if (btn != null)
                {
                    var p = product;
                    btn.onClick.AddListener(() => SelectProduct(p));
                }
            }
        }

        // ------------------------------------------------------------ selection

        public void SelectProduct(ProductOption product) => SelectProduct(product, 0);

        public void SelectProduct(ProductOption product, int variant)
        {
            if (product == null) return;
            if (product.category != category) ShowZone(product.category);

            Debug.Log($"[M3] Produit sélectionné: {product.displayName} | id={product.id}");
            current = product;
            variantIndex = Mathf.Clamp(variant, 0, Mathf.Max(0, product.variants.Count - 1));

            if (shelf != null)
            {
                shelf.Highlight(null);
                var item = shelf.Find(product);
                if (item != null && inspector != null) inspector.Inspect(item);
            }

            if (selectedLabel != null) selectedLabel.text = product.displayName + " • " + product.category;
            FillSheet(product);
            ApplyVariant(variantIndex, false);
            StoreSelection();
            HapticManager.Vibrate(HapticIntensity.Legere);
        }

        private void FillSheet(ProductOption product)
        {
            if (sheetPanel != null) sheetPanel.SetActive(true);
            if (sheetName != null) sheetName.text = product.displayName;
            if (sheetPrice != null) sheetPrice.text = product.PriceLabel;
            if (sheetDimensions != null) sheetDimensions.text = "Dimensions : " + product.DimensionsLabel
                + "   •   Pose : " + PlacementLabel(product.placement);
            if (sheetDescription != null) sheetDescription.text = product.description;

            foreach (var b in variantButtons)
                if (b != null) Destroy(b.gameObject);
            variantButtons.Clear();
            if (variantRoot == null || variantButtonPrefab == null) return;

            for (int i = 0; i < product.variants.Count; i++)
            {
                var v = product.variants[i];
                var go = Instantiate(variantButtonPrefab, variantRoot);
                go.name = "Variant_" + v.name;
                var label = go.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = v.name;
                var swatch = go.transform.Find("Swatch");
                if (swatch != null && swatch.TryGetComponent<Image>(out var swatchImage))
                {
                    swatchImage.color = v.color;
                    if (v.thumbnail != null) { swatchImage.sprite = v.thumbnail; swatchImage.color = Color.white; }
                }
                int index = i;
                var btn = go.GetComponent<Button>();
                btn.onClick.AddListener(() => ApplyVariant(index, true));
                variantButtons.Add(btn);
            }
        }

        private void ApplyVariant(int index, bool userChoice)
        {
            if (current == null) return;
            variantIndex = index;
            var variant = current.GetVariant(index);
            if (variant != null && inspector != null) inspector.ApplyVariant(variant);

            for (int i = 0; i < variantButtons.Count; i++)
            {
                var img = variantButtons[i].GetComponent<Image>();
                if (img != null) img.color = i == index ? VariantOn : Color.white;
            }

            if (userChoice)
            {
                StoreSelection();
                Toast($"Variante : {variant?.name}");
            }
        }

        // Transmet le produit et sa variante a M4 / M5 via le GameManager
        private void StoreSelection()
        {
            var gm = GameManager.Instance;
            if (gm == null || current == null)
            {
                Debug.LogWarning("[M3] GameManager absent : le produit n'est pas enregistré.");
                return;
            }
            gm.SelectProduct(current.id, current.prefab, current.sizeCm, current.placement, variantIndex);
        }

        private void PutBack() => CloseSheet(false);

        private void CloseSheet(bool silent)
        {
            if (inspector != null) inspector.ReturnCurrent();
            current = null;
            if (sheetPanel != null) sheetPanel.SetActive(false);
            if (selectedLabel != null) selectedLabel.text = "Touchez un produit pour l'inspecter";
            if (!silent) Toast("Produit reposé sur l'étagère");
        }

        private void OnShake()
        {
            if (current == null) return;
            CloseSheet(false);
        }

        // ------------------------------------------------------------ actions

        private void ViewInAR()
        {
            if (current == null) return;
            if (current.prefab == null)
            {
                Toast("Modèle 3D manquant pour ce produit");
                return;
            }
            StoreSelection();
            Debug.Log($"[M3] Voir en AR : {current.id} variante {variantIndex}");
            SceneManager.LoadScene(SceneNames.AR);
        }

        private void AddToCart()
        {
            if (current == null) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            var variant = current.GetVariant(variantIndex);
            gm.AddToCart(new CartItem
            {
                productId = current.id,
                name = current.displayName,
                variantName = variant != null ? variant.name : "",
                color = variant != null ? variant.color : Color.white,
                price = current.price,
                prefab = current.prefab,
            });
            HapticManager.Vibrate(HapticIntensity.Legere);
            Toast($"{current.displayName} ajouté au panier");
        }

        private void BackToShop()
        {
            if (inspector != null && inspector.Current != null) inspector.ReturnCurrent();
            SceneManager.LoadScene(SceneNames.Shop);
        }

        // ------------------------------------------------------------ recherche

        public void Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                if (shelf != null) shelf.Highlight(null);
                SetHeard("");
                return;
            }

            var results = catalog.Search(query);
            if (results.Count == 0)
            {
                if (shelf != null) shelf.Highlight(null);
                SetHeard($"« {query} » : aucun produit trouvé");
                return;
            }

            // Si rien ne correspond dans la zone ouverte, on va dans la zone du premier resultat
            bool inZone = results.Exists(p => p.category == category);
            if (!inZone)
            {
                if (current != null) CloseSheet(true);
                ShowZone(results[0].category);
            }

            int shown = shelf != null ? shelf.Highlight(results) : 0;
            SetHeard($"« {query} » : {results.Count} produit(s)" + (shown < results.Count ? $", {shown} dans cette zone" : ""));
            if (results.Count == 1) SelectProduct(results[0]);
        }

        private void ToggleVoice()
        {
            if (voice == null) return;
            if (voice.IsListening) { voice.StopListening(); return; }

            if (!M3VoiceSearch.IsAvailable)
            {
                OnVoiceFailed("Recherche vocale indisponible ici : tapez votre recherche.");
                return;
            }

            // Explication avant la demande d'autorisation systeme (charte 9.2)
            if (PlayerPrefs.GetInt(MicExplainedKey, 0) == 0)
            {
                PlayerPrefs.SetInt(MicExplainedKey, 1);
                PlayerPrefs.Save();
                SetHeard("Le micro sert uniquement à rechercher un produit par la voix. Touchez à nouveau « Micro » pour parler.");
                return;
            }
            SetHeard("Je vous écoute… dites un nom de produit");
            voice.StartListening();
        }

        private void OnVoiceResult(string text)
        {
            if (searchField != null) searchField.text = text;
            Search(text);
        }

        private void OnVoiceFailed(string message)
        {
            SetHeard(message);
            if (searchField != null) searchField.ActivateInputField();
        }

        // ------------------------------------------------------------ affichage

        private void SetHeard(string text)
        {
            if (heardText != null) heardText.text = text;
        }

        private void RefreshCart(int count)
        {
            if (cartLabel != null) cartLabel.text = $"Panier ({count})";
        }

        private void Toast(string message, float seconds = 2f)
        {
            if (toastText == null) return;
            toastText.text = message;
            toastUntil = Time.time + seconds;
        }

        private static string PlacementLabel(ProductPlacement placement)
        {
            switch (placement)
            {
                case ProductPlacement.Mur: return "mur";
                case ProductPlacement.Sol: return "sol";
                default: return "sol ou table";
            }
        }
    }
}
