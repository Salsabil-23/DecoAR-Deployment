using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DecoAR.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Networking;
using UnityEngine.UI;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace DecoAR.M5
{
    /// Choix de l'adresse de livraison sur une carte OpenStreetMap, plein écran, style DecoAR :
    /// glisser pour déplacer, pincer / molette / + − pour zoomer, double toucher pour zoomer ;
    /// l'épingle reste au centre et l'adresse s'affiche en direct (Nominatim) ; recherche d'adresse avec
    /// suggestions ; « Ma position » (GPS) ; « Livrer ici » renvoie l'adresse et ses coordonnées.
    /// Tuiles : tile.openstreetmap.org (attribution affichée), géocodage : nominatim.openstreetmap.org (1 requête / s).
    public class M5MapPicker : MonoBehaviour
    {
        const string TileUrl = "https://tile.openstreetmap.org/{0}/{1}/{2}.png";
        const string Nominatim = "https://nominatim.openstreetmap.org/";
        const string UserAgent = "DecoAR/1.0 (projet etudiant ESPRIT, Unity)";
        const float Tile = 256f;
        const int MinZoom = 3, MaxZoom = 19, MaxDownloads = 4, MaxCache = 140;
        const string LatKey = "M5_AdresseLat", LonKey = "M5_AdresseLon";

        static double lastNominatim = -10;

        Action<string, double, double> onPick;
        double mx, my;          // centre en coordonnées Mercator normalisées (0..1)
        float zoom = 15f;
        RectTransform canvasRoot, map, tiles, pin, pinShadow, results, resultsContent, ring;
        CanvasGroup group, resultsGroup, ringGroup;
        TextMeshProUGUI addressText, addressTitle;
        TMP_InputField search;
        Button pickButton;
        Image spinnerDot;
        string address;
        bool resolving, dragging, closing;
        float idleSince, lastTap, pinLift, ringT = 1f, searchTypedAt = -1f;
        Vector2 dragLast;
        float pinchLast;
        Coroutine fly, reverse;
        M5LocationService gps;

        readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        readonly LinkedList<string> lru = new LinkedList<string>();
        readonly HashSet<string> loading = new HashSet<string>();
        readonly HashSet<string> failed = new HashSet<string>();
        readonly Dictionary<string, RawImage> shown = new Dictionary<string, RawImage>();
        readonly Stack<RawImage> pool = new Stack<RawImage>();
        int downloads;

        // ------------------------------------------------------------ ouverture

        /// Ouvre la carte. onPick(adresse, latitude, longitude) quand l'utilisateur touche « Livrer ici ».
        public static M5MapPicker Open(string currentAddress, Action<string, double, double> onPick)
        {
            var go = new GameObject("M5_Carte");
            var m = go.AddComponent<M5MapPicker>();
            m.onPick = onPick;
            m.address = currentAddress;
            return m;
        }

        void Start()
        {
            EnhancedTouchSupport.Enable();
            DecoUI.EnsureEventSystem();
            // Départ : dernière adresse choisie, sinon Tunis
            double lat = PlayerPrefs.HasKey(LatKey) ? double.Parse(PlayerPrefs.GetString(LatKey), CultureInfo.InvariantCulture) : 36.8065;
            double lon = PlayerPrefs.HasKey(LonKey) ? double.Parse(PlayerPrefs.GetString(LonKey), CultureInfo.InvariantCulture) : 10.1815;
            zoom = PlayerPrefs.HasKey(LatKey) ? 17f : 13f;
            (mx, my) = ToMercator(lat, lon);
            Build();
            StartCoroutine(FadeIn());
            if (!string.IsNullOrEmpty(address) && PlayerPrefs.HasKey(LatKey)) SetAddress(address, false);
            else RequestReverse(0.2f);
        }

        IEnumerator FadeIn()
        {
            for (float t = 0; t < 0.25f; t += Time.unscaledDeltaTime) { group.alpha = t / 0.25f; yield return null; }
            group.alpha = 1f;
        }

        public void Close()
        {
            if (closing) return;
            closing = true;
            StartCoroutine(FadeOut());
        }

        IEnumerator FadeOut()
        {
            group.blocksRaycasts = false;
            for (float t = 0; t < 0.2f; t += Time.unscaledDeltaTime) { group.alpha = 1f - t / 0.2f; yield return null; }
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            foreach (var t in cache.Values) if (t) Destroy(t);
        }

        // ------------------------------------------------------------ boucle

        void Update()
        {
            if (map == null) return;
            HandleInput();
            DrawTiles();

            // Épingle : se soulève pendant le déplacement, retombe avec un rebond
            bool moving = dragging || fly != null;
            pinLift = moving ? Mathf.MoveTowards(pinLift, 1f, Time.unscaledDeltaTime * 6f) : Mathf.MoveTowards(pinLift, 0f, Time.unscaledDeltaTime * 4f);
            pin.anchoredPosition = new Vector2(0, 46f + 26f * EaseOutBack(pinLift));
            pinShadow.localScale = new Vector3(1f - 0.35f * pinLift, (1f - 0.35f * pinLift) * 0.45f, 1f);
            if (!moving && ringT < 1f)
            {
                ringT += Time.unscaledDeltaTime / 0.8f;
                ring.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.6f, ringT);
                ringGroup.alpha = 1f - ringT;
            }

            // Adresse : demandée quand la carte s'arrête
            if (moving) { idleSince = Time.unscaledTime; ringT = 1f; ringGroup.alpha = 0; }
            if (spinnerDot) spinnerDot.color = new Color(1, 1, 1, resolving ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f) : 0f);

            // Recherche pendant la frappe (pause de 0,8 s, 3 lettres minimum)
            if (searchTypedAt > 0 && Time.unscaledTime - searchTypedAt > 0.8f)
            {
                searchTypedAt = -1f;
                if (search.text.Trim().Length >= 3) StartCoroutine(Search(search.text.Trim()));
            }
            resultsGroup.alpha = Mathf.MoveTowards(resultsGroup.alpha, resultsGroup.blocksRaycasts ? 1f : 0f, Time.unscaledDeltaTime * 8f);

            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Close();
        }

        // ------------------------------------------------------------ gestes

        void HandleInput()
        {
            var touches = ETouch.activeTouches;
            if (touches.Count >= 2)
            {
                float d = Vector2.Distance(touches[0].screenPosition, touches[1].screenPosition);
                if (pinchLast > 0f && d > 1f)
                {
                    var mid = (touches[0].screenPosition + touches[1].screenPosition) * 0.5f;
                    ZoomAt(Mathf.Log(d / pinchLast, 2f), mid);
                }
                pinchLast = d;
                dragging = true;
                dragLast = (touches[0].screenPosition + touches[1].screenPosition) * 0.5f;
                return;
            }
            bool wasPinching = pinchLast > 0f;
            pinchLast = 0f;

            Vector2 pos; bool down;
            if (touches.Count == 1) { pos = touches[0].screenPosition; down = true; }
            else if (Mouse.current != null) { pos = Mouse.current.position.ReadValue(); down = Mouse.current.leftButton.isPressed; }
            else
            {
                if (dragging) { dragging = false; RequestReverse(0.6f); }
                return;
            }
            if (wasPinching && down) dragLast = pos; // un doigt reste après le pincement : pas de saut

            if (down && !dragging)
            {
                if (!OverMap(pos)) return;
                dragging = true; dragLast = pos;
                if (fly != null) { StopCoroutine(fly); fly = null; }
                if (Time.unscaledTime - lastTap < 0.3f) ZoomAt(1f, pos, true); // double toucher : zoom avant
                lastTap = Time.unscaledTime;
                HideResults();
            }
            else if (down && dragging)
            {
                Pan((pos - dragLast) / Scale);
                dragLast = pos;
            }
            else if (!down && dragging)
            {
                dragging = false;
                RequestReverse(0.6f);
            }

            if (Mouse.current != null && touches.Count == 0)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f && OverMap(Mouse.current.position.ReadValue()))
                {
                    ZoomAt(Mathf.Sign(scroll) * 0.35f, Mouse.current.position.ReadValue());
                    RequestReverse(0.6f);
                }
            }
        }

        float Scale => canvasRoot.GetComponentInParent<Canvas>().scaleFactor;

        bool OverMap(Vector2 screen)
        {
            var es = EventSystem.current;
            if (es == null) return true;
            var hits = new List<RaycastResult>();
            es.RaycastAll(new PointerEventData(es) { position = screen }, hits);
            return hits.Count > 0 && hits[0].gameObject == map.gameObject;
        }

        void Pan(Vector2 deltaUnits)
        {
            double world = Tile * Math.Pow(2, zoom);
            mx -= deltaUnits.x / world;
            my += deltaUnits.y / world;
            mx = mx - Math.Floor(mx);                  // la Terre fait le tour
            my = Math.Max(0.0001, Math.Min(0.9999, my));
        }

        /// Zoom autour d'un point de l'écran (le point sous les doigts reste en place).
        void ZoomAt(float deltaZoom, Vector2 screen, bool animate = false)
        {
            float target = Mathf.Clamp(zoom + deltaZoom, MinZoom, MaxZoom);
            if (animate)
            {
                // zoom vers le point touché, en douceur
                RectTransformUtility.ScreenPointToLocalPointInRectangle(map, screen, null, out var lp);
                double world = Tile * Math.Pow(2, zoom);
                double tx = mx + lp.x / world * (1 - Math.Pow(2, zoom - target)), ty = my - lp.y / world * (1 - Math.Pow(2, zoom - target));
                FlyTo(tx, ty, target, 0.35f);
                return;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(map, screen, null, out var local);
            double w0 = Tile * Math.Pow(2, zoom), w1 = Tile * Math.Pow(2, target);
            // point sous les doigts, en Mercator, avant et après : on compense
            double px = mx + local.x / w0, py = my - local.y / w0;
            mx = px - local.x / w1; my = py + local.y / w1;
            zoom = target;
        }

        void FlyTo(double tx, double ty, float tz, float duration)
        {
            if (fly != null) StopCoroutine(fly);
            fly = StartCoroutine(FlyRoutine(tx, ty, tz, duration));
        }

        IEnumerator FlyRoutine(double tx, double ty, float tz, float duration)
        {
            double x0 = mx, y0 = my; float z0 = zoom;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float e = Mathf.SmoothStep(0f, 1f, t / duration);
                mx = x0 + (tx - x0) * e; my = y0 + (ty - y0) * e; zoom = Mathf.Lerp(z0, tz, e);
                yield return null;
            }
            mx = tx; my = ty; zoom = tz;
            fly = null;
            RequestReverse(0.15f);
        }

        // ------------------------------------------------------------ tuiles

        void DrawTiles()
        {
            int z = Mathf.Clamp(Mathf.RoundToInt(zoom), MinZoom, MaxZoom);
            int n = 1 << z;
            float size = Tile * Mathf.Pow(2f, zoom - z);
            double cx = mx * n, cy = my * n;
            var rect = map.rect;
            int x0 = (int)Math.Floor(cx - rect.width * 0.5 / size), x1 = (int)Math.Floor(cx + rect.width * 0.5 / size);
            int y0 = Math.Max(0, (int)Math.Floor(cy - rect.height * 0.5 / size)), y1 = Math.Min(n - 1, (int)Math.Floor(cy + rect.height * 0.5 / size));

            var used = new HashSet<string>();
            var wanted = new List<(string key, int z, int x, int y, double d)>();
            for (int ty = y0; ty <= y1; ty++)
                for (int tx = x0; tx <= x1; tx++)
                {
                    int wx = ((tx % n) + n) % n;
                    string key = $"{z}/{wx}/{ty}";
                    string slot = $"{key}@{tx}";
                    used.Add(slot);
                    if (!shown.TryGetValue(slot, out var img)) { img = Take(); shown[slot] = img; }
                    var rt = img.rectTransform;
                    rt.anchoredPosition = new Vector2((float)((tx - cx) * size), (float)(-(ty - cy) * size));
                    rt.sizeDelta = new Vector2(size + 0.6f, size + 0.6f); // léger recouvrement : pas de joint visible

                    if (cache.TryGetValue(key, out var tex) && tex)
                    {
                        img.texture = tex; img.uvRect = new Rect(0, 0, 1, 1);
                        img.color = Color.Lerp(img.color, Color.white, Time.unscaledDeltaTime * 10f);
                        Touch(key);
                    }
                    else
                    {
                        // En attendant : la tuile parente agrandie (zoom précédent), sinon fond lin
                        string parent = $"{z - 1}/{wx / 2}/{ty / 2}";
                        if (z > MinZoom && cache.TryGetValue(parent, out var ptex) && ptex)
                        {
                            img.texture = ptex;
                            img.uvRect = new Rect((wx % 2) * 0.5f, (1 - ty % 2) * 0.5f, 0.5f, 0.5f);
                            img.color = Color.white;
                        }
                        else { img.texture = null; img.color = new Color32(0xED, 0xE1, 0xCD, 255); }
                        if (!loading.Contains(key) && !failed.Contains(key))
                            wanted.Add((key, z, wx, ty, (tx + 0.5 - cx) * (tx + 0.5 - cx) + (ty + 0.5 - cy) * (ty + 0.5 - cy)));
                    }
                }

            // Tuiles qui sortent de l'écran : rendues au pool
            var gone = new List<string>();
            foreach (var kv in shown) if (!used.Contains(kv.Key)) gone.Add(kv.Key);
            foreach (var k in gone) { var img = shown[k]; shown.Remove(k); img.gameObject.SetActive(false); pool.Push(img); }

            // Téléchargements : d'abord le centre
            wanted.Sort((a, b) => a.d.CompareTo(b.d));
            foreach (var w in wanted)
            {
                if (downloads >= MaxDownloads) break;
                if (loading.Contains(w.key)) continue;
                StartCoroutine(Download(w.key, w.z, w.x, w.y));
            }
        }

        RawImage Take()
        {
            RawImage img;
            if (pool.Count > 0) img = pool.Pop();
            else
            {
                img = new GameObject("Tuile", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                img.transform.SetParent(tiles, false);
                img.raycastTarget = false;
                var rt = img.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0, 1);
            }
            img.gameObject.SetActive(true);
            img.color = new Color32(0xED, 0xE1, 0xCD, 255);
            return img;
        }

        IEnumerator Download(string key, int z, int x, int y)
        {
            loading.Add(key); downloads++;
            using (var req = UnityWebRequestTexture.GetTexture(string.Format(TileUrl, z, x, y), true))
            {
                req.SetRequestHeader("User-Agent", UserAgent);
                req.timeout = 15;
                yield return req.SendWebRequest();
                downloads--; loading.Remove(key);
                if (this == null) yield break;
                if (req.result != UnityWebRequest.Result.Success) { failed.Add(key); StartCoroutine(Retry(key)); yield break; }
                var tex = DownloadHandlerTexture.GetContent(req);
                tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear;
                cache[key] = tex;
                Touch(key);
                while (lru.Count > MaxCache)
                {
                    var old = lru.Last.Value; lru.RemoveLast();
                    if (cache.TryGetValue(old, out var t)) { cache.Remove(old); Destroy(t); }
                }
            }
        }

        IEnumerator Retry(string key) { yield return new WaitForSecondsRealtime(4f); failed.Remove(key); }

        void Touch(string key)
        {
            var node = lru.Find(key);
            if (node != null) lru.Remove(node);
            lru.AddFirst(key);
        }

        // ------------------------------------------------------------ adresse (Nominatim)

        void RequestReverse(float delay)
        {
            if (reverse != null) StopCoroutine(reverse);
            reverse = StartCoroutine(ReverseRoutine(delay));
        }

        IEnumerator ReverseRoutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            while (dragging || fly != null) yield return null;
            resolving = true;
            pickButton.interactable = false;
            addressText.text = "<color=#DDCDB5>Recherche de l'adresse…</color>";
            yield return NominatimSlot();
            var (lat, lon) = FromMercator(mx, my);
            var ci = CultureInfo.InvariantCulture;
            string url = $"{Nominatim}reverse?format=jsonv2&addressdetails=1&zoom=18&accept-language=fr&lat={lat.ToString("0.000000", ci)}&lon={lon.ToString("0.000000", ci)}";
            using (var req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("User-Agent", UserAgent);
                req.timeout = 10;
                yield return req.SendWebRequest();
                string found = null;
                if (req.result == UnityWebRequest.Result.Success)
                {
                    try { found = M5LocationService.Format(JsonUtility.FromJson<M5LocationService.NominatimResult>(req.downloadHandler.text)); }
                    catch (Exception e) { Debug.LogWarning("[M5] Adresse illisible : " + e.Message); }
                }
                resolving = false;
                if (string.IsNullOrEmpty(found))
                {
                    found = $"Position : {lat.ToString("0.00000", ci)}, {lon.ToString("0.00000", ci)}";
                    if (req.result != UnityWebRequest.Result.Success) found += "  <color=#F2A08F>(pas de connexion)</color>";
                }
                SetAddress(found, true);
            }
            reverse = null;
        }

        void SetAddress(string text, bool fromMap)
        {
            address = text;
            addressText.text = text;
            pickButton.interactable = !string.IsNullOrEmpty(text);
            if (fromMap) { ringT = 0f; M5Haptics.Light(); }
        }

        /// Nominatim : une requête par seconde au maximum (règle d'utilisation d'OpenStreetMap).
        static IEnumerator NominatimSlot()
        {
            double wait = lastNominatim + 1.05 - Time.realtimeSinceStartupAsDouble;
            if (wait > 0) yield return new WaitForSecondsRealtime((float)wait);
            lastNominatim = Time.realtimeSinceStartupAsDouble;
        }

        [Serializable] class SearchList { public M5LocationService.NominatimResult[] items; }

        IEnumerator Search(string query)
        {
            yield return NominatimSlot();
            string url = $"{Nominatim}search?format=jsonv2&addressdetails=1&limit=5&accept-language=fr&q={UnityWebRequest.EscapeURL(query)}";
            using (var req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("User-Agent", UserAgent);
                req.timeout = 10;
                yield return req.SendWebRequest();
                for (int i = resultsContent.childCount - 1; i >= 0; i--) Destroy(resultsContent.GetChild(i).gameObject);
                if (req.result != UnityWebRequest.Result.Success) { ResultRow("Pas de connexion. Vérifie ton réseau puis réessaie.", null); yield break; }
                SearchList list = null;
                try { list = JsonUtility.FromJson<SearchList>("{\"items\":" + req.downloadHandler.text + "}"); }
                catch (Exception e) { Debug.LogWarning("[M5] Recherche illisible : " + e.Message); }
                if (list?.items == null || list.items.Length == 0) { ResultRow("Aucun résultat. Essaie un autre mot ou ajoute la ville.", null); yield break; }
                foreach (var r in list.items)
                {
                    var item = r;
                    string label = M5LocationService.Format(item) ?? item.display_name;
                    ResultRow(label, () =>
                    {
                        double lat = double.Parse(item.lat, CultureInfo.InvariantCulture), lon = double.Parse(item.lon, CultureInfo.InvariantCulture);
                        var (tx, ty) = ToMercator(lat, lon);
                        search.text = "";
                        search.DeactivateInputField();
                        HideResults();
                        FlyTo(tx, ty, 17.5f, 0.9f);
                    });
                }
                resultsGroup.blocksRaycasts = resultsGroup.interactable = true;
            }
        }

        void ResultRow(string text, Action onClick)
        {
            var row = DecoUI.Row(resultsContent, 76, 14, DecoUI.Tile, 18, 18);
            DecoUI.Icon(row, onClick != null ? M5Icons.Kind.Position : M5Icons.Kind.Loupe, 30, onClick != null ? DecoUI.Brass : DecoUI.Soft);
            var t = DecoUI.Label(row, text, 22, DecoUI.Cream, TextAlignmentOptions.MidlineLeft);
            t.GetComponent<LayoutElement>().flexibleWidth = 1;
            t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis;
            t.rectTransform.offsetMin = Vector2.zero;
            if (onClick != null) row.gameObject.AddComponent<Button>().onClick.AddListener(() => onClick());
            resultsGroup.blocksRaycasts = resultsGroup.interactable = true;
        }

        void HideResults()
        {
            resultsGroup.blocksRaycasts = resultsGroup.interactable = false;
            for (int i = resultsContent.childCount - 1; i >= 0; i--) Destroy(resultsContent.GetChild(i).gameObject);
        }

        void MyPosition()
        {
            if (!gps) gps = gameObject.AddComponent<M5LocationService>();
            if (gps.Busy) return;
            addressText.text = "<color=#DDCDB5>Recherche de ta position…</color>";
            gps.FindCoordinates(s => addressText.text = $"<color=#DDCDB5>{s}</color>", (lat, lon, err) =>
            {
                if (err != null) { addressText.text = $"<color=#F2A08F>{err}</color>"; return; }
                var (tx, ty) = ToMercator(lat, lon);
                FlyTo(tx, ty, 17.5f, 1f);
            });
        }

        void Pick()
        {
            if (string.IsNullOrEmpty(address) || resolving) return;
            var (lat, lon) = FromMercator(mx, my);
            var ci = CultureInfo.InvariantCulture;
            PlayerPrefs.SetString(LatKey, lat.ToString("R", ci));
            PlayerPrefs.SetString(LonKey, lon.ToString("R", ci));
            PlayerPrefs.Save();
            M5Haptics.Strong();
            onPick?.Invoke(address, lat, lon);
            Close();
        }

        // ------------------------------------------------------------ Mercator

        static (double, double) ToMercator(double lat, double lon)
        {
            double x = (lon + 180.0) / 360.0;
            double s = Math.Sin(lat * Math.PI / 180.0);
            double y = 0.5 - Math.Log((1 + s) / (1 - s)) / (4 * Math.PI);
            return (x, Math.Max(0.0001, Math.Min(0.9999, y)));
        }

        static (double, double) FromMercator(double x, double y)
        {
            double lon = x * 360.0 - 180.0;
            double n = Math.PI - 2.0 * Math.PI * y;
            double lat = 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
            return (lat, lon);
        }

        static float EaseOutBack(float t) { const float c = 1.7f; t -= 1f; return 1f + (c + 1f) * t * t * t + c * t * t; }

        // ------------------------------------------------------------ construction

        void Build()
        {
            var safe = DecoUI.Canvas(transform, "M5_CarteCanvas", 150);
            canvasRoot = (RectTransform)safe.parent;
            group = canvasRoot.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0;

            // Carte plein écran (sous la zone sûre)
            map = DecoUI.Box("Carte", canvasRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color32(0xED, 0xE1, 0xCD, 255));
            map.offsetMin = map.offsetMax = Vector2.zero;
            map.GetComponent<Image>().sprite = null;
            map.gameObject.AddComponent<RectMask2D>();
            map.SetAsFirstSibling();
            tiles = new GameObject("Tuiles", typeof(RectTransform)).GetComponent<RectTransform>();
            tiles.SetParent(map, false);
            tiles.anchorMin = tiles.anchorMax = new Vector2(0.5f, 0.5f);
            // Voile chaud léger : la carte prend les tons DecoAR sans perdre sa lisibilité
            var veil = DecoUI.Box("Voile", map, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.98f, 0.92f, 0.84f, 0.12f));
            veil.offsetMin = veil.offsetMax = Vector2.zero; veil.GetComponent<Image>().sprite = null; veil.GetComponent<Image>().raycastTarget = false;
            var top = DecoUI.Box("Ombre haut", map, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 200), new Color(0.12f, 0.1f, 0.08f, 0.35f));
            top.pivot = new Vector2(0.5f, 1); top.GetComponent<Image>().sprite = Resources.Load<Sprite>("DecoAR/UI/dc-voile-haut"); top.GetComponent<Image>().raycastTarget = false;

            // Épingle au centre (pointe sur le centre), ombre, onde
            ring = DecoUI.Box("Onde", map, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 70), DecoAR.Design.DecoPalette.Terracotta500);
            var ri = ring.GetComponent<Image>(); ri.sprite = DecoUI.RingSprite; ri.type = Image.Type.Sliced; ri.raycastTarget = false;
            ringGroup = ring.gameObject.AddComponent<CanvasGroup>(); ringGroup.alpha = 0;
            pinShadow = DecoUI.Box("Ombre épingle", map, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30), new Color(0, 0, 0, 0.35f));
            var ps = pinShadow.GetComponent<Image>(); ps.sprite = DecoUI.Circle; ps.type = Image.Type.Simple; ps.raycastTarget = false;
            pin = new GameObject("Épingle", typeof(RectTransform)).GetComponent<RectTransform>();
            pin.SetParent(map, false);
            pin.anchorMin = pin.anchorMax = new Vector2(0.5f, 0.5f); pin.sizeDelta = new Vector2(84, 84);
            var head = DecoUI.Box("Tête", pin, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84, 84), DecoAR.Design.DecoPalette.Terracotta500);
            head.GetComponent<Image>().sprite = DecoUI.Circle; head.GetComponent<Image>().type = Image.Type.Simple; head.GetComponent<Image>().raycastTarget = false;
            DecoUI.Layer(head, "Liseré", DecoUI.RingSprite, new Color(1, 1, 1, 0.9f), false).type = Image.Type.Sliced;
            var dot = DecoUI.Box("Centre", head, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30), DecoAR.Design.DecoPalette.Lin50);
            dot.GetComponent<Image>().sprite = DecoUI.Circle; dot.GetComponent<Image>().type = Image.Type.Simple; dot.GetComponent<Image>().raycastTarget = false;
            var stem = DecoUI.Box("Pointe", pin, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(8, 34), DecoAR.Design.DecoPalette.Terracotta600);
            stem.GetComponent<Image>().sprite = DecoUI.PillShape; stem.GetComponent<Image>().raycastTarget = false;

            // Haut : fermer + recherche
            var close = DecoUI.FloatButton(safe, M5Icons.Kind.Retour, 84, Close, out _, out _);
            var crt = (RectTransform)close.transform; crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0, 1); crt.anchoredPosition = new Vector2(24, -24);
            var bar = new GameObject("Recherche", typeof(RectTransform)).GetComponent<RectTransform>();
            bar.SetParent(safe, false);
            bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1); bar.pivot = new Vector2(0.5f, 1);
            bar.offsetMin = new Vector2(24 + 84 + 16, -24 - 84); bar.offsetMax = new Vector2(-(24 + 84 + 16), -24);
            DecoUI.Glassy(bar, DecoUI.GlassStrong);
            var h = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(26, 14, 8, 8); h.spacing = 12; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            DecoUI.Icon(bar, M5Icons.Kind.Loupe, 34, DecoUI.Soft);
            search = DecoUI.InputField(bar, "Rechercher une adresse, une ville…", 26);
            search.GetComponent<LayoutElement>().flexibleWidth = 1; search.GetComponent<LayoutElement>().preferredHeight = 64;
            search.characterLimit = 120;
            search.onValueChanged.AddListener(_ => searchTypedAt = Time.unscaledTime);
            search.onSubmit.AddListener(q => { searchTypedAt = -1f; if (q.Trim().Length > 0) StartCoroutine(Search(q.Trim())); });
            DecoUI.IconButton(bar, M5Icons.Kind.Fermer, 56, DecoUI.Tile, DecoUI.Cream, () => { search.text = ""; HideResults(); });

            // Suggestions sous la recherche
            results = DecoUI.Box("Suggestions", safe, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero, Color.clear);
            results.pivot = new Vector2(0.5f, 1);
            results.offsetMin = new Vector2(24 + 84 + 16, -24 - 84 - 12 - 420); results.offsetMax = new Vector2(-(24 + 84 + 16), -24 - 84 - 12);
            DecoUI.Glassy(results, DecoUI.GlassStrong, DecoUI.Rounded).raycastTarget = false;
            resultsGroup = results.gameObject.AddComponent<CanvasGroup>(); resultsGroup.alpha = 0; resultsGroup.blocksRaycasts = false;
            DecoUI.Column(results, 8, 14);
            var fit = results.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            resultsContent = results;

            // Droite : zoom + ma position
            var tools = new GameObject("Outils", typeof(RectTransform)).GetComponent<RectTransform>();
            tools.SetParent(safe, false);
            tools.anchorMin = tools.anchorMax = tools.pivot = new Vector2(1, 0.5f);
            tools.anchoredPosition = new Vector2(-24, 20); tools.sizeDelta = new Vector2(84, 300);
            var tv = tools.gameObject.AddComponent<VerticalLayoutGroup>();
            tv.spacing = 14; tv.childAlignment = TextAnchor.MiddleCenter; tv.childControlWidth = tv.childControlHeight = false;
            DecoUI.FloatButton(tools, M5Icons.Kind.Plus, 84, () => { ZoomAt(1f, MapCenterScreen(), true); }, out _, out _);
            DecoUI.FloatButton(tools, M5Icons.Kind.Moins, 84, () => { ZoomAt(-1f, MapCenterScreen(), true); }, out _, out _);
            DecoUI.FloatButton(tools, M5Icons.Kind.Recentrer, 84, MyPosition, out _, out _);

            // Bas : adresse + « Livrer ici »
            var card = DecoUI.Box("Adresse", safe, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(1040, 150), Color.clear);
            card.pivot = new Vector2(0.5f, 0);
            DecoUI.Glassy(card, DecoUI.GlassStrong, DecoUI.Rounded);
            var ch = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            ch.padding = new RectOffset(30, 22, 18, 18); ch.spacing = 18; ch.childAlignment = TextAnchor.MiddleLeft;
            ch.childControlWidth = ch.childControlHeight = true; ch.childForceExpandWidth = false; ch.childForceExpandHeight = false;
            var iconBox = DecoUI.Box("Icône", card, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.87f, 0.37f, 0.16f, 0.18f));
            iconBox.GetComponent<Image>().sprite = DecoUI.Circle; iconBox.GetComponent<Image>().type = Image.Type.Simple;
            var ile = iconBox.GetComponent<LayoutElement>(); ile.preferredWidth = ile.preferredHeight = 76; ile.minWidth = 76;
            DecoUI.Icon(iconBox, M5Icons.Kind.Position, 40, DecoUI.Terracotta).GetComponent<LayoutElement>().ignoreLayout = true;
            spinnerDot = DecoUI.Dot(iconBox, Color.white, 12); spinnerDot.GetComponent<LayoutElement>().ignoreLayout = true;
            spinnerDot.rectTransform.anchoredPosition = new Vector2(26, 26);
            var texts = new GameObject("Textes", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            texts.SetParent(card, false);
            texts.GetComponent<LayoutElement>().flexibleWidth = 1;
            var tvg = DecoUI.Column(texts, 2, 0); tvg.childAlignment = TextAnchor.MiddleLeft;
            addressTitle = DecoUI.Label(texts, "LIVRAISON", 18, DecoUI.Price, TextAlignmentOptions.MidlineLeft, 26);
            addressTitle.characterSpacing = 8; addressTitle.fontStyle = FontStyles.Bold; addressTitle.rectTransform.offsetMin = Vector2.zero;
            addressText = DecoUI.Label(texts, "", 24, DecoUI.Cream, TextAlignmentOptions.TopLeft, 70);
            addressText.textWrappingMode = TextWrappingModes.Normal; addressText.overflowMode = TextOverflowModes.Ellipsis;
            addressText.rectTransform.offsetMin = Vector2.zero;
            pickButton = DecoUI.Pill(card, "<b>Livrer ici</b>", DecoUI.Sage, Color.white, Pick, 88, 27, M5Icons.Kind.Valide);
            pickButton.GetComponent<LayoutElement>().minWidth = 260;

            // Attribution obligatoire d'OpenStreetMap
            var attr = DecoUI.Box("Attribution", canvasRoot, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-8, 6), new Vector2(310, 30), new Color(1, 1, 1, 0.75f));
            attr.pivot = new Vector2(1, 0); attr.GetComponent<Image>().sprite = DecoUI.PillShape; attr.GetComponent<Image>().raycastTarget = false;
            var at = DecoUI.Label(attr, "© les contributeurs OpenStreetMap", 15, DecoAR.Design.DecoPalette.Lin800, TextAlignmentOptions.Center);
            at.rectTransform.offsetMin = at.rectTransform.offsetMax = Vector2.zero;
        }

        Vector2 MapCenterScreen() => RectTransformUtility.WorldToScreenPoint(null, map.TransformPoint(map.rect.center));
    }
}
