using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DecoAR.M5
{
    /// HUD de la boutique (construit par code), pensé mobile et minimaliste :
    /// à gauche le profil + les réglages, à droite le catalogue + le panier ; chaque icône ouvre un tiroir de son côté.
    /// Une pastille de notification en haut au centre, un bouton « Payer » au comptoir, joystick tactile.
    public class M5CheckoutUI : MonoBehaviour
    {
        [Serializable]
        public class Product
        {
            public string name; public float price; public Color color = Color.white; public GameObject prefab;
            public Product(string n, float p, Color c) { name = n; price = p; color = c; }
        }

        public enum Page { Aucune = -1, Catalogue, Panier, Reglages, Profil }

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
        [Tooltip("Paiement Stripe (vide = commande confirmée sans paiement, mode démo).")]
        public M5StripePayment payment;
        [Tooltip("Bouton « Se connecter » du profil (à brancher sur le module de connexion).")]
        public UnityEvent onProfileRequested = new UnityEvent();
        [Tooltip("D'où s'envole l'argent après le paiement (la caisse enregistreuse).")]
        public Transform moneyOrigin;

        public event Action<float> OnOrderConfirmed;
        public float Total { get { float t = 0; foreach (var l in lines) t += l.p.price * l.qty; return t; } }
        public int ItemCount { get { int n = 0; foreach (var l in lines) n += l.qty; return n; } }
        public string DeliveryAddress => addressField ? addressField.text.Trim() : "";
        public Page OpenPage => page;

        class Line { public Product p; public int qty; public readonly List<GameObject> items = new List<GameObject>(); }
        readonly List<Line> lines = new List<Line>();

        // Palette boutique déco
        static readonly Color Glass = new Color(0.12f, 0.10f, 0.08f, 0.72f);
        static readonly Color GlassStrong = new Color(0.12f, 0.10f, 0.08f, 0.94f);
        static readonly Color Cream = new Color(0.98f, 0.95f, 0.89f);
        static readonly Color Soft = new Color(0.72f, 0.64f, 0.56f);
        static readonly Color Sage = new Color(0.45f, 0.58f, 0.43f);
        static readonly Color Brass = new Color(0.82f, 0.65f, 0.32f);
        static readonly Color Ink = new Color(0.16f, 0.13f, 0.10f);
        static readonly Color Tile = new Color(1f, 1f, 1f, 0.07f);
        static readonly Color Muted = new Color(0.55f, 0.47f, 0.40f);

        const float DrawerWidth = 620f, DockTop = 24f, DockHeight = 84f;
        const string AddressKey = "M5_Adresse";
        const string GpsText = "Utiliser ma position";

        Sprite rounded, circle, pill, ring, shadow;
        Page page = Page.Aucune;
        RectTransform safeArea, drawer, chip, cta, joyBase, joyKnob, catalogContent, cartContent;
        CanvasGroup drawerGroup, dimGroup, chipGroup, ctaGroup, payGroup;
        readonly RectTransform[] pages = new RectTransform[4];
        readonly Image[] dockBg = new Image[4], dockIcon = new Image[4], dockRing = new Image[4];
        bool drawerLeft;
        TextMeshProUGUI profileName;
        Image profileAvatar, profileAvatarIcon;
        const string ProfileLabel = "<b>Invité</b>\n<size=72%>Mon profil</size>";
        TextMeshProUGUI drawerTitle, chipText, ctaText, badgeText, totalText, confirmText, addressStatus, gpsLabel, payStatus, gyroSub, profileAddress;
        Image chipDot;
        RectTransform badge, gyroKnob, musicKnob, cartButton;
        Image gyroTrack, musicTrack;
        TextMeshProUGUI musicSub;
        bool cartBlinking;
        static readonly Color RingIdle = new Color(1, 1, 1, 0.16f), RingOn = new Color(1f, 0.88f, 0.6f, 0.9f);
        Button confirmBtn, gpsBtn, recenterBtn, clearBtn;
        TMP_InputField addressField;
        M5LocationService location;
        Rect lastSafe;
        bool inZone;
        float chipUntil, badgePop;
        static readonly string[] Titles = { "Catalogue", "Mon panier", "Réglages", "Profil" };

        void Start()
        {
            if (!basket) basket = Basket3D.Instance;
            if (!vendor || !vendor.isActiveAndEnabled) vendor = VendorNPC.Instance;
            if (!payment) payment = GetComponent<M5StripePayment>();
            EnsureEventSystem();
            BuildUI();
            ImportSharedCart();
            Refresh();
            Toast(Application.isMobilePlatform
                ? "Glissez à gauche pour avancer, à droite pour tourner"
                : "ZQSD pour avancer, clic droit pour regarder", 5f);
        }

        // Articles ajoutés depuis le catalogue M3 ou l'AR M4 (GameManager)
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
            if (l == null) { l = new Line { p = p }; lines.Add(l); }
            l.qty++;
            if (basket) l.items.Add(basket.AddItem(p.prefab, p.color));
            if (vendor) vendor.ShowBubble("Excellent choix !", 2f);
            badgePop = 1f;
            M5ShopAudio.Item();
            Toast($"{p.name} ajouté au panier", 2f, true);
            Refresh();
        }

        /// Point d'entrée pour les autres modules.
        public void AddProduct(string name, float price, Color color, GameObject prefab = null)
            => AddProduct(new Product(name, price, color) { prefab = prefab });

        public void RemoveOne(Product p)
        {
            var l = lines.Find(x => x.p == p);
            if (l == null) return;
            l.qty--;
            if (l.items.Count > 0)
            {
                var go = l.items[l.items.Count - 1];
                l.items.RemoveAt(l.items.Count - 1);
                if (basket) basket.RemoveItem(go);
            }
            if (l.qty <= 0) lines.Remove(l);
            Refresh();
        }

        public void ClearCart()
        {
            lines.Clear();
            if (basket) basket.Clear();
            Refresh();
        }

        public void ConfirmOrder()
        {
            if (lines.Count == 0) { Toast("Votre panier est vide"); return; }
            if (!inZone && zoneCenter) { Toast("Rendez-vous au comptoir pour payer"); return; }
            if (string.IsNullOrEmpty(DeliveryAddress))
            {
                Toast("Ajoutez votre adresse de livraison", 3f);
                ShowPage(Page.Panier);
                if (addressField) addressField.Select();
                return;
            }
            if (parcel && parcel.IsPlaying) return;
            float total = Total;
            string address = DeliveryAddress;
            PlayerPrefs.SetString(AddressKey, address);
            if (payment && payment.IsConfigured) { StartPayment(total, address); return; }
            FinishOrder(total, address, false);
        }

        // ---------- Paiement Stripe ----------
        void StartPayment(float total, string address)
        {
            if (payment.Busy) { ShowPayment(true); return; }
            var items = new List<(string, float, int)>();
            foreach (var l in lines) items.Add((l.p.name, l.p.price, l.qty));
            ShowPayment(true);
            payment.Pay(items, address, s => payStatus.text = s, (paid, msg) =>
            {
                ShowPayment(false);
                if (paid) FinishOrder(total, address, true);
                else Toast(msg, 3f);
            });
        }

        void ShowPayment(bool show)
        {
            if (!payGroup) return;
            payGroup.alpha = show ? 1f : 0f;
            payGroup.interactable = payGroup.blocksRaycasts = show;
        }

        void FinishOrder(float total, string address, bool paid)
        {
            ShowPage(Page.Aucune); // on dégage l'écran pour voir Aj et le colis
            M5ShopAudio.Register();
            MoneyFlies();
            if (parcel) parcel.Play(); // vendeur + vibration + applaudissements + vide le panier 3D
            else { if (vendor) vendor.PlayConfirm(); M5Haptics.Strong(); if (basket) basket.Clear(); }
            lines.Clear();
            if (DecoAR.Core.GameManager.Instance != null) DecoAR.Core.GameManager.Instance.SetCartCount(0);
            Refresh();
            Toast($"{(paid ? "Paiement accepté" : "Commande confirmée")} · {Money(total)}\n<size=80%><color=#D9CDBF>Livraison : {address}</color></size>", 5f, true);
            OnOrderConfirmed?.Invoke(total);
        }

        /// Billets et pièces qui s'envolent de la caisse enregistreuse vers le joueur.
        void MoneyFlies()
        {
            var origin = moneyOrigin ? moneyOrigin : basket ? basket.transform : zoneCenter;
            if (!origin) return;
            var b = new Bounds(origin.position, Vector3.zero); bool first = true;
            foreach (var r in origin.GetComponentsInChildren<Renderer>()) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
            var top = new Vector3(b.center.x, b.max.y + 0.05f, b.center.z);
            var cam = Camera.main;
            M5MoneyBurst.Spawn(top, cam ? cam.transform.position - top : Vector3.forward, b.min.y);
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
                if (!string.IsNullOrEmpty(addr)) { addressField.text = addr; Toast("Adresse ajoutée depuis votre position", 2.5f, true); }
                addressStatus.text = err ?? "<color=#A8C79E>Adresse trouvée, vous pouvez la modifier.</color>";
            });
        }

        // ---------- Profil ----------
        void Login()
        {
            if (onProfileRequested.GetPersistentEventCount() > 0) onProfileRequested.Invoke();
            else Toast("La connexion arrive bientôt");
        }

        // ---------- Gyroscope ----------
        void ToggleGyro()
        {
            if (!player || !player.GyroAvailable) { Toast("Le gyroscope est disponible sur téléphone"); return; }
            player.SetGyro(!player.useGyro);
            Toast(player.useGyro ? "Tournez le téléphone pour regarder" : "Glissez à droite pour regarder");
            RefreshSettings();
        }

        void ToggleMusic()
        {
            var a = M5ShopAudio.Instance;
            if (!a) { Toast("Aucune musique installée (menu 11)"); return; }
            a.MusicOn = !a.MusicOn;
            RefreshSettings();
        }

        void RefreshSettings()
        {
            bool music = M5ShopAudio.Instance && M5ShopAudio.Instance.MusicOn;
            if (musicTrack) musicTrack.color = music ? Sage : new Color(1, 1, 1, 0.18f);
            if (musicKnob) musicKnob.anchoredPosition = new Vector2(music ? 22f : -22f, 0);
            if (musicSub) musicSub.text = music ? "Ambiance calme de la boutique" : "Coupée";
            bool avail = player && player.GyroAvailable, on = avail && player.useGyro;
            if (gyroTrack) gyroTrack.color = on ? Sage : new Color(1, 1, 1, 0.18f);
            if (gyroKnob) gyroKnob.anchoredPosition = new Vector2(on ? 22f : -22f, 0);
            if (gyroSub) gyroSub.text = avail ? "Tournez le téléphone pour regarder" : "Disponible sur téléphone";
            if (recenterBtn) recenterBtn.interactable = on;
        }

        // ---------- Navigation ----------
        /// Ouvre une page du tiroir (Aucune = fermer). Re-toucher l'icône ouverte la referme.
        public void ShowPage(Page p)
        {
            if (p != Page.Aucune) PlaceDrawer(p == Page.Reglages || p == Page.Profil);
            page = p;
            for (int i = 0; i < pages.Length; i++)
            {
                bool on = (int)p == i;
                if (pages[i]) pages[i].gameObject.SetActive(on);
            }
            RefreshDock();
            bool prof = p == Page.Profil;
            if (profileName) profileName.text = prof ? ProfileLabel : ProfileLabel.Replace("<size=72%>", "<size=72%><color=#B8A48E>").Replace("</size>", "</color></size>");
            if (profileName) profileName.color = prof ? Ink : Cream;
            if (profileAvatar) profileAvatar.color = prof ? Ink : Brass;
            if (profileAvatarIcon) profileAvatarIcon.color = prof ? Brass : Ink;
            if (p != Page.Aucune) drawerTitle.text = Titles[(int)p];
            if (p == Page.Reglages) RefreshSettings();
            if (p == Page.Profil) profileAddress.text = string.IsNullOrEmpty(PlayerPrefs.GetString(AddressKey, "")) ? "Aucune adresse enregistrée" : PlayerPrefs.GetString(AddressKey, "");
            if (p == Page.Aucune && EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }

        void RefreshDock()
        {
            for (int i = 0; i < dockBg.Length; i++)
            {
                bool on = (int)page == i;
                if (dockBg[i]) dockBg[i].color = on ? Brass : Glass;
                if (dockIcon[i]) dockIcon[i].color = on ? Ink : Cream;
                if (dockRing[i]) dockRing[i].color = on ? RingOn : RingIdle;
            }
            if (cartButton) cartButton.localScale = Vector3.one;
        }

        void Toggle(Page p) => ShowPage(page == p ? Page.Aucune : p);

        /// Le tiroir sort du côté de l'icône touchée (gauche : profil / réglages, droite : catalogue / panier).
        void PlaceDrawer(bool left)
        {
            if (left == drawerLeft && drawer.anchorMin.x == (left ? 0f : 1f)) return;
            drawerLeft = left;
            float x = left ? 0f : 1f;
            drawer.anchorMin = new Vector2(x, 0); drawer.anchorMax = new Vector2(x, 1); drawer.pivot = new Vector2(x, 0.5f);
            drawer.sizeDelta = new Vector2(DrawerWidth, drawer.sizeDelta.y);
            drawer.offsetMin = new Vector2(drawer.offsetMin.x, 24f);
            drawer.offsetMax = new Vector2(drawer.offsetMax.x, -(DockTop + DockHeight + 22f));
            drawer.anchoredPosition = new Vector2(HiddenX, drawer.anchoredPosition.y);
            drawerGroup.alpha = 0f;
        }

        float HiddenX => drawerLeft ? -(DrawerWidth + 40f) : DrawerWidth + 40f;

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
            if (!cartContent) return;
            int n = ItemCount;
            badge.gameObject.SetActive(n > 0);
            badgeText.text = n > 9 ? "9+" : n.ToString();

            // Lignes du panier
            for (int i = cartContent.childCount - 1; i >= 0; i--) Destroy(cartContent.GetChild(i).gameObject);
            if (lines.Count == 0)
            {
                var empty = Label(cartContent, "Votre panier est vide.\n<size=80%><color=#B8A48E>Ajoutez des articles depuis le catalogue.</color></size>", 26, Cream, TextAlignmentOptions.Center, 110);
                empty.textWrappingMode = TextWrappingModes.Normal;
                var browse = Btn(cartContent, "Parcourir le catalogue", Tile, () => ShowPage(Page.Catalogue), 60, 24, TextAlignmentOptions.Center);
                browse.GetComponentInChildren<TextMeshProUGUI>().color = Brass;
            }
            foreach (var l in lines) CartRow(l);

            totalText.text = $"<color=#B8A48E>Total · {n} article{(n > 1 ? "s" : "")}</color><pos=55%><b>{Money(Total)}</b>";
            bool canPay = lines.Count > 0 && (inZone || !zoneCenter);
            confirmBtn.interactable = canPay;
            clearBtn.gameObject.SetActive(lines.Count > 0);
            string verb = payment && payment.IsConfigured ? "Payer" : "Confirmer";
            confirmText.text = lines.Count == 0 ? "Panier vide"
                : canPay ? $"<b>{verb} · {Money(Total)}</b>" : "Rendez-vous au comptoir pour payer";
            ctaText.text = $"<b>{verb}</b>  ·  {Money(Total)}";
        }

        void CartRow(Line l)
        {
            var row = Row(cartContent, 92, 14, Tile);
            Swatch(row, l.p.color);
            var name = Label(row, $"<b>{l.p.name}</b>\n<size=78%><color=#B8A48E>{Money(l.p.price)}</color></size>", 25, Cream, TextAlignmentOptions.MidlineLeft);
            name.GetComponent<LayoutElement>().flexibleWidth = 1;
            name.textWrappingMode = TextWrappingModes.NoWrap; name.overflowMode = TextOverflowModes.Ellipsis;
            var p = l.p;
            IconButton(row, M5Icons.Kind.Moins, 54, new Color(1, 1, 1, 0.12f), Cream, () => RemoveOne(p));
            var q = Label(row, l.qty.ToString(), 28, Cream, TextAlignmentOptions.Center);
            q.GetComponent<LayoutElement>().preferredWidth = 34;
            IconButton(row, M5Icons.Kind.Plus, 54, new Color(1, 1, 1, 0.12f), Cream, () => AddProduct(p));
        }

        /// Pastille de notification en haut au centre (remplace indication, aide et toasts).
        void Toast(string msg, float duration = 2.5f, bool success = false)
        {
            if (!chipText) return;
            chipText.text = msg;
            var pref = chipText.GetPreferredValues(msg, 860f, 0f);
            chip.sizeDelta = new Vector2(Mathf.Min(pref.x, 860f) + 94f, Mathf.Max(64f, pref.y + 30f));
            chipDot.color = success ? new Color(0.62f, 0.80f, 0.58f) : Brass;
            chipUntil = Time.time + duration;
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
                    if (inZone)
                    {
                        Toast(lines.Count > 0 ? "Bienvenue à la caisse ! Touchez Payer pour finaliser" : "Bienvenue à la caisse ! Votre panier est vide", 3.5f);
                        if (focusBasketOnEnter && player) player.FocusOn(basket ? BasketCenter() : zoneCenter.position);
                    }
                    Refresh();
                }
            }

            float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            bool open = page != Page.Aucune;
            drawer.anchoredPosition = new Vector2(Mathf.Lerp(drawer.anchoredPosition.x, open ? (drawerLeft ? 24f : -24f) : HiddenX, k), drawer.anchoredPosition.y);
            drawerGroup.alpha = Mathf.Lerp(drawerGroup.alpha, open ? 1f : 0f, k);
            drawerGroup.interactable = drawerGroup.blocksRaycasts = open;
            dimGroup.alpha = Mathf.Lerp(dimGroup.alpha, open ? 1f : 0f, k);
            dimGroup.blocksRaycasts = open;

            bool chipOn = Time.time < chipUntil;
            chipGroup.alpha = Mathf.Lerp(chipGroup.alpha, chipOn ? 1f : 0f, k);
            chip.anchoredPosition = new Vector2(0, Mathf.Lerp(chip.anchoredPosition.y, chipOn ? -DockTop : 10f, k));

            bool showCta = inZone && lines.Count > 0 && !open && !(payGroup && payGroup.blocksRaycasts);
            ctaGroup.alpha = Mathf.Lerp(ctaGroup.alpha, showCta ? 1f : 0f, k);
            ctaGroup.blocksRaycasts = ctaGroup.interactable = showCta;
            cta.localScale = Vector3.one * (showCta ? 1f + 0.025f * Mathf.Sin(Time.time * 3.2f) : 0.96f);

            // Au comptoir avec des articles : le bouton Panier clignote (« touchez-moi pour payer »)
            bool blink = inZone && lines.Count > 0 && page != Page.Panier && !(payGroup && payGroup.blocksRaycasts);
            if (blink && cartButton)
            {
                float s = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                int c = (int)Page.Panier;
                dockBg[c].color = Color.Lerp(Glass, Brass, s);
                dockIcon[c].color = Color.Lerp(Cream, Ink, s);
                dockRing[c].color = Color.Lerp(RingIdle, RingOn, s);
                cartButton.localScale = Vector3.one * (1f + 0.1f * s);
                cartBlinking = true;
            }
            else if (cartBlinking) { cartBlinking = false; RefreshDock(); }

            badgePop = Mathf.MoveTowards(badgePop, 0f, Time.unscaledDeltaTime * 3f);
            badge.localScale = Vector3.one * (1f + 0.45f * Mathf.Sin(badgePop * Mathf.PI) * badgePop);

            bool joy = player && player.JoystickActive;
            joyBase.gameObject.SetActive(joy);
            if (joy)
            {
                float s = joyBase.GetComponentInParent<Canvas>().scaleFactor;
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

        // ---------- Construction ----------
        static void EnsureEventSystem()
        {
            if (EventSystem.current || FindAnyObjectByType<EventSystem>()) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<InputSystemUIInputModule>();
        }

        void BuildUI()
        {
            rounded = MakeSprite(64, 22, true);
            circle = MakeSprite(64, 32, false);
            pill = MakeSprite(64, 32, true);
            ring = MakeRing(64, 32, 2.5f);
            shadow = MakeShadow(64);

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

            // Joystick visuel (sous tout le reste)
            joyBase = Box("Joystick", root, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(200, 200), new Color(1, 1, 1, 0.14f));
            var jb = joyBase.GetComponent<Image>(); jb.sprite = circle; jb.type = Image.Type.Simple; jb.raycastTarget = false;
            joyKnob = Box("Knob", joyBase, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84, 84), new Color(1, 1, 1, 0.5f));
            var jk = joyKnob.GetComponent<Image>(); jk.sprite = circle; jk.type = Image.Type.Simple; jk.raycastTarget = false;
            joyBase.gameObject.SetActive(false);

            // Voile : toucher hors du tiroir le referme
            var dim = Box("Voile", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.22f));
            dim.offsetMin = dim.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().sprite = null;
            dim.gameObject.AddComponent<Button>().onClick.AddListener(() => ShowPage(Page.Aucune));
            dimGroup = dim.gameObject.AddComponent<CanvasGroup>(); dimGroup.alpha = 0; dimGroup.blocksRaycasts = false;

            BuildDrawer(root);
            BuildDock(root);
            BuildChip(root);
            BuildCta(root);
            BuildPayment(root);
            ShowPage(Page.Aucune);
        }

        void BuildDock(RectTransform root)
        {
            // Gauche : profil + réglages
            var left = Cluster(root, true);
            ProfilePill(left);
            FloatButton(left, M5Icons.Kind.Reglages, Page.Reglages);

            // Droite : catalogue + panier
            var right = Cluster(root, false);
            FloatButton(right, M5Icons.Kind.Catalogue, Page.Catalogue);
            var cart = FloatButton(right, M5Icons.Kind.Panier, Page.Panier);
            cartButton = cart;

            // Pastille du nombre d'articles sur l'icône Panier
            badge = Box("Badge", cart, Vector2.one, Vector2.one, new Vector2(-8, -8), new Vector2(32, 32), new Color(0.82f, 0.36f, 0.27f));
            var bi = badge.GetComponent<Image>(); bi.sprite = circle; bi.type = Image.Type.Simple; bi.raycastTarget = false;
            badgeText = Label(badge, "", 18, Color.white, TextAlignmentOptions.Center);
            badgeText.fontStyle = FontStyles.Bold;
            badgeText.rectTransform.offsetMin = badgeText.rectTransform.offsetMax = Vector2.zero;
        }

        RectTransform Cluster(RectTransform root, bool left)
        {
            var rt = new GameObject(left ? "HUD_Gauche" : "HUD_Droite", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(root, false);
            float x = left ? 0f : 1f;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(x, 1);
            rt.anchoredPosition = new Vector2(left ? 24f : -24f, -DockTop);
            rt.sizeDelta = new Vector2(0, DockHeight);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14; h.childAlignment = left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            h.childControlWidth = h.childControlHeight = false; h.childForceExpandWidth = h.childForceExpandHeight = false;
            rt.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rt;
        }

        /// Pastille de verre : ombre douce + fond + liseré clair (calques hors mise en page).
        Image Glassy(RectTransform rt, Color fill, out Image ringImg)
        {
            var sh = Layer(rt, "Ombre", shadow, new Color(0, 0, 0, 0.38f), raycast: false);
            sh.rectTransform.offsetMin = new Vector2(-14, -18); sh.rectTransform.offsetMax = new Vector2(14, 8);
            var bg = Layer(rt, "Fond", pill, fill, raycast: true);
            ringImg = Layer(rt, "Liseré", ring, new Color(1, 1, 1, 0.16f), raycast: false);
            return bg;
        }

        Image Layer(RectTransform parent, string name, Sprite s, Color c, bool raycast)
        {
            var img = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
            img.transform.SetParent(parent, false);
            img.GetComponent<LayoutElement>().ignoreLayout = true;
            var r = img.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            img.sprite = s; img.type = Image.Type.Sliced; img.color = c; img.raycastTarget = raycast;
            return img;
        }

        RectTransform FloatButton(Transform parent, M5Icons.Kind k, Page p)
        {
            var rt = new GameObject("Bouton_" + k, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = Vector2.one * DockHeight;
            var bg = Glassy(rt, Glass, out var rg);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            var cb = b.colors; cb.pressedColor = new Color(0.75f, 0.75f, 0.75f); b.colors = cb;
            b.onClick.AddListener(() => Toggle(p));
            var icon = Icon(rt, k, DockHeight * 0.46f, Cream);
            icon.GetComponent<LayoutElement>().ignoreLayout = true;
            dockBg[(int)p] = bg; dockIcon[(int)p] = icon; dockRing[(int)p] = rg;
            return rt;
        }

        /// Carte profil : avatar + nom (sera remplie par le module de connexion).
        void ProfilePill(Transform parent)
        {
            var rt = new GameObject("Profil", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(0, DockHeight);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(10, 30, 10, 10); h.spacing = 14; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            rt.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var bg = Glassy(rt, Glass, out var rg);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            b.onClick.AddListener(() => Toggle(Page.Profil));

            var av = Box("Avatar", rt, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Brass);
            var ai = av.GetComponent<Image>(); ai.sprite = circle; ai.type = Image.Type.Simple; ai.raycastTarget = false;
            var ale = av.GetComponent<LayoutElement>(); ale.preferredWidth = ale.preferredHeight = DockHeight - 20f;
            profileAvatar = ai;
            profileAvatarIcon = Icon(av, M5Icons.Kind.Profil, (DockHeight - 20f) * 0.62f, Ink);
            profileAvatarIcon.GetComponent<LayoutElement>().ignoreLayout = true;

            profileName = Label(rt, "<b>Invité</b>\n<size=72%><color=#B8A48E>Mon profil</color></size>", 23, Cream, TextAlignmentOptions.MidlineLeft);
            profileName.textWrappingMode = TextWrappingModes.NoWrap;
            dockBg[(int)Page.Profil] = bg; dockRing[(int)Page.Profil] = rg;
        }

        void BuildDrawer(RectTransform root)
        {
            drawer = Box("Tiroir", root, new Vector2(1, 0), new Vector2(1, 1), new Vector2(DrawerWidth + 40f, 0), new Vector2(DrawerWidth, 0), GlassStrong);
            drawer.pivot = new Vector2(1, 0.5f);
            drawer.offsetMin = new Vector2(drawer.offsetMin.x, 24f);
            drawer.offsetMax = new Vector2(drawer.offsetMax.x, -(DockTop + DockHeight + 22f));
            drawerGroup = drawer.gameObject.AddComponent<CanvasGroup>(); drawerGroup.alpha = 0;
            var dsh = Layer(drawer, "Ombre", shadow, new Color(0, 0, 0, 0.45f), raycast: false);
            dsh.rectTransform.offsetMin = new Vector2(-30, -34); dsh.rectTransform.offsetMax = new Vector2(30, 20);
            dsh.transform.SetAsFirstSibling();
            drawer.GetComponent<Image>().color = Color.clear; // le fond est un calque, au-dessus de l'ombre
            Layer(drawer, "Fond", pill, GlassStrong, raycast: false).transform.SetSiblingIndex(1);
            Layer(drawer, "Liseré", ring, new Color(1, 1, 1, 0.10f), raycast: false);
            var v = Column(drawer, 14, 26);
            v.childForceExpandHeight = false;

            var head = Row(drawer, 64, 10, Color.clear);
            drawerTitle = Label(head, "", 36, Cream, TextAlignmentOptions.MidlineLeft);
            drawerTitle.fontStyle = FontStyles.Bold;
            drawerTitle.GetComponent<LayoutElement>().flexibleWidth = 1;
            IconButton(head, M5Icons.Kind.Fermer, 56, new Color(1, 1, 1, 0.1f), Cream, () => ShowPage(Page.Aucune));
            Separator(drawer);

            pages[0] = PageRoot("Page_Catalogue"); BuildCatalogPage(pages[0]);
            pages[1] = PageRoot("Page_Panier"); BuildCartPage(pages[1]);
            pages[2] = PageRoot("Page_Reglages"); BuildSettingsPage(pages[2]);
            pages[3] = PageRoot("Page_Profil"); BuildProfilePage(pages[3]);
        }

        RectTransform PageRoot(string name)
        {
            var rt = new GameObject(name, typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            rt.SetParent(drawer, false);
            rt.GetComponent<LayoutElement>().flexibleHeight = 1;
            Column(rt, 12, 0);
            return rt;
        }

        void BuildCatalogPage(RectTransform pg)
        {
            Scroll(pg, 200, out catalogContent);
            foreach (var prod in catalog)
            {
                var p = prod;
                var row = Row(catalogContent, 96, 16, Tile);
                Swatch(row, p.color);
                var name = Label(row, $"<b>{p.name}</b>\n<size=78%><color=#B8A48E>{p.price:0} {currency}</color></size>", 26, Cream, TextAlignmentOptions.MidlineLeft);
                name.GetComponent<LayoutElement>().flexibleWidth = 1;
                name.textWrappingMode = TextWrappingModes.NoWrap; name.overflowMode = TextOverflowModes.Ellipsis;
                IconButton(row, M5Icons.Kind.Plus, 62, Brass, Ink, () => AddProduct(p));
            }
        }

        void BuildCartPage(RectTransform pg)
        {
            // Articles, total et adresse défilent ensemble ; le bouton Payer reste fixe en bas (écrans peu hauts)
            Scroll(pg, 120, out var body);
            cartContent = new GameObject("Articles", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            cartContent.SetParent(body, false);
            Column(cartContent, 10, 0);
            totalText = Label(body, "", 28, Cream, TextAlignmentOptions.MidlineLeft, 50);
            Separator(body);

            Label(body, "<b>Adresse de livraison</b>", 22, Soft, TextAlignmentOptions.MidlineLeft, 34);
            addressField = InputField(body, "Saisissez votre adresse…", 84);
            addressField.text = PlayerPrefs.GetString(AddressKey, "");
            var gpsRow = Row(body, 52, 8, Color.clear);
            gpsBtn = gpsRow.gameObject.AddComponent<Button>();
            gpsBtn.onClick.AddListener(UseMyLocation);
            gpsBtn.targetGraphic = gpsRow.GetComponent<Image>();
            Icon(gpsRow, M5Icons.Kind.Position, 34, Brass);
            gpsLabel = Label(gpsRow, GpsText, 24, Brass, TextAlignmentOptions.MidlineLeft);
            gpsLabel.GetComponent<LayoutElement>().flexibleWidth = 1;
            addressStatus = Label(body, "", 20, Soft, TextAlignmentOptions.MidlineLeft, 26);

            confirmBtn = Btn(pg, "", Sage, ConfirmOrder, 78, 28, TextAlignmentOptions.Center);
            confirmText = confirmBtn.GetComponentInChildren<TextMeshProUGUI>();
            clearBtn = Btn(pg, "Vider le panier", new Color(1, 1, 1, 0f), ClearCart, 36, 22, TextAlignmentOptions.Center);
            clearBtn.GetComponentInChildren<TextMeshProUGUI>().color = Soft;
        }

        void BuildSettingsPage(RectTransform pg)
        {
            SwitchRow(pg, "Vue au gyroscope", ToggleGyro, out gyroSub, out gyroTrack, out gyroKnob);
            SwitchRow(pg, "Musique d'ambiance", ToggleMusic, out musicSub, out musicTrack, out musicKnob);

            // Recentrer
            var rc = Row(pg, 84, 16, Tile);
            recenterBtn = rc.gameObject.AddComponent<Button>();
            recenterBtn.targetGraphic = rc.GetComponent<Image>();
            recenterBtn.onClick.AddListener(() => { if (player) { player.Recenter(); Toast("Vue recentrée"); } });
            Icon(rc, M5Icons.Kind.Recentrer, 40, Cream);
            Label(rc, "Recentrer la vue", 26, Cream, TextAlignmentOptions.MidlineLeft).GetComponent<LayoutElement>().flexibleWidth = 1;

            // Aide
            var help = Label(pg, "<b>Commandes</b>\n" +
                "<color=#D9CDBF>• Moitié gauche : glisser pour avancer\n" +
                "• Moitié droite : glisser pour tourner\n" +
                "• Tournez le téléphone pour regarder\n" +
                "• PC : ZQSD + clic droit</color>", 22, Soft, TextAlignmentOptions.TopLeft, 170);
            help.textWrappingMode = TextWrappingModes.Normal;
            RefreshSettings();
        }

        void SwitchRow(RectTransform pg, string title, Action onToggle, out TextMeshProUGUI sub, out Image track, out RectTransform knob)
        {
            var row = Row(pg, 96, 16, Tile);
            var txt = new GameObject("Textes", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            txt.SetParent(row, false);
            txt.GetComponent<LayoutElement>().flexibleWidth = 1;
            Column(txt, 2, 0).childAlignment = TextAnchor.MiddleLeft;
            Label(txt, $"<b>{title}</b>", 26, Cream, TextAlignmentOptions.MidlineLeft, 34);
            sub = Label(txt, "", 20, Soft, TextAlignmentOptions.MidlineLeft, 26);
            var sw = Box("Interrupteur", row, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(92, 50), Color.white);
            var le = sw.GetComponent<LayoutElement>(); le.preferredWidth = 92; le.preferredHeight = 50;
            track = sw.GetComponent<Image>();
            sw.gameObject.AddComponent<Button>().onClick.AddListener(() => onToggle());
            knob = Box("Bouton", sw, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40), Cream);
            var kn = knob.GetComponent<Image>(); kn.sprite = circle; kn.type = Image.Type.Simple; kn.raycastTarget = false;
        }

        void BuildProfilePage(RectTransform pg)
        {
            var head = Column(new GameObject("Entete", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>(), 10, 10);
            var headRt = (RectTransform)head.transform;
            headRt.SetParent(pg, false);
            head.childAlignment = TextAnchor.UpperCenter; head.childControlWidth = false;
            var avatar = Box("Avatar", headRt, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(132, 132), Brass);
            var ai = avatar.GetComponent<Image>(); ai.sprite = circle; ai.type = Image.Type.Simple;
            var ale = avatar.GetComponent<LayoutElement>(); ale.preferredWidth = ale.preferredHeight = 132;
            var icon = Icon(avatar, M5Icons.Kind.Profil, 84, Ink);
            ((RectTransform)icon.transform).anchoredPosition = Vector2.zero;
            var name = Label(headRt, "<b>Invité</b>", 32, Cream, TextAlignmentOptions.Center, 44);
            name.rectTransform.sizeDelta = new Vector2(500, 44);
            var sub = Label(headRt, "Connectez-vous pour retrouver vos commandes", 21, Soft, TextAlignmentOptions.Center, 30);
            sub.rectTransform.sizeDelta = new Vector2(540, 30);

            var addr = Row(pg, 96, 14, Tile);
            Icon(addr, M5Icons.Kind.Position, 36, Brass);
            profileAddress = Label(addr, "", 22, Cream, TextAlignmentOptions.MidlineLeft);
            profileAddress.GetComponent<LayoutElement>().flexibleWidth = 1;
            profileAddress.textWrappingMode = TextWrappingModes.Normal; profileAddress.overflowMode = TextOverflowModes.Ellipsis;

            Btn(pg, "<b>Se connecter</b>", Sage, Login, 74, 28, TextAlignmentOptions.Center);
        }

        void BuildChip(RectTransform root)
        {
            chip = Box("Notification", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 10f), new Vector2(400, 64), Glass);
            chip.pivot = new Vector2(0.5f, 1);
            var chipBg = chip.GetComponent<Image>(); chipBg.raycastTarget = false; chipBg.sprite = pill;
            Layer(chip, "Liseré", ring, new Color(1, 1, 1, 0.14f), raycast: false);
            chipGroup = chip.gameObject.AddComponent<CanvasGroup>(); chipGroup.alpha = 0; chipGroup.blocksRaycasts = false;
            var dot = Box("Point", chip, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(36, 0), new Vector2(14, 14), Brass);
            chipDot = dot.GetComponent<Image>(); chipDot.sprite = circle; chipDot.type = Image.Type.Simple; chipDot.raycastTarget = false;
            chipText = Label(chip, "", 25, Cream, TextAlignmentOptions.MidlineLeft);
            chipText.rectTransform.offsetMin = new Vector2(60, 0); chipText.rectTransform.offsetMax = new Vector2(-34, 0);
            chipText.textWrappingMode = TextWrappingModes.Normal;
        }

        void BuildCta(RectTransform root)
        {
            cta = Box("Payer", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(460, 92), Sage);
            cta.pivot = new Vector2(0.5f, 0);
            cta.GetComponent<Image>().sprite = pill;
            var csh = Layer(cta, "Ombre", shadow, new Color(0, 0, 0, 0.4f), raycast: false);
            csh.rectTransform.offsetMin = new Vector2(-16, -20); csh.rectTransform.offsetMax = new Vector2(16, 8);
            csh.transform.SetAsFirstSibling();
            cta.GetComponent<Image>().color = Color.clear;
            Layer(cta, "Fond", pill, Sage, raycast: false).transform.SetSiblingIndex(1);
            Layer(cta, "Liseré", ring, new Color(1, 1, 1, 0.22f), raycast: false);
            var h = cta.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(30, 22, 0, 0); h.spacing = 14; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            cta.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize; // largeur selon le total
            var b = cta.gameObject.AddComponent<Button>();
            b.onClick.AddListener(() => ShowPage(Page.Panier));
            Icon(cta, M5Icons.Kind.Panier, 40, Color.white);
            ctaText = Label(cta, "", 28, Color.white, TextAlignmentOptions.MidlineLeft);
            ctaText.textWrappingMode = TextWrappingModes.NoWrap;
            Icon(cta, M5Icons.Kind.Chevron, 36, Color.white);
            ctaGroup = cta.gameObject.AddComponent<CanvasGroup>(); ctaGroup.alpha = 0; ctaGroup.blocksRaycasts = false;
        }

        void BuildPayment(RectTransform root)
        {
            var dim = Box("Paiement", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.55f));
            dim.offsetMin = dim.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().sprite = null;
            payGroup = dim.gameObject.AddComponent<CanvasGroup>();
            var box = Box("Fenetre", dim, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680, 0), GlassStrong);
            Column(box, 16, 34);
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Label(box, "<b>Paiement sécurisé</b>  <size=70%><color=#B8A48E>par Stripe</color></size>", 36, Cream, TextAlignmentOptions.Center, 56);
            payStatus = Label(box, "", 24, Soft, TextAlignmentOptions.Center, 80);
            payStatus.textWrappingMode = TextWrappingModes.Normal;
            Btn(box, "Rouvrir la page de paiement", new Color(0.33f, 0.45f, 0.55f), () => payment.ReopenPage(), 68, 26, TextAlignmentOptions.Center);
            Btn(box, "Annuler", Muted, () => payment.Cancel(), 58, 26, TextAlignmentOptions.Center);
            ShowPayment(false);
        }

        // ---------- Briques ----------
        RectTransform Box(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>(); img.color = c; img.sprite = rounded; img.type = Image.Type.Sliced;
            return rt;
        }

        RectTransform Row(Transform parent, float height, float spacing, Color bg)
        {
            var rt = Box("Ligne", parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, bg);
            rt.GetComponent<LayoutElement>().preferredHeight = height;
            if (bg.a <= 0f) rt.GetComponent<Image>().color = new Color(0, 0, 0, 0); // reste touchable
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(16, 14, 0, 0); h.spacing = spacing; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            return rt;
        }

        static VerticalLayoutGroup Column(RectTransform rt, float spacing, int pad)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(pad, pad, pad, pad);
            v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false; v.childForceExpandWidth = true;
            return v;
        }

        void Separator(Transform parent)
        {
            var s = Box("Separateur", parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0.1f));
            s.GetComponent<LayoutElement>().preferredHeight = 2;
            s.GetComponent<Image>().sprite = null;
        }

        void Swatch(Transform parent, Color c)
        {
            var s = Box("Couleur", parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, c);
            var img = s.GetComponent<Image>(); img.sprite = circle; img.type = Image.Type.Simple; img.raycastTarget = false;
            var le = s.GetComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = 42;
        }

        Image Icon(Transform parent, M5Icons.Kind k, float size, Color c)
        {
            var img = new GameObject("Icone", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
            img.transform.SetParent(parent, false);
            img.sprite = M5Icons.Get(k); img.color = c; img.raycastTarget = false; img.preserveAspect = true;
            var rt = img.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = Vector2.one * size;
            var le = img.GetComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = size;
            return img;
        }

        Button IconButton(Transform parent, M5Icons.Kind k, float size, Color bg, Color fg, Action onClick)
        {
            var rt = Box("Bouton_" + k, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one * size, bg);
            var img = rt.GetComponent<Image>(); img.sprite = circle; img.type = Image.Type.Simple;
            var le = rt.GetComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = size; le.minWidth = size;
            var b = rt.gameObject.AddComponent<Button>();
            var cb = b.colors; cb.pressedColor = new Color(0.75f, 0.75f, 0.75f); cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); b.colors = cb;
            b.onClick.AddListener(() => onClick());
            var icon = Icon(rt, k, size * 0.5f, fg);
            icon.GetComponent<LayoutElement>().ignoreLayout = true;
            return b;
        }

        void Scroll(Transform parent, float minHeight, out RectTransform content)
        {
            var view = new GameObject("Liste", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(LayoutElement), typeof(ScrollRect)).GetComponent<RectTransform>();
            view.SetParent(parent, false);
            view.GetComponent<Image>().color = new Color(0, 0, 0, 0); // capte le glissé
            var le = view.GetComponent<LayoutElement>(); le.flexibleHeight = 1; le.minHeight = minHeight;
            content = new GameObject("Contenu", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(view, false);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            Column(content, 10, 0);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = view.GetComponent<ScrollRect>();
            sr.viewport = view; sr.content = content; sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Elastic; sr.scrollSensitivity = 30f;
        }

        TMP_InputField InputField(Transform parent, string placeholder, float height)
        {
            var rt = Box("Adresse", parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0.09f));
            rt.GetComponent<LayoutElement>().preferredHeight = height;
            rt.gameObject.SetActive(false); // références posées avant OnEnable du champ
            var area = new GameObject("Zone", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            area.SetParent(rt, false);
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = new Vector2(18, 8); area.offsetMax = new Vector2(-18, -8);
            var ph = Label(area, placeholder, 23, new Color(1, 1, 1, 0.4f), TextAlignmentOptions.TopLeft);
            ph.fontStyle = FontStyles.Italic;
            var txt = Label(area, "", 23, Cream, TextAlignmentOptions.TopLeft);
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

        /// Contour arrondi (liseré), 9-slice.
        static Sprite MakeRing(int size, int radius, float thickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius), cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float inside = radius - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(inside + 0.5f) * Mathf.Clamp01(thickness - inside + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, Vector4.one * radius);
        }

        /// Ombre douce (dégradé radial), 9-slice.
        static Sprite MakeShadow(int size)
        {
            int r = size / 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    float a = Mathf.Clamp01(1f - d); a *= a;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, Vector4.one * (r - 1));
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
