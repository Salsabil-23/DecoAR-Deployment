using System.Collections.Generic;
using DecoAR.Core;
using DecoAR.M5;
using DecoAR.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DecoAR.M3
{
    // Ecran du rayon M3 : zones, etagere, liste des produits, fiche produit (F3.5),
    // variantes (F3.4), "Voir en AR" / "Ajouter au panier" (F3.6), recherche vocale et texte (F3.7, F3.9),
    // secousse pour reposer (F3.8) et vibrations legeres (F3.10).
    // L'interface est construite par code, dans le style du HUD de la boutique (DecoUI).
    public class M3CatalogSelector : MonoBehaviour
    {
        [Header("Donnees et 3D")]
        [SerializeField] private M3ProductCatalog catalog;
        [SerializeField] private M3Shelf shelf;
        [SerializeField] private M3Inspector inspector;
        [SerializeField] private M3ShakeDetector shake;
        [SerializeField] private M3VoiceSearch voice;

        private const string MicExplainedKey = "DecoAR.M3.MicExplained";
        private const float Top = 24f, BarHeight = 84f, SheetWidth = 600f;

        private string category;
        private ProductOption current;
        private int variantIndex;
        private bool leaving;

        // Interface
        private RectTransform root, sheet, searchBar, strip, stripContent, tabsContent, badge, inspectHint;
        private CanvasGroup sheetGroup, stripGroup, searchGroup, fadeGroup, hintGroup;
        private TextMeshProUGUI badgeText, stripTitle, sheetName, sheetMeta, sheetPrice, sheetSize, sheetDescription, variantLabel;
        private RectTransform variantRow;
        private Image micBg;
        private TMP_InputField searchField;
        private DecoToast toast;
        private bool searchOpen;
        private float badgePop;
        private readonly List<(Button b, Image bg, TextMeshProUGUI t, string zone)> tabs = new List<(Button, Image, TextMeshProUGUI, string)>();
        private readonly List<(ProductOption p, Image ring)> cards = new List<(ProductOption, Image)>();
        private readonly List<Image> swatchRings = new List<Image>();

        private void Start()
        {
            if (catalog == null)
            {
                Debug.LogError("[M3] M3ProductCatalog est manquant dans M3CatalogSelector.");
                return;
            }
            DecoUI.EnsureEventSystem();
            BuildUI();

            if (inspector != null)
            {
                inspector.ItemTapped += item => SelectProduct(item.Product);
                inspector.Returned += () => Debug.Log("[M3] Produit repose sur l'etagere");
            }
            if (shake != null) shake.Shaken += OnShake;
            if (voice != null)
            {
                voice.Partial += text => toast.Show($"« {text} »", 3f);
                voice.Result += OnVoiceResult;
                voice.Failed += OnVoiceFailed;
                voice.ListeningChanged += listening => { if (micBg != null) micBg.color = listening ? DecoUI.Terracotta : DecoUI.Tile; };
            }

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.CartCountChanged += RefreshCart;
                RefreshCart(gm.CartCount);
            }

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
            else toast.Show("Touchez un produit sur l'étagère ou dans la liste", 3.5f);
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.CartCountChanged -= RefreshCart;
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
            if (shelf != null) { shelf.Fill(products); shelf.SetTitle(zone); }
            BuildCards(products);

            foreach (var (b, bg, t, z) in tabs)
            {
                bool on = z == zone;
                bg.color = on ? DecoUI.Brass : new Color(1, 1, 1, 0f);
                t.color = on ? DecoUI.Ink : DecoUI.Cream;
                t.fontStyle = on ? FontStyles.Bold : FontStyles.Normal;
            }
            stripTitle.text = $"<b>{zone}</b>  <color=#B8A48E>·  {products.Count} produit{(products.Count > 1 ? "s" : "")}  ·  touchez un produit pour l'inspecter</color>";
            Debug.Log($"[M3] Zone {zone} : {products.Count} produits");
        }

        // Liste des produits sous l'etagere : alternative tactile au tap sur les modeles 3D
        private void BuildCards(List<ProductOption> products)
        {
            for (int i = stripContent.childCount - 1; i >= 0; i--) Destroy(stripContent.GetChild(i).gameObject);
            cards.Clear();
            foreach (var product in products)
            {
                var p = product;
                var card = new GameObject("Produit_" + p.id, typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
                card.SetParent(stripContent, false);
                var le = card.GetComponent<LayoutElement>(); le.preferredWidth = 250; le.preferredHeight = 128;
                var bg = DecoUI.Glassy(card, DecoUI.Glass, DecoUI.Rounded);
                var ring = card.Find("Liseré").GetComponent<Image>();
                var b = card.gameObject.AddComponent<Button>();
                b.targetGraphic = bg;
                b.onClick.AddListener(() => SelectProduct(p));
                var col = DecoUI.Column(card, 2, 0);
                col.padding = new RectOffset(22, 18, 16, 14); col.childAlignment = TextAnchor.MiddleLeft;
                var dot = DecoUI.Row(card, 26, 8, Color.clear, 0, 0);
                DecoUI.Dot(dot, PlacementColor(p.placement), 12);
                DecoUI.Label(dot, PlacementLabel(p.placement), 17, DecoUI.Soft, TextAlignmentOptions.MidlineLeft).rectTransform.offsetMin = Vector2.zero;
                var name = DecoUI.Label(card, $"<b>{p.displayName}</b>", 23, DecoUI.Cream, TextAlignmentOptions.TopLeft, 56);
                name.textWrappingMode = TextWrappingModes.Normal; name.overflowMode = TextOverflowModes.Ellipsis;
                name.rectTransform.offsetMin = name.rectTransform.offsetMax = Vector2.zero;
                var price = DecoUI.Label(card, p.PriceLabel, 23, DecoUI.Brass, TextAlignmentOptions.MidlineLeft, 28);
                price.fontStyle = FontStyles.Bold;
                cards.Add((p, ring));
            }
            var sr = strip.GetComponentInChildren<ScrollRect>();
            if (sr != null) sr.horizontalNormalizedPosition = 0f;
            RefreshCards();
        }

        private void RefreshCards()
        {
            foreach (var (p, ring) in cards)
                ring.color = p == current ? new Color(0.92f, 0.76f, 0.42f, 0.95f) : DecoUI.RingIdle;
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

            FillSheet(product);
            ApplyVariant(variantIndex, false);
            StoreSelection();
            RefreshCards();
            CloseSearch();
            HapticManager.Vibrate(HapticIntensity.Legere);
        }

        private void FillSheet(ProductOption product)
        {
            sheetName.text = $"<b>{product.displayName}</b>";
            sheetMeta.text = $"{product.category}  ·  <color=#{ColorUtility.ToHtmlStringRGB(PlacementColor(product.placement))}>●</color> {PlacementLabel(product.placement)}";
            sheetPrice.text = product.PriceLabel;
            sheetSize.text = $"<b>Dimensions</b>\n<color=#D9CDBF>{product.DimensionsLabel}</color>  <size=80%><color=#B8A48E>(l × p × h)</color></size>";
            sheetDescription.text = product.description;

            for (int i = variantRow.childCount - 1; i >= 0; i--) Destroy(variantRow.GetChild(i).gameObject);
            swatchRings.Clear();
            bool several = product.variants.Count > 1;
            variantRow.parent.gameObject.SetActive(several);
            for (int i = 0; i < product.variants.Count; i++)
            {
                int index = i;
                var v = product.variants[i];
                var sw = new GameObject("Coloris_" + v.name, typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
                sw.SetParent(variantRow, false);
                var le = sw.GetComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = 64;
                var ring = DecoUI.Layer(sw, "Anneau", DecoUI.Circle, DecoUI.Brass, false);
                ring.type = Image.Type.Simple;
                var fill = DecoUI.Layer(sw, "Couleur", DecoUI.Circle, v.color, true);
                fill.type = Image.Type.Simple;
                fill.rectTransform.offsetMin = Vector2.one * 6; fill.rectTransform.offsetMax = -Vector2.one * 6;
                if (v.name == "Original") DecoUI.Icon(fill.transform, M5Icons.Kind.Variante, 30, new Color(0.45f, 0.38f, 0.3f)).GetComponent<LayoutElement>().ignoreLayout = true;
                var b = sw.gameObject.AddComponent<Button>();
                b.targetGraphic = fill;
                b.onClick.AddListener(() => ApplyVariant(index, true));
                swatchRings.Add(ring);
            }
            sheet.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition = 1f;
        }

        private void ApplyVariant(int index, bool userChoice)
        {
            if (current == null) return;
            variantIndex = index;
            var variant = current.GetVariant(index);
            if (variant != null && inspector != null) inspector.ApplyVariant(variant);

            for (int i = 0; i < swatchRings.Count; i++)
                swatchRings[i].color = i == index ? DecoUI.Brass : new Color(1, 1, 1, 0.12f);
            variantLabel.text = variant != null ? $"<b>Coloris</b>  <color=#D9CDBF>{variant.name}</color>" : "";

            if (userChoice)
            {
                StoreSelection();
                HapticManager.Vibrate(HapticIntensity.Legere);
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
            RefreshCards();
            if (!silent) toast.Show("Produit reposé sur l'étagère", 2f);
        }

        private void OnShake()
        {
            if (current == null) return;
            CloseSheet(false);
        }

        // ------------------------------------------------------------ actions

        private void ViewInAR()
        {
            if (current == null || leaving) return;
            if (current.prefab == null)
            {
                toast.Show("Modèle 3D manquant pour ce produit");
                return;
            }
            StoreSelection();
            Debug.Log($"[M3] Voir en AR : {current.id} variante {variantIndex}");
            Leave(SceneNames.AR);
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
            badgePop = 1f;
            toast.Show($"{current.displayName} ajouté au panier", 2.5f, true);
        }

        private void GoToCheckout()
        {
            if (GameManager.Instance != null) GameManager.Instance.ClearShopPose(); // la caisse place le joueur au comptoir
            Leave(SceneNames.Checkout);
        }

        private void BackToShop() => Leave(SceneNames.Shop);

        private void Leave(string scene)
        {
            if (leaving) return;
            leaving = true;
            if (inspector != null && inspector.Current != null) inspector.ReturnCurrent();
            StartCoroutine(FadeAndLoad(scene));
        }

        private System.Collections.IEnumerator FadeAndLoad(string scene)
        {
            fadeGroup.blocksRaycasts = true;
            for (float e = 0; e < 0.3f; e += Time.unscaledDeltaTime) { fadeGroup.alpha = e / 0.3f; yield return null; }
            fadeGroup.alpha = 1f;
            SceneManager.LoadScene(scene);
        }

        // ------------------------------------------------------------ recherche

        public void Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                if (shelf != null) shelf.Highlight(null);
                return;
            }

            var results = catalog.Search(query);
            if (results.Count == 0)
            {
                if (shelf != null) shelf.Highlight(null);
                toast.Show($"« {query} » : aucun produit trouvé", 3f);
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
            toast.Show($"« {query} » : {results.Count} produit{(results.Count > 1 ? "s" : "")}" + (shown < results.Count ? $", {shown} dans ce rayon" : "") + " (en surbrillance)", 3.5f, true);
            if (results.Count == 1) SelectProduct(results[0]);
        }

        private void ToggleSearch()
        {
            searchOpen = !searchOpen;
            if (searchOpen) { searchField.text = ""; searchField.ActivateInputField(); }
            else if (voice != null && voice.IsListening) voice.StopListening();
        }

        private void CloseSearch()
        {
            if (!searchOpen) return;
            searchOpen = false;
            if (voice != null && voice.IsListening) voice.StopListening();
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
                toast.Show("Le micro sert uniquement à rechercher un produit par la voix. Touchez à nouveau le micro pour parler.", 5f);
                return;
            }
            toast.Show("Je vous écoute… dites un nom de produit", 4f);
            voice.StartListening();
        }

        private void OnVoiceResult(string text)
        {
            searchField.text = text;
            Search(text);
        }

        private void OnVoiceFailed(string message)
        {
            toast.Show(message, 3.5f);
            searchField.ActivateInputField();
        }

        // ------------------------------------------------------------ boucle

        private void Update()
        {
            if (root == null) return;
            float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            bool open = current != null;

            sheet.anchoredPosition = new Vector2(Mathf.Lerp(sheet.anchoredPosition.x, open ? -24f : SheetWidth + 60f, k), sheet.anchoredPosition.y);
            sheetGroup.alpha = Mathf.Lerp(sheetGroup.alpha, open ? 1f : 0f, k);
            sheetGroup.interactable = sheetGroup.blocksRaycasts = open;

            // Pendant l'inspection, la liste laisse la place au produit et aux gestes
            stripGroup.alpha = Mathf.Lerp(stripGroup.alpha, open ? 0f : 1f, k);
            stripGroup.interactable = stripGroup.blocksRaycasts = !open;
            strip.anchoredPosition = new Vector2(0, Mathf.Lerp(strip.anchoredPosition.y, open ? -60f : 24f, k));
            hintGroup.alpha = Mathf.Lerp(hintGroup.alpha, open && inspector != null && !inspector.IsBusy ? 1f : 0f, k);

            searchGroup.alpha = Mathf.Lerp(searchGroup.alpha, searchOpen ? 1f : 0f, k);
            searchGroup.interactable = searchGroup.blocksRaycasts = searchOpen;
            searchBar.anchoredPosition = new Vector2(searchBar.anchoredPosition.x, Mathf.Lerp(searchBar.anchoredPosition.y, searchOpen ? -(Top + BarHeight + 16f) : -(Top + BarHeight - 20f), k));
            if (micBg != null && voice != null && voice.IsListening) micBg.color = Color.Lerp(DecoUI.Terracotta, DecoUI.Brass, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f));

            if (!leaving && fadeGroup.alpha > 0f) fadeGroup.alpha = Mathf.MoveTowards(fadeGroup.alpha, 0f, Time.unscaledDeltaTime * 2.5f);
            badgePop = Mathf.MoveTowards(badgePop, 0f, Time.unscaledDeltaTime * 3f);
            badge.localScale = Vector3.one * (1f + 0.45f * Mathf.Sin(badgePop * Mathf.PI) * badgePop);
        }

        private void RefreshCart(int count)
        {
            if (badge == null) return;
            badge.gameObject.SetActive(count > 0);
            badgeText.text = count > 9 ? "9+" : count.ToString();
            badgePop = 1f;
        }

        // ------------------------------------------------------------ construction

        private void BuildUI()
        {
            root = DecoUI.Canvas(transform, "M3_Interface", 50);

            BuildStrip();
            BuildSheet();
            BuildTopBar();
            BuildSearch();
            BuildHint();
            toast = DecoToast.Create(root, Top + BarHeight + 18f);

            var fade = DecoUI.Box("Fondu", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.08f, 0.06f, 0.05f));
            fade.offsetMin = fade.offsetMax = new Vector2(-200, -200);
            fade.GetComponent<Image>().sprite = null;
            fadeGroup = fade.gameObject.AddComponent<CanvasGroup>(); fadeGroup.alpha = 1f; fadeGroup.blocksRaycasts = false;
        }

        private void BuildTopBar()
        {
            // Gauche : retour à la boutique
            var back = new GameObject("Retour", typeof(RectTransform)).GetComponent<RectTransform>();
            back.SetParent(root, false);
            back.anchorMin = back.anchorMax = back.pivot = new Vector2(0, 1);
            back.anchoredPosition = new Vector2(24, -Top); back.sizeDelta = new Vector2(250, BarHeight);
            var bbg = DecoUI.Glassy(back, DecoUI.Glass);
            var bb = back.gameObject.AddComponent<Button>(); bb.targetGraphic = bbg;
            bb.onClick.AddListener(BackToShop);
            var h = back.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(22, 30, 0, 0); h.spacing = 8; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            DecoUI.Icon(back, M5Icons.Kind.Retour, 38, DecoUI.Cream);
            var bt = DecoUI.Label(back, "<b>Boutique</b>", 25, DecoUI.Cream, TextAlignmentOptions.MidlineLeft);
            bt.rectTransform.offsetMin = Vector2.zero;

            // Droite : recherche + panier
            var right = new GameObject("Actions", typeof(RectTransform)).GetComponent<RectTransform>();
            right.SetParent(root, false);
            right.anchorMin = right.anchorMax = right.pivot = new Vector2(1, 1);
            right.anchoredPosition = new Vector2(-24, -Top); right.sizeDelta = new Vector2(BarHeight * 2 + 14, BarHeight);
            var rh = right.gameObject.AddComponent<HorizontalLayoutGroup>();
            rh.spacing = 14; rh.childAlignment = TextAnchor.MiddleRight;
            rh.childControlWidth = rh.childControlHeight = false; rh.childForceExpandWidth = rh.childForceExpandHeight = false;
            DecoUI.FloatButton(right, M5Icons.Kind.Loupe, BarHeight, ToggleSearch, out _, out _);
            var cart = DecoUI.FloatButton(right, M5Icons.Kind.Panier, BarHeight, GoToCheckout, out _, out _);
            badge = DecoUI.Box("Badge", cart.transform, Vector2.one, Vector2.one, new Vector2(-8, -8), new Vector2(32, 32), new Color(0.82f, 0.36f, 0.27f));
            var bi = badge.GetComponent<Image>(); bi.sprite = DecoUI.Circle; bi.type = Image.Type.Simple; bi.raycastTarget = false;
            badge.GetComponent<LayoutElement>().ignoreLayout = true;
            badgeText = DecoUI.Label(badge, "", 18, Color.white, TextAlignmentOptions.Center);
            badgeText.fontStyle = FontStyles.Bold;
            badgeText.rectTransform.offsetMin = badgeText.rectTransform.offsetMax = Vector2.zero;

            // Centre : les rayons, dans une pastille de verre défilante
            var bar = new GameObject("Rayons", typeof(RectTransform)).GetComponent<RectTransform>();
            bar.SetParent(root, false);
            bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1); bar.pivot = new Vector2(0.5f, 1);
            bar.offsetMin = new Vector2(24 + 250 + 18, -Top - BarHeight); bar.offsetMax = new Vector2(-(24 + BarHeight * 2 + 14 + 18), -Top);
            DecoUI.Glassy(bar, DecoUI.Glass);
            var inner = new GameObject("Zone", typeof(RectTransform)).GetComponent<RectTransform>();
            inner.SetParent(bar, false);
            DecoUI.Stretch(inner, 0); inner.offsetMin = new Vector2(10, 9); inner.offsetMax = new Vector2(-10, -9);
            DecoUI.Column(inner, 0, 0);
            DecoUI.Scroll(inner, true, 6, out tabsContent);
            foreach (var zone in M3Categories.All)
            {
                string z = zone;
                var tab = DecoUI.Box("Rayon_" + z, tabsContent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Color.clear);
                tab.GetComponent<Image>().sprite = DecoUI.PillShape;
                var t = DecoUI.Label(tab, z, 23, DecoUI.Cream, TextAlignmentOptions.Center);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                tab.GetComponent<LayoutElement>().preferredWidth = t.GetPreferredValues(z, 400, 40).x + 54;
                t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
                var b = tab.gameObject.AddComponent<Button>();
                b.onClick.AddListener(() => { CloseSearch(); ShowZone(z); HapticManager.Vibrate(HapticIntensity.Legere); });
                tabs.Add((b, tab.GetComponent<Image>(), t, z));
            }
        }

        private void BuildSearch()
        {
            searchBar = new GameObject("Recherche", typeof(RectTransform)).GetComponent<RectTransform>();
            searchBar.SetParent(root, false);
            searchBar.anchorMin = searchBar.anchorMax = searchBar.pivot = new Vector2(1, 1);
            searchBar.sizeDelta = new Vector2(640, 78);
            searchBar.anchoredPosition = new Vector2(-24, -(Top + BarHeight - 20f));
            DecoUI.Glassy(searchBar, DecoUI.GlassStrong);
            searchGroup = searchBar.gameObject.AddComponent<CanvasGroup>(); searchGroup.alpha = 0; searchGroup.blocksRaycasts = false;
            var h = searchBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(24, 10, 8, 8); h.spacing = 10; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            DecoUI.Icon(searchBar, M5Icons.Kind.Loupe, 34, DecoUI.Soft);
            searchField = DecoUI.InputField(searchBar, "Canapé, lampe, plante…", 25);
            var le = searchField.GetComponent<LayoutElement>(); le.flexibleWidth = 1; le.preferredHeight = 60;
            searchField.onSubmit.AddListener(Search);
            var mic = DecoUI.IconButton(searchBar, M5Icons.Kind.Micro, 60, DecoUI.Tile, DecoUI.Cream, ToggleVoice);
            micBg = mic.GetComponent<Image>();
            DecoUI.IconButton(searchBar, M5Icons.Kind.Chevron, 60, DecoUI.Brass, DecoUI.Ink, () => Search(searchField.text));
        }

        private void BuildStrip()
        {
            strip = new GameObject("Produits", typeof(RectTransform)).GetComponent<RectTransform>();
            strip.SetParent(root, false);
            strip.anchorMin = new Vector2(0, 0); strip.anchorMax = new Vector2(1, 0); strip.pivot = new Vector2(0.5f, 0);
            strip.offsetMin = new Vector2(24, 24); strip.offsetMax = new Vector2(-24, 24 + 190);
            stripGroup = strip.gameObject.AddComponent<CanvasGroup>();
            DecoUI.Column(strip, 8, 0).childAlignment = TextAnchor.LowerLeft;
            var titleRow = DecoUI.Row(strip, 40, 0, Color.clear, 0, 0);
            var pill = DecoUI.Box("Titre", titleRow, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, DecoUI.Glass);
            pill.GetComponent<Image>().sprite = DecoUI.PillShape; pill.GetComponent<Image>().raycastTarget = false;
            var ph = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            ph.padding = new RectOffset(22, 22, 0, 0); ph.childAlignment = TextAnchor.MiddleLeft;
            ph.childControlWidth = ph.childControlHeight = true; ph.childForceExpandWidth = ph.childForceExpandHeight = false;
            stripTitle = DecoUI.Label(pill, "", 21, DecoUI.Cream, TextAlignmentOptions.MidlineLeft);
            stripTitle.textWrappingMode = TextWrappingModes.NoWrap;
            var sr = DecoUI.Scroll(strip, true, 14, out stripContent);
            sr.GetComponent<LayoutElement>().preferredHeight = 140;
            stripContent.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(4, 4, 6, 6);
        }

        private void BuildSheet()
        {
            sheet = DecoUI.Box("Fiche produit", root, new Vector2(1, 0), new Vector2(1, 1), new Vector2(SheetWidth + 60f, 0), new Vector2(SheetWidth, 0), Color.clear);
            sheet.pivot = new Vector2(1, 0.5f);
            sheet.offsetMin = new Vector2(sheet.offsetMin.x, 24f);
            sheet.offsetMax = new Vector2(sheet.offsetMax.x, -(Top + BarHeight + 22f));
            sheetGroup = sheet.gameObject.AddComponent<CanvasGroup>(); sheetGroup.alpha = 0;
            var sh = DecoUI.Layer(sheet, "Ombre", DecoUI.Shadow, new Color(0, 0, 0, 0.45f), false);
            sh.rectTransform.offsetMin = new Vector2(-30, -34); sh.rectTransform.offsetMax = new Vector2(30, 20);
            DecoUI.Layer(sheet, "Fond", DecoUI.PillShape, DecoUI.GlassStrong, true);
            DecoUI.Layer(sheet, "Liseré", DecoUI.RingSprite, new Color(1, 1, 1, 0.10f), false);
            var v = DecoUI.Column(sheet, 12, 26);
            v.padding = new RectOffset(28, 28, 24, 24);

            var head = DecoUI.Row(sheet, 0, 10, Color.clear, 0, 0);
            var titles = new GameObject("Titres", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            titles.SetParent(head, false);
            titles.GetComponent<LayoutElement>().flexibleWidth = 1;
            DecoUI.Column(titles, 2, 0);
            titles.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sheetName = DecoUI.Label(titles, "", 34, DecoUI.Cream, TextAlignmentOptions.TopLeft);
            sheetName.textWrappingMode = TextWrappingModes.Normal;
            sheetName.rectTransform.offsetMin = Vector2.zero;
            sheetMeta = DecoUI.Label(titles, "", 21, DecoUI.Soft, TextAlignmentOptions.TopLeft, 30);
            sheetMeta.rectTransform.offsetMin = Vector2.zero;
            head.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var close = DecoUI.IconButton(head, M5Icons.Kind.Fermer, 56, new Color(1, 1, 1, 0.1f), DecoUI.Cream, PutBack);
            close.GetComponent<LayoutElement>().flexibleWidth = 0;

            sheetPrice = DecoUI.Label(sheet, "", 40, DecoUI.Brass, TextAlignmentOptions.MidlineLeft, 52);
            sheetPrice.fontStyle = FontStyles.Bold;
            sheetPrice.rectTransform.offsetMin = Vector2.zero;
            DecoUI.Separator(sheet);

            DecoUI.Scroll(sheet, false, 14, out var body);
            var size = DecoUI.Row(body, 76, 14, DecoUI.Tile);
            DecoUI.Icon(size, M5Icons.Kind.Taille, 34, DecoUI.Brass);
            sheetSize = DecoUI.Label(size, "", 21, DecoUI.Cream, TextAlignmentOptions.MidlineLeft);
            sheetSize.GetComponent<LayoutElement>().flexibleWidth = 1;

            var colors = new GameObject("Coloris", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            colors.SetParent(body, false);
            DecoUI.Column(colors, 8, 0);
            colors.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            variantLabel = DecoUI.Label(colors, "", 22, DecoUI.Cream, TextAlignmentOptions.MidlineLeft, 32);
            variantLabel.rectTransform.offsetMin = Vector2.zero;
            variantRow = DecoUI.Row(colors, 70, 12, Color.clear, 0, 0);

            sheetDescription = DecoUI.Label(body, "", 21, DecoUI.Soft, TextAlignmentOptions.TopLeft);
            sheetDescription.textWrappingMode = TextWrappingModes.Normal;
            sheetDescription.rectTransform.offsetMin = Vector2.zero;
            sheetDescription.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            DecoUI.Pill(sheet, "<b>Voir chez moi en AR</b>", DecoUI.Brass, DecoUI.Ink, ViewInAR, 80, 27, M5Icons.Kind.Cube);
            DecoUI.Pill(sheet, "<b>Ajouter au panier</b>", DecoUI.Sage, Color.white, AddToCart, 72, 25, M5Icons.Kind.Panier);
            var put = DecoUI.Pill(sheet, "Reposer sur l'étagère", new Color(1, 1, 1, 0f), DecoUI.Soft, PutBack, 44, 21);
            put.GetComponent<Image>().raycastTarget = true;
        }

        private void BuildHint()
        {
            inspectHint = DecoUI.Box("Gestes", root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 24), new Vector2(0, 64), Color.clear);
            inspectHint.pivot = Vector2.zero;
            DecoUI.Glassy(inspectHint, DecoUI.Glass).raycastTarget = false;
            hintGroup = inspectHint.gameObject.AddComponent<CanvasGroup>(); hintGroup.alpha = 0; hintGroup.blocksRaycasts = false;
            var h = inspectHint.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(24, 26, 0, 0); h.spacing = 10; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            inspectHint.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            DecoUI.Icon(inspectHint, M5Icons.Kind.Pivoter, 30, DecoUI.Brass);
            DecoUI.Label(inspectHint, "Glissez pour tourner", 21, DecoUI.Cream, TextAlignmentOptions.MidlineLeft).rectTransform.offsetMin = Vector2.zero;
            DecoUI.Icon(inspectHint, M5Icons.Kind.Pincer, 30, DecoUI.Brass);
            DecoUI.Label(inspectHint, "Pincez pour zoomer", 21, DecoUI.Cream, TextAlignmentOptions.MidlineLeft).rectTransform.offsetMin = Vector2.zero;
            DecoUI.Icon(inspectHint, M5Icons.Kind.Toucher, 30, DecoUI.Brass);
            DecoUI.Label(inspectHint, "Secouez pour reposer", 21, DecoUI.Cream, TextAlignmentOptions.MidlineLeft).rectTransform.offsetMin = Vector2.zero;
        }

        private static string PlacementLabel(ProductPlacement placement)
        {
            switch (placement)
            {
                case ProductPlacement.Mur: return "Se fixe au mur";
                case ProductPlacement.Sol: return "Se pose au sol";
                default: return "Sur une table ou au sol";
            }
        }

        private static Color PlacementColor(ProductPlacement placement)
        {
            switch (placement)
            {
                case ProductPlacement.Mur: return new Color(0.86f, 0.62f, 0.5f);
                case ProductPlacement.Sol: return new Color(0.62f, 0.78f, 0.58f);
                default: return new Color(0.9f, 0.76f, 0.46f);
            }
        }
    }
}
