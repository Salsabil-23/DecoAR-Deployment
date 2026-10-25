using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DecoAR.M5
{
    /// UI de caisse (construite par code) : catalogue démo, récapitulatif commande en TND, confirmation, toasts, joystick mobile.
    public class M5CheckoutUI : MonoBehaviour
    {
        [Serializable]
        public class Product
        {
            public string name; public float price; public Color color = Color.white; public GameObject prefab;
            public Product(string n, float p, Color c) { name = n; price = p; color = c; }
        }

        public List<Product> catalog = new List<Product>
        {
            new Product("Vase en céramique", 45f, new Color(0.76f, 0.45f, 0.32f)),
            new Product("Coussin en lin", 60f, new Color(0.62f, 0.69f, 0.58f)),
            new Product("Bougie parfumée", 25f, new Color(0.97f, 0.94f, 0.87f)),
            new Product("Lampe en laiton", 120f, new Color(0.83f, 0.67f, 0.33f)),
            new Product("Cadre photo", 35f, new Color(0.86f, 0.66f, 0.64f)),
        };
        public string currency = "TND";
        [Header("Scène")]
        public Transform zoneCenter;
        public float zoneRadius = 2.5f;
        public Basket3D basket;
        public ParcelAnimation parcel;
        public VendorNPC vendor;
        public M5PlayerController player;
        public bool focusBasketOnEnter = true;

        public event Action<float> OnOrderConfirmed;
        public float Total { get { float t = 0; foreach (var l in lines) t += l.p.price * l.qty; return t; } }
        public string DeliveryAddress => addressField ? addressField.text.Trim() : "";

        class Line { public Product p; public int qty; }
        readonly List<Line> lines = new List<Line>();

        static readonly Color Dark = new Color(0.17f, 0.14f, 0.11f, 0.92f);
        static readonly Color Cream = new Color(0.98f, 0.95f, 0.89f);
        static readonly Color Sage = new Color(0.45f, 0.58f, 0.43f);
        static readonly Color Brass = new Color(0.80f, 0.63f, 0.30f);
        static readonly Color Muted = new Color(0.55f, 0.47f, 0.40f);

        Sprite rounded, circle;
        RectTransform panel, joyBase, joyKnob;
        CanvasGroup panelGroup, toastGroup;
        TextMeshProUGUI listText, totalText, hintText, toastText;
        Button confirmBtn, gpsBtn;
        TMP_InputField addressField;
        TextMeshProUGUI addressStatus, gpsLabel, gyroLabel;
        M5LocationService location;
        RectTransform safeArea;
        Rect lastSafe;
        bool inZone;
        float toastUntil;
        const string AddressKey = "M5_Adresse";
        const string GpsText = "Utiliser ma position (GPS)";

        void Start()
        {
            if (!basket) basket = Basket3D.Instance;
            if (!vendor || !vendor.isActiveAndEnabled) vendor = VendorNPC.Instance;
            EnsureEventSystem();
            BuildUI();
            ImportSharedCart();
            Refresh();
        }

        // Articles ajoutes depuis le catalogue M3 ou l'AR M4 (GameManager)
        void ImportSharedCart()
        {
            var gm = DecoAR.Core.GameManager.Instance;
            if (gm == null) return;
            foreach (var item in gm.TakeCartItems())
            {
                string label = string.IsNullOrEmpty(item.variantName) ? item.name : $"{item.name} ({item.variantName})";
                var p = catalog.Find(x => x.name == label) ?? new Product(label, item.price, item.color) { prefab = item.prefab };
                for (int i = 0; i < Mathf.Max(1, item.quantity); i++) AddProduct(p);
            }
        }

        // ---------- Logique panier ----------
        public void AddProduct(Product p)
        {
            var l = lines.Find(x => x.p == p);
            if (l != null) l.qty++; else lines.Add(new Line { p = p, qty = 1 });
            if (basket) basket.AddItem(p.prefab, p.color);
            if (vendor) vendor.ShowBubble("Excellent choix !", 2f);
            Toast($"{p.name} ajouté au panier");
            Refresh();
        }

        /// Point d'entrée pour les autres modules.
        public void AddProduct(string name, float price, Color color, GameObject prefab = null)
            => AddProduct(new Product(name, price, color) { prefab = prefab });

        public void ClearCart()
        {
            lines.Clear();
            if (basket) basket.Clear();
            Refresh();
        }

        public void ConfirmOrder()
        {
            if (lines.Count == 0) { Toast("Votre panier est vide"); return; }
            if (string.IsNullOrEmpty(DeliveryAddress))
            {
                Toast("Ajoutez votre adresse de livraison (ou « Utiliser ma position »)", 3f);
                if (addressField) addressField.Select();
                return;
            }
            if (parcel && parcel.IsPlaying) return;
            float total = Total;
            string address = DeliveryAddress;
            PlayerPrefs.SetString(AddressKey, address);
            if (parcel) parcel.Play(); // vendeur + vibration + son + vide le panier 3D
            else { if (vendor) vendor.PlayConfirm(); M5Haptics.Strong(); if (basket) basket.Clear(); }
            lines.Clear();
            if (DecoAR.Core.GameManager.Instance != null) DecoAR.Core.GameManager.Instance.SetCartCount(0);
            Refresh();
            Toast($"Commande confirmée ! Total payé : {Money(total)}\n<size=80%>Livraison : {address}</size>", 4.5f);
            OnOrderConfirmed?.Invoke(total);
        }

        // ---------- Adresse (GPS) ----------
        void UseMyLocation()
        {
            if (!location) location = gameObject.AddComponent<M5LocationService>();
            if (location.Busy) return;
            gpsBtn.interactable = false;
            gpsLabel.text = "Localisation…";
            location.FindAddress(status => addressStatus.text = status, (addr, err) =>
            {
                gpsBtn.interactable = true;
                gpsLabel.text = GpsText;
                if (!string.IsNullOrEmpty(addr)) { addressField.text = addr; Toast("Adresse ajoutée depuis votre position"); }
                addressStatus.text = err ?? "<color=#A8C79E>Adresse trouvée, vous pouvez la modifier.</color>";
            });
        }

        // ---------- Gyroscope ----------
        void ToggleGyro()
        {
            player.SetGyro(!player.useGyro);
            UpdateGyroLabel();
            Toast(player.useGyro ? "Gyroscope activé : tournez le téléphone pour regarder" : "Gyroscope désactivé : glissez à droite pour regarder");
        }

        void UpdateGyroLabel()
        {
            if (gyroLabel) gyroLabel.text = player.useGyro ? "Gyroscope : <b>ON</b>" : "Gyroscope : OFF";
        }

        void ApplySafeArea()
        {
            var r = Screen.safeArea;
            if (!safeArea || r == lastSafe || Screen.width == 0 || Screen.height == 0) return;
            lastSafe = r;
            safeArea.anchorMin = new Vector2(r.xMin / Screen.width, r.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(r.xMax / Screen.width, r.yMax / Screen.height);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }

        string Money(float v) => v.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',') + " " + currency;

        void Refresh()
        {
            if (!listText) return;
            var sb = new StringBuilder();
            if (lines.Count == 0) sb.Append("<color=#8C7866>Aucun article pour le moment.</color>");
            foreach (var l in lines)
                sb.Append($"{l.p.name} <color=#CCA14D>x{l.qty}</color><pos=72%>{Money(l.p.price * l.qty)}\n");
            listText.text = sb.ToString();
            int n = 0; foreach (var l in lines) n += l.qty;
            totalText.text = $"Total ({n} article{(n > 1 ? "s" : "")}) : <b>{Money(Total)}</b>";
            confirmBtn.interactable = lines.Count > 0;
        }

        void Toast(string msg, float duration = 2f)
        {
            if (!toastText) return;
            toastText.text = msg;
            toastUntil = Time.time + duration;
        }

        // ---------- Boucle ----------
        void Update()
        {
            ApplySafeArea();
            var cam = Camera.main;
            if (cam && zoneCenter)
            {
                var d = cam.transform.position - zoneCenter.position; d.y = 0;
                bool now = d.magnitude < zoneRadius;
                if (now != inZone)
                {
                    inZone = now;
                    hintText.text = inZone ? "Bienvenue à la caisse ! Vérifiez votre commande." : "Avancez vers le comptoir pour finaliser votre commande";
                    if (inZone && focusBasketOnEnter && player)
                        player.FocusOn(basket ? BasketCenter() : zoneCenter.position);
                }
            }

            float dt = Time.unscaledDeltaTime * 8f;
            panelGroup.alpha = Mathf.Lerp(panelGroup.alpha, inZone ? 1f : 0f, dt);
            panelGroup.interactable = panelGroup.blocksRaycasts = inZone;
            panel.anchoredPosition = Vector2.Lerp(panel.anchoredPosition, new Vector2(inZone ? -30f : 460f, 0), dt);
            toastGroup.alpha = Mathf.Lerp(toastGroup.alpha, Time.time < toastUntil ? 1f : 0f, dt);

            bool joy = player && player.JoystickActive;
            joyBase.gameObject.SetActive(joy);
            if (joy)
            {
                var canvas = joyBase.GetComponentInParent<Canvas>();
                float s = canvas.scaleFactor;
                joyBase.position = player.JoystickOrigin;
                joyKnob.anchoredPosition = player.JoystickVector * (player.JoystickRadius / s);
                joyBase.sizeDelta = Vector2.one * (player.JoystickRadius * 2f / s);
            }
        }

        Vector3 BasketCenter()
        {
            var rs = basket.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return basket.transform.position;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            return b.center;
        }

        // ---------- Construction UI ----------
        static void EnsureEventSystem()
        {
            if (EventSystem.current || FindAnyObjectByType<EventSystem>()) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<InputSystemUIInputModule>();
        }

        void BuildUI()
        {
            rounded = MakeSprite(64, 18, true);
            circle = MakeSprite(64, 32, false);

            var cgo = new GameObject("M5_CheckoutCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            // Zone sûre : rien sous l'encoche / les bords arrondis du téléphone
            safeArea = new GameObject("ZoneSure", typeof(RectTransform)).GetComponent<RectTransform>();
            safeArea.SetParent(cgo.transform, false);
            safeArea.anchorMin = Vector2.zero; safeArea.anchorMax = Vector2.one; safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            ApplySafeArea();
            var root = safeArea;

            // Indication en haut
            var hint = Box("Indication", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(900, 70), Dark);
            hintText = Label(hint, "Avancez vers le comptoir pour finaliser votre commande", 30, Cream, TextAlignmentOptions.Center);

            // Catalogue (démo) à gauche
            var cat = Box("Catalogue", root, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(420, 0), Dark);
            cat.pivot = new Vector2(0, 0.5f);
            Stack(cat, 12, 20);
            Label(cat, "<b>Catalogue</b>  <size=70%><color=#B8A48E>(ajouter au panier)</color></size>", 32, Cream, TextAlignmentOptions.Left, 50);
            foreach (var p in catalog)
            {
                var prod = p;
                Btn(cat, $"+  {p.name}<pos=68%><color=#F2D49A>{p.price:0} {currency}</color>", new Color(1, 1, 1, 0.08f), () => AddProduct(prod), 64, 26, TextAlignmentOptions.Left);
            }

            // Panneau commande à droite
            panel = Box("Commande", root, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(460, 0), new Vector2(560, 0), Dark);
            panel.pivot = new Vector2(1, 0.5f);
            panelGroup = panel.gameObject.AddComponent<CanvasGroup>(); panelGroup.alpha = 0;
            Stack(panel, 14, 26);
            Label(panel, "<b>Votre commande</b>", 40, Cream, TextAlignmentOptions.Left, 56);
            Box("Separateur", panel, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 3), Brass).GetComponent<LayoutElement>().preferredHeight = 3;
            listText = Label(panel, "", 28, Cream, TextAlignmentOptions.TopLeft, 200);
            totalText = Label(panel, "", 34, new Color(0.95f, 0.83f, 0.6f), TextAlignmentOptions.Left, 56);
            var beige = new Color(0.85f, 0.78f, 0.68f);
            Label(panel, "<b>Adresse de livraison</b>", 26, beige, TextAlignmentOptions.Left, 34);
            addressField = InputField(panel, "Saisissez votre adresse…", 92);
            addressField.text = PlayerPrefs.GetString(AddressKey, "");
            gpsBtn = Btn(panel, GpsText, new Color(0.33f, 0.45f, 0.55f), UseMyLocation, 58, 26, TextAlignmentOptions.Center);
            gpsLabel = gpsBtn.GetComponentInChildren<TextMeshProUGUI>();
            addressStatus = Label(panel, "", 22, beige, TextAlignmentOptions.Left, 28);
            confirmBtn = Btn(panel, "<b>Confirmer la commande</b>", Sage, ConfirmOrder, 76, 34, TextAlignmentOptions.Center);
            Btn(panel, "Vider le panier", Muted, ClearCart, 52, 28, TextAlignmentOptions.Center);

            // Toast
            var toast = Box("Toast", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 170), new Vector2(980, 80), new Color(0.30f, 0.42f, 0.29f, 0.95f));
            toastGroup = toast.gameObject.AddComponent<CanvasGroup>(); toastGroup.alpha = 0; toastGroup.blocksRaycasts = false;
            toastText = Label(toast, "", 32, Color.white, TextAlignmentOptions.Center);

            // Gyroscope (téléphone uniquement)
            if (player && player.GyroAvailable)
            {
                var bar = Box("Gyro", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -30), new Vector2(320, 0), Color.clear);
                bar.pivot = new Vector2(0, 1);
                bar.GetComponent<Image>().enabled = false;
                Stack(bar, 10, 0);
                gyroLabel = Btn(bar, "", Dark, ToggleGyro, 62, 26, TextAlignmentOptions.Center).GetComponentInChildren<TextMeshProUGUI>();
                Btn(bar, "Recentrer la vue", Dark, () => player.Recenter(), 54, 24, TextAlignmentOptions.Center);
                UpdateGyroLabel();
            }

            // Aide contrôles
            string helpText = Application.isMobilePlatform
                ? "Glisser à gauche pour avancer  •  à droite pour tourner  •  tournez le téléphone pour regarder"
                : "PC : ZQSD / flèches + clic droit pour regarder";
            var help = Label(root, helpText, 22, new Color(1, 1, 1, 0.75f), TextAlignmentOptions.Center);
            var hr = help.rectTransform; hr.anchorMin = new Vector2(0.5f, 0); hr.anchorMax = new Vector2(0.5f, 0); hr.anchoredPosition = new Vector2(0, 40); hr.sizeDelta = new Vector2(1400, 40);
            help.raycastTarget = false;

            // Joystick visuel
            joyBase = Box("Joystick", root, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(200, 200), new Color(1, 1, 1, 0.15f));
            joyBase.GetComponent<Image>().sprite = circle; joyBase.GetComponent<Image>().type = Image.Type.Simple; joyBase.GetComponent<Image>().raycastTarget = false;
            joyKnob = Box("Knob", joyBase, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 90), new Color(1, 1, 1, 0.5f));
            joyKnob.GetComponent<Image>().sprite = circle; joyKnob.GetComponent<Image>().type = Image.Type.Simple; joyKnob.GetComponent<Image>().raycastTarget = false;
            joyBase.gameObject.SetActive(false);
        }

        RectTransform Box(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>(); img.color = c; img.sprite = rounded; img.type = Image.Type.Sliced;
            return rt;
        }

        TMP_InputField InputField(Transform parent, string placeholder, float height)
        {
            var rt = Box("Adresse", parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0.1f));
            rt.GetComponent<LayoutElement>().preferredHeight = height;
            rt.gameObject.SetActive(false); // références posées avant OnEnable du champ
            var area = new GameObject("Zone", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            area.SetParent(rt, false);
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = new Vector2(18, 8); area.offsetMax = new Vector2(-18, -8);
            var ph = Label(area, placeholder, 24, new Color(1, 1, 1, 0.4f), TextAlignmentOptions.TopLeft);
            ph.fontStyle = FontStyles.Italic;
            var txt = Label(area, "", 24, Cream, TextAlignmentOptions.TopLeft);
            foreach (var t in new[] { ph, txt }) { t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero; t.richText = false; }
            var f = rt.gameObject.AddComponent<TMP_InputField>();
            f.textViewport = area; f.textComponent = txt; f.placeholder = ph;
            f.targetGraphic = rt.GetComponent<Image>();
            f.lineType = TMP_InputField.LineType.MultiLineSubmit;
            f.characterLimit = 160;
            f.customCaretColor = true; f.caretColor = Brass; f.caretWidth = 3;
            f.selectionColor = new Color(0.8f, 0.63f, 0.3f, 0.35f);
            rt.gameObject.SetActive(true);
            return f;
        }

        static void Stack(RectTransform rt, float spacing, int pad)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(pad, pad, pad, pad);
            v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
            var f = rt.gameObject.AddComponent<ContentSizeFitter>(); f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static TextMeshProUGUI Label(Transform parent, string text, float size, Color c, TextAlignmentOptions align, float height = -1)
        {
            var go = new GameObject("Texte", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(16, 0); rt.offsetMax = new Vector2(-16, 0);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = c; t.alignment = align; t.richText = true; t.raycastTarget = false;
            if (height > 0) go.GetComponent<LayoutElement>().preferredHeight = height;
            return t;
        }

        Button Btn(Transform parent, string text, Color c, Action onClick, float height, float fontSize, TextAlignmentOptions align)
        {
            var rt = Box("Bouton", parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, c);
            rt.GetComponent<LayoutElement>().preferredHeight = height;
            var b = rt.gameObject.AddComponent<Button>();
            var cb = b.colors; cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f); cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f); b.colors = cb;
            b.onClick.AddListener(() => onClick());
            var t = Label(rt, text, fontSize, Color.white, align);
            t.rectTransform.offsetMin = new Vector2(22, 0); t.rectTransform.offsetMax = new Vector2(-22, 0);
            return b;
        }

        static Sprite MakeSprite(int size, int radius, bool sliced)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius), cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            var border = sliced ? new Vector4(radius, radius, radius, radius) : Vector4.zero;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
        }
    }
}
