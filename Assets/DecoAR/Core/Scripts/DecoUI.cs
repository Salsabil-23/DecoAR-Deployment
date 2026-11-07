using System;
using DecoAR.M5;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DecoAR.UI
{
    // Briques d'interface communes (même style que le HUD de la boutique M5) : verre fumé, crème, laiton,
    // pastilles arrondies, icônes vectorielles M5Icons. Utilisées par le catalogue M3 et l'AR M4.
    public static class DecoUI
    {
        public static readonly Color Glass = new Color(0.12f, 0.10f, 0.08f, 0.72f);
        public static readonly Color GlassStrong = new Color(0.12f, 0.10f, 0.08f, 0.94f);
        public static readonly Color Cream = new Color(0.98f, 0.95f, 0.89f);
        public static readonly Color Soft = new Color(0.72f, 0.64f, 0.56f);
        public static readonly Color Sage = new Color(0.45f, 0.58f, 0.43f);
        public static readonly Color Brass = new Color(0.82f, 0.65f, 0.32f);
        public static readonly Color Terracotta = new Color(0.78f, 0.45f, 0.29f);
        public static readonly Color Ink = new Color(0.16f, 0.13f, 0.10f);
        public static readonly Color Tile = new Color(1f, 1f, 1f, 0.07f);
        public static readonly Color TileOn = new Color(1f, 1f, 1f, 0.16f);
        public static readonly Color RingIdle = new Color(1, 1, 1, 0.16f);

        static Sprite rounded, circle, pill, ring, shadow;
        public static Sprite Rounded => rounded ? rounded : rounded = MakeSprite(64, 22, true);
        public static Sprite Circle => circle ? circle : circle = MakeSprite(64, 32, false);
        public static Sprite PillShape => pill ? pill : pill = MakeSprite(64, 32, true);
        public static Sprite RingSprite => ring ? ring : ring = MakeRing(64, 32, 2.5f);
        public static Sprite Shadow => shadow ? shadow : shadow = MakeShadow(64);

        /// Canvas plein écran 1920×1080 (comme M5) ; retourne la zone sûre (hors encoche).
        public static RectTransform Canvas(Transform parent, string name, int order)
        {
            var cgo = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(parent, false);
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            var safe = new GameObject("ZoneSure", typeof(RectTransform), typeof(SafeArea)).GetComponent<RectTransform>();
            safe.SetParent(cgo.transform, false);
            safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = safe.offsetMax = Vector2.zero;
            return safe;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current || UnityEngine.Object.FindAnyObjectByType<EventSystem>()) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public static RectTransform Box(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>(); img.color = c; img.sprite = Rounded; img.type = Image.Type.Sliced;
            return rt;
        }

        public static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.one * inset; rt.offsetMax = -Vector2.one * inset;
        }

        /// Calque décoratif hors mise en page (fond, ombre, liseré).
        public static Image Layer(RectTransform parent, string name, Sprite s, Color c, bool raycast)
        {
            var img = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
            img.transform.SetParent(parent, false);
            img.GetComponent<LayoutElement>().ignoreLayout = true;
            Stretch(img.rectTransform);
            img.sprite = s; img.type = Image.Type.Sliced; img.color = c; img.raycastTarget = raycast;
            return img;
        }

        /// Pastille de verre : ombre douce + fond + liseré clair. Retourne le fond (cible du bouton).
        public static Image Glassy(RectTransform rt, Color fill, Sprite shape = null)
        {
            var own = rt.GetComponent<Image>();
            if (own) own.color = Color.clear;
            var sh = Layer(rt, "Ombre", Shadow, new Color(0, 0, 0, 0.38f), false);
            sh.rectTransform.offsetMin = new Vector2(-14, -18); sh.rectTransform.offsetMax = new Vector2(14, 8);
            sh.transform.SetAsFirstSibling();
            var bg = Layer(rt, "Fond", shape ? shape : PillShape, fill, true);
            bg.transform.SetSiblingIndex(1);
            var rg = Layer(rt, "Liseré", RingSprite, RingIdle, false);
            rg.transform.SetSiblingIndex(2);
            return bg;
        }

        public static RectTransform Row(Transform parent, float height, float spacing, Color bg, int padL = 16, int padR = 14)
        {
            var rt = Box("Ligne", parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, bg.a > 0 ? bg : new Color(0, 0, 0, 0));
            if (height > 0) rt.GetComponent<LayoutElement>().preferredHeight = height;
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(padL, padR, 0, 0); h.spacing = spacing; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            return rt;
        }

        public static VerticalLayoutGroup Column(RectTransform rt, float spacing, int pad)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(pad, pad, pad, pad);
            v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false; v.childForceExpandWidth = true;
            return v;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, float size, Color c, TextAlignmentOptions align, float height = -1)
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

        public static Image Icon(Transform parent, M5Icons.Kind k, float size, Color c)
        {
            var img = new GameObject("Icone", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
            img.transform.SetParent(parent, false);
            img.sprite = M5Icons.Get(k); img.color = c; img.raycastTarget = false; img.preserveAspect = true;
            var rt = img.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = Vector2.one * size;
            var le = img.GetComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = size; le.minWidth = size;
            return img;
        }

        static void Colors(Button b)
        {
            var cb = b.colors; cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f); cb.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.55f); b.colors = cb;
        }

        /// Bouton rond à icône.
        public static Button IconButton(Transform parent, M5Icons.Kind k, float size, Color bg, Color fg, Action onClick)
        {
            var rt = Box("Bouton_" + k, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size, bg);
            var img = rt.GetComponent<Image>(); img.sprite = Circle; img.type = Image.Type.Simple;
            var le = rt.GetComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = size; le.minWidth = size;
            var b = rt.gameObject.AddComponent<Button>(); Colors(b);
            b.onClick.AddListener(() => onClick());
            Icon(rt, k, size * 0.5f, fg).GetComponent<LayoutElement>().ignoreLayout = true;
            return b;
        }

        /// Bouton flottant de verre (comme le dock M5), avec icône.
        public static Button FloatButton(Transform parent, M5Icons.Kind k, float size, Action onClick, out Image bg, out Image icon)
        {
            var rt = new GameObject("Bouton_" + k, typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = Vector2.one * size;
            var le = rt.GetComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = size;
            bg = Glassy(rt, Glass, Circle);
            bg.type = Image.Type.Simple;
            var b = rt.gameObject.AddComponent<Button>(); Colors(b);
            b.targetGraphic = bg;
            b.onClick.AddListener(() => onClick());
            icon = Icon(rt, k, size * 0.46f, Cream);
            icon.GetComponent<LayoutElement>().ignoreLayout = true;
            return b;
        }

        /// Bouton pilule plein (action principale) : icône facultative + texte.
        public static Button Pill(Transform parent, string text, Color c, Color fg, Action onClick, float height, float fontSize, M5Icons.Kind? icon = null, bool chevron = false)
        {
            var rt = Box("Bouton", parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, c);
            rt.GetComponent<Image>().sprite = PillShape;
            rt.GetComponent<LayoutElement>().preferredHeight = height;
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(26, 26, 0, 0); h.spacing = 12; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            var b = rt.gameObject.AddComponent<Button>(); Colors(b);
            b.onClick.AddListener(() => onClick());
            if (icon.HasValue) Icon(rt, icon.Value, fontSize * 1.4f, fg);
            var t = Label(rt, text, fontSize, fg, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            if (chevron) Icon(rt, M5Icons.Kind.Chevron, fontSize * 1.25f, fg);
            return b;
        }

        public static void Separator(Transform parent)
        {
            var s = Box("Separateur", parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0.1f));
            s.GetComponent<LayoutElement>().preferredHeight = 2;
            s.GetComponent<Image>().sprite = null;
        }

        public static Image Dot(Transform parent, Color c, float size)
        {
            var s = Box("Pastille", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size, c);
            var img = s.GetComponent<Image>(); img.sprite = Circle; img.type = Image.Type.Simple; img.raycastTarget = false;
            var le = s.GetComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = size; le.minWidth = size;
            return img;
        }

        /// Liste défilante (verticale ou horizontale). Le contenu s'agrandit avec ses enfants.
        public static ScrollRect Scroll(Transform parent, bool horizontal, float spacing, out RectTransform content)
        {
            var view = new GameObject("Liste", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(LayoutElement), typeof(ScrollRect)).GetComponent<RectTransform>();
            view.SetParent(parent, false);
            view.GetComponent<Image>().color = new Color(0, 0, 0, 0); // capte le glissé
            view.GetComponent<LayoutElement>().flexibleHeight = 1;
            content = new GameObject("Contenu", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(view, false);
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            if (horizontal)
            {
                content.anchorMin = Vector2.zero; content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0, 0.5f);
                content.offsetMin = content.offsetMax = Vector2.zero;
                var h = content.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = spacing; h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = true;
                fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            else
            {
                content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
                content.offsetMin = content.offsetMax = Vector2.zero;
                Column(content, spacing, 0);
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            var sr = view.GetComponent<ScrollRect>();
            sr.viewport = view; sr.content = content; sr.horizontal = horizontal; sr.vertical = !horizontal;
            sr.movementType = ScrollRect.MovementType.Elastic; sr.scrollSensitivity = 30f; sr.inertia = true;
            return sr;
        }

        public static TMP_InputField InputField(Transform parent, string placeholder, float fontSize)
        {
            var rt = new GameObject("Champ", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            rt.gameObject.SetActive(false); // références posées avant OnEnable du champ
            var area = new GameObject("Zone", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            area.SetParent(rt, false);
            Stretch(area);
            var ph = Label(area, placeholder, fontSize, new Color(1, 1, 1, 0.42f), TextAlignmentOptions.MidlineLeft);
            var txt = Label(area, "", fontSize, Cream, TextAlignmentOptions.MidlineLeft);
            foreach (var t in new[] { ph, txt }) { t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero; t.richText = false; t.textWrappingMode = TextWrappingModes.NoWrap; }
            var f = rt.gameObject.AddComponent<TMP_InputField>();
            f.textViewport = area; f.textComponent = txt; f.placeholder = ph;
            f.targetGraphic = rt.GetComponent<Image>();
            f.lineType = TMP_InputField.LineType.SingleLine;
            f.characterLimit = 60;
            f.customCaretColor = true; f.caretColor = Brass; f.caretWidth = 3;
            f.selectionColor = new Color(0.8f, 0.63f, 0.3f, 0.35f);
            rt.gameObject.SetActive(true);
            return f;
        }

        // ---------- Sprites (générés une fois) ----------
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
    }

    /// Garde l'interface hors de l'encoche et des bords arrondis du téléphone.
    public class SafeArea : MonoBehaviour
    {
        Rect last;
        void Update()
        {
            var s = Screen.safeArea;
            if (s == last || Screen.width == 0) return;
            last = s;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(s.xMin / Screen.width, s.yMin / Screen.height);
            rt.anchorMax = new Vector2(s.xMax / Screen.width, s.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    /// Notification en haut au centre (pastille de verre avec point coloré), comme dans la boutique.
    public class DecoToast : MonoBehaviour
    {
        RectTransform chip;
        TextMeshProUGUI text;
        Image dot;
        CanvasGroup group;
        float until, top;

        public static DecoToast Create(RectTransform root, float top)
        {
            var chip = DecoUI.Box("Notification", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 10f), new Vector2(400, 64), DecoUI.Glass);
            chip.pivot = new Vector2(0.5f, 1);
            var bg = chip.GetComponent<Image>(); bg.raycastTarget = false; bg.sprite = DecoUI.PillShape;
            DecoUI.Layer(chip, "Liseré", DecoUI.RingSprite, new Color(1, 1, 1, 0.14f), false);
            var t = chip.gameObject.AddComponent<DecoToast>();
            t.chip = chip; t.top = top;
            t.group = chip.gameObject.AddComponent<CanvasGroup>(); t.group.alpha = 0; t.group.blocksRaycasts = false;
            var d = DecoUI.Box("Point", chip, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(36, 0), new Vector2(14, 14), DecoUI.Brass);
            t.dot = d.GetComponent<Image>(); t.dot.sprite = DecoUI.Circle; t.dot.type = Image.Type.Simple; t.dot.raycastTarget = false;
            t.text = DecoUI.Label(chip, "", 25, DecoUI.Cream, TextAlignmentOptions.MidlineLeft);
            t.text.rectTransform.offsetMin = new Vector2(60, 0); t.text.rectTransform.offsetMax = new Vector2(-34, 0);
            t.text.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        public void Show(string msg, float duration = 2.5f, bool success = false)
        {
            text.text = msg;
            var pref = text.GetPreferredValues(msg, 860f, 0f);
            chip.sizeDelta = new Vector2(Mathf.Min(pref.x, 860f) + 94f, Mathf.Max(64f, pref.y + 30f));
            dot.color = success ? new Color(0.62f, 0.80f, 0.58f) : DecoUI.Brass;
            until = Time.unscaledTime + duration;
        }

        public void Hide() => until = 0f;

        void Update()
        {
            float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            bool on = Time.unscaledTime < until;
            group.alpha = Mathf.Lerp(group.alpha, on ? 1f : 0f, k);
            chip.anchoredPosition = new Vector2(0, Mathf.Lerp(chip.anchoredPosition.y, on ? -top : 10f, k));
        }
    }
}
