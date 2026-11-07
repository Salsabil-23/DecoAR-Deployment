using DecoAR.Core;
using DecoAR.M5;
using DecoAR.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DecoAR.AR
{
    // Interface de l'AR dans le style de l'application (DecoUI) : remplace M4_Canvas au lancement.
    //  - en haut : retour au rayon / à la boutique, fiche du produit (nom, coloris, prix, taille), aide, panier ;
    //  - à droite : coloris suivant, taille réelle, photo ;
    //  - en bas : consigne de scan, puis « Ajouter au panier » une fois le produit posé ;
    //  - guide des gestes à la première pose : glisser pour déplacer, pincer pour la taille, deux doigts pour tourner.
    public class M4Hud : MonoBehaviour
    {
        const float Top = 24f, BarHeight = 84f;

        M4PlacementGate gate;
        M4ShopActions shop;
        M4Photo photo;

        RectTransform root, guide, badge, cta, message, tools;
        CanvasGroup guideGroup, ctaGroup, messageGroup, toolsGroup, fadeGroup;
        TextMeshProUGUI productText, sizeText, messageText, badgeText;
        GameObject variantTool, resetTool;
        DecoToast toast;
        bool guideOpen, guideShown, leaving;
        float badgePop;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded += (s, _) => Attach();
            Attach();
        }

        static void Attach()
        {
            var g = FindAnyObjectByType<M4PlacementGate>();
            if (g == null || g.GetComponent<M4Hud>() != null) return;
            g.gameObject.AddComponent<M4Hud>();
        }

        void Start()
        {
            gate = GetComponent<M4PlacementGate>();
            shop = FindAnyObjectByType<M4ShopActions>();
            photo = FindAnyObjectByType<M4Photo>();
            var old = GameObject.Find("M4_Canvas");
            if (old != null) old.SetActive(false);

            DecoUI.EnsureEventSystem();
            Build();
            gate.BindUI(messageText, message.gameObject, resetTool, variantTool, productText);
            gate.ProductPlaced += OnPlaced;
            if (photo != null) photo.UiCanvas = root.GetComponentInParent<Canvas>();

            var gm = GameManager.Instance;
            if (gm != null) { gm.CartCountChanged += RefreshCart; RefreshCart(gm.CartCount); }
        }

        void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.CartCountChanged -= RefreshCart;
            if (gate != null) gate.ProductPlaced -= OnPlaced;
        }

        void OnPlaced()
        {
            if (guideShown) return;
            guideShown = true;
            guideOpen = true; // la première fois : comment déplacer, agrandir, tourner
        }

        void Update()
        {
            if (root == null) return;
            float k = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            bool placed = gate.IsPlaced;

            guideGroup.alpha = Mathf.Lerp(guideGroup.alpha, guideOpen ? 1f : 0f, k);
            guideGroup.interactable = guideGroup.blocksRaycasts = guideOpen;
            guide.localScale = Vector3.one * Mathf.Lerp(guide.localScale.x, guideOpen ? 1f : 0.94f, k);

            bool showCta = placed && !guideOpen;
            ctaGroup.alpha = Mathf.Lerp(ctaGroup.alpha, showCta ? 1f : 0f, k);
            ctaGroup.interactable = ctaGroup.blocksRaycasts = showCta;
            toolsGroup.alpha = Mathf.Lerp(toolsGroup.alpha, guideOpen ? 0.25f : 1f, k);

            bool msg = message.gameObject.activeSelf && !guideOpen;
            messageGroup.alpha = Mathf.Lerp(messageGroup.alpha, msg ? 1f : 0f, k);
            var pref = messageText.GetPreferredValues(messageText.text, 900f, 0f);
            message.sizeDelta = new Vector2(Mathf.Lerp(message.sizeDelta.x, Mathf.Min(pref.x, 900f) + 110f, k), 72f);
            message.anchoredPosition = new Vector2(0, placed ? 140f : 40f);

            // Taille : 100 % = taille réelle du catalogue
            if (placed)
            {
                float pct = gate.Placed.localScale.x * 100f;
                sizeText.text = Mathf.Abs(pct - 100f) < 1.5f
                    ? "<color=#9FCB94>●</color> Taille réelle"
                    : $"<color=#E0B35A>●</color> {pct:0} % de la taille réelle";
            }
            else sizeText.text = gate.Placement == PlacementType.Mur ? "Vise un mur bien éclairé" : "Vise le sol, bouge doucement le téléphone";

            if (!leaving && fadeGroup.alpha > 0f) fadeGroup.alpha = Mathf.MoveTowards(fadeGroup.alpha, 0f, Time.unscaledDeltaTime * 2.5f);
            badgePop = Mathf.MoveTowards(badgePop, 0f, Time.unscaledDeltaTime * 3f);
            badge.localScale = Vector3.one * (1f + 0.45f * Mathf.Sin(badgePop * Mathf.PI) * badgePop);
            cta.localScale = Vector3.one * (showCta ? 1f + 0.02f * Mathf.Sin(Time.unscaledTime * 3f) : 0.96f);
        }

        void RefreshCart(int count)
        {
            badge.gameObject.SetActive(count > 0);
            badgeText.text = count > 9 ? "9+" : count.ToString();
            badgePop = 1f;
        }

        void Go(string scene)
        {
            if (leaving) return;
            leaving = true;
            StartCoroutine(FadeAndLoad(scene));
        }

        System.Collections.IEnumerator FadeAndLoad(string scene)
        {
            fadeGroup.blocksRaycasts = true;
            for (float e = 0; e < 0.3f; e += Time.unscaledDeltaTime) { fadeGroup.alpha = e / 0.3f; yield return null; }
            SceneManager.LoadScene(scene);
        }

        // ------------------------------------------------------------ construction

        void Build()
        {
            root = DecoUI.Canvas(transform, "M4_Interface", 60);

            // En haut à gauche : retours
            var left = Cluster(true);
            NavPill(left, "Rayon", M5Icons.Kind.Retour, () => Go(SceneNames.Catalog));
            NavPill(left, "Boutique", null, () => Go(SceneNames.Shop));

            // En haut à droite : aide + panier
            var right = Cluster(false);
            DecoUI.FloatButton(right, M5Icons.Kind.Aide, BarHeight, () => guideOpen = !guideOpen, out _, out _);
            var cart = DecoUI.FloatButton(right, M5Icons.Kind.Panier, BarHeight, () => { GameManager.Instance?.ClearShopPose(); Go(SceneNames.Checkout); }, out _, out _);
            badge = DecoUI.Box("Badge", cart.transform, Vector2.one, Vector2.one, new Vector2(-8, -8), new Vector2(32, 32), DecoUI.Badge);
            var bi = badge.GetComponent<Image>(); bi.sprite = DecoUI.Circle; bi.type = Image.Type.Simple; bi.raycastTarget = false;
            badge.GetComponent<LayoutElement>().ignoreLayout = true;
            badgeText = DecoUI.Label(badge, "", 18, Color.white, TextAlignmentOptions.Center);
            badgeText.fontStyle = FontStyles.Bold;
            badgeText.rectTransform.offsetMin = badgeText.rectTransform.offsetMax = Vector2.zero;

            // En haut au centre : la fiche du produit
            var card = DecoUI.Box("Produit", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -Top), new Vector2(0, BarHeight + 18), Color.clear);
            card.pivot = new Vector2(0.5f, 1);
            DecoUI.Glassy(card, DecoUI.Glass).raycastTarget = false;
            var cv = DecoUI.Column(card, 0, 0);
            cv.padding = new RectOffset(34, 34, 12, 12); cv.childAlignment = TextAnchor.MiddleCenter; cv.childForceExpandWidth = false;
            card.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            productText = DecoUI.Label(card, "", 25, DecoUI.Cream, TextAlignmentOptions.Center, 40);
            productText.textWrappingMode = TextWrappingModes.NoWrap;
            productText.rectTransform.offsetMin = productText.rectTransform.offsetMax = Vector2.zero;
            sizeText = DecoUI.Label(card, "", 20, DecoUI.Soft, TextAlignmentOptions.Center, 30);
            sizeText.textWrappingMode = TextWrappingModes.NoWrap;

            // À droite : outils
            tools = new GameObject("Outils", typeof(RectTransform)).GetComponent<RectTransform>();
            tools.SetParent(root, false);
            tools.anchorMin = tools.anchorMax = tools.pivot = new Vector2(1, 0.5f);
            tools.anchoredPosition = new Vector2(-24, -10);
            tools.sizeDelta = new Vector2(130, 420);
            var tv = tools.gameObject.AddComponent<VerticalLayoutGroup>();
            tv.spacing = 18; tv.childAlignment = TextAnchor.MiddleCenter;
            tv.childControlWidth = tv.childControlHeight = false; tv.childForceExpandWidth = tv.childForceExpandHeight = false;
            toolsGroup = tools.gameObject.AddComponent<CanvasGroup>();
            variantTool = Tool(tools, M5Icons.Kind.Variante, "Coloris", () => gate.NextVariant());
            resetTool = Tool(tools, M5Icons.Kind.Taille, "Taille réelle", () => gate.ResetToRealSize());
            Tool(tools, M5Icons.Kind.Photo, "Photo", () => { if (photo != null) photo.TakePhoto(); });

            // En bas : consigne, puis action principale
            message = DecoUI.Box("Consigne", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(500, 72), Color.clear);
            message.pivot = new Vector2(0.5f, 0);
            DecoUI.Glassy(message, DecoUI.Glass).raycastTarget = false;
            messageGroup = message.gameObject.AddComponent<CanvasGroup>(); messageGroup.blocksRaycasts = false;
            var dot = DecoUI.Box("Point", message, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(16, 16), DecoUI.Brass);
            dot.GetComponent<Image>().sprite = DecoUI.Circle; dot.GetComponent<Image>().type = Image.Type.Simple;
            dot.gameObject.AddComponent<Pulse>();
            messageText = DecoUI.Label(message, "", 25, DecoUI.Cream, TextAlignmentOptions.MidlineLeft);
            messageText.rectTransform.offsetMin = new Vector2(66, 0); messageText.rectTransform.offsetMax = new Vector2(-30, 0);
            messageText.textWrappingMode = TextWrappingModes.NoWrap;

            cta = new GameObject("Ajouter", typeof(RectTransform)).GetComponent<RectTransform>();
            cta.SetParent(root, false);
            cta.anchorMin = cta.anchorMax = cta.pivot = new Vector2(0.5f, 0);
            cta.anchoredPosition = new Vector2(0, 36); cta.sizeDelta = new Vector2(480, 92);
            var add = DecoUI.Pill(cta, "<b>Ajouter au panier</b>", DecoUI.Sage, Color.white, AddToCart, 92, 29, M5Icons.Kind.Panier);
            DecoUI.Stretch((RectTransform)add.transform);
            ctaGroup = cta.gameObject.AddComponent<CanvasGroup>(); ctaGroup.alpha = 0;

            BuildGuide();
            toast = DecoToast.Create(root, Top + BarHeight + 40f);

            fadeGroup = DecoUI.Fade(root);
        }

        void AddToCart()
        {
            if (shop != null) shop.AddToCart();
            var p = gate.Product;
            if (p != null) toast.Show($"{p.displayName} ajouté au panier", 2.5f, true);
        }

        /// Fenêtre d'aide : les trois gestes, avec leurs icônes.
        void BuildGuide()
        {
            guide = DecoUI.Box("Guide des gestes", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(720, 0), Color.clear);
            var sh = DecoUI.Layer(guide, "Ombre", DecoUI.Shadow, new Color(0, 0, 0, 0.5f), false);
            sh.rectTransform.offsetMin = new Vector2(-30, -34); sh.rectTransform.offsetMax = new Vector2(30, 20);
            DecoUI.Layer(guide, "Fond", DecoUI.PillShape, DecoUI.GlassStrong, true);
            DecoUI.Layer(guide, "Liseré", DecoUI.RingSprite, new Color(1, 1, 1, 0.12f), false);
            var v = DecoUI.Column(guide, 14, 34);
            guide.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            guideGroup = guide.gameObject.AddComponent<CanvasGroup>(); guideGroup.alpha = 0; guideGroup.blocksRaycasts = false;

            DecoUI.Label(guide, "<b>Posé ! Tu peux l'ajuster du doigt</b>", 34, DecoUI.Cream, TextAlignmentOptions.Center, 48);
            DecoUI.Label(guide, "Ajuste-le directement avec tes doigts", 22, DecoUI.Soft, TextAlignmentOptions.Center, 32);
            Gesture(M5Icons.Kind.Deplacer, "Déplacer", "Glisse l'objet avec un doigt");
            Gesture(M5Icons.Kind.Pincer, "Changer la taille", "Pince ou écarte deux doigts sur l'objet");
            Gesture(M5Icons.Kind.Pivoter, "Tourner", "Fais tourner deux doigts sur l'objet");
            Gesture(M5Icons.Kind.Toucher, "Changer de place", "Touche un autre endroit du sol ou du mur");
            DecoUI.Label(guide, "<color=#B8A48E>« Taille réelle » remet l'objet à ses vraies dimensions.</color>", 20, DecoUI.Soft, TextAlignmentOptions.Center, 34);
            DecoUI.Pill(guide, "<b>C'est compris</b>", DecoUI.Brass, DecoUI.Ink, () => guideOpen = false, 76, 27);
        }

        void Gesture(M5Icons.Kind icon, string title, string text)
        {
            var row = DecoUI.Row(guide, 88, 18, DecoUI.Tile, 18, 18);
            var circle = DecoUI.Box("Cercle", row, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.82f, 0.65f, 0.32f, 0.18f));
            circle.GetComponent<Image>().sprite = DecoUI.Circle; circle.GetComponent<Image>().type = Image.Type.Simple;
            var le = circle.GetComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = 64; le.minWidth = 64;
            DecoUI.Icon(circle, icon, 38, DecoUI.Brass).GetComponent<LayoutElement>().ignoreLayout = true;
            var t = DecoUI.Label(row, $"<b>{title}</b>\n<size=82%><color=#D9CDBF>{text}</color></size>", 24, DecoUI.Cream, TextAlignmentOptions.MidlineLeft);
            t.GetComponent<LayoutElement>().flexibleWidth = 1;
            t.rectTransform.offsetMin = Vector2.zero;
        }

        RectTransform Cluster(bool left)
        {
            var rt = new GameObject(left ? "Gauche" : "Droite", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(root, false);
            float x = left ? 0f : 1f;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(x, 1);
            rt.anchoredPosition = new Vector2(left ? 24f : -24f, -Top);
            rt.sizeDelta = new Vector2(0, BarHeight);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14; h.childAlignment = left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            rt.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rt;
        }

        void NavPill(Transform parent, string text, M5Icons.Kind? icon, System.Action onClick)
        {
            var rt = new GameObject("Retour_" + text, typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.GetComponent<LayoutElement>().preferredHeight = BarHeight;
            var bg = DecoUI.Glassy(rt, DecoUI.Glass);
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = bg;
            b.onClick.AddListener(() => onClick());
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(icon.HasValue ? 22 : 30, 30, 0, 0); h.spacing = 8; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            if (icon.HasValue) DecoUI.Icon(rt, icon.Value, 38, DecoUI.Cream);
            var t = DecoUI.Label(rt, $"<b>{text}</b>", 25, DecoUI.Cream, TextAlignmentOptions.MidlineLeft);
            t.rectTransform.offsetMin = Vector2.zero;
            t.textWrappingMode = TextWrappingModes.NoWrap;
        }

        GameObject Tool(Transform parent, M5Icons.Kind icon, string label, System.Action onClick)
        {
            var rt = new GameObject("Outil_" + label, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(130, 118);
            var b = DecoUI.FloatButton(rt, icon, 84, onClick, out _, out _);
            var brt = (RectTransform)b.transform;
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 1); brt.anchoredPosition = Vector2.zero;
            // Étiquette sur une pastille de verre : lisible sur n'importe quelle image de la caméra
            var tag = DecoUI.Box("Etiquette", rt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 30), DecoUI.Glass);
            tag.pivot = new Vector2(0.5f, 0);
            tag.GetComponent<Image>().sprite = DecoUI.PillShape; tag.GetComponent<Image>().raycastTarget = false;
            var t = DecoUI.Label(tag, label, 17, DecoUI.Cream, TextAlignmentOptions.Center);
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.fontStyle = FontStyles.Bold;
            tag.sizeDelta = new Vector2(t.GetPreferredValues(label).x + 28f, 30f);
            return rt.gameObject;
        }

        /// Point qui pulse doucement (consigne en attente).
        class Pulse : MonoBehaviour
        {
            void Update() => transform.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(Time.unscaledTime * 4f));
        }
    }
}
