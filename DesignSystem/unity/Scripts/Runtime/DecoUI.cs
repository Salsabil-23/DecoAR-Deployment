// DecoAR Design System — fabrique de composants uGUI + TextMeshPro.
// Prérequis (voir docs/12-integration-unity.md) :
//   Resources/DecoAR/UI/      -> sprites 9-slice (assets/unity-sprites)
//   Resources/DecoAR/Icons/   -> icônes blanches (icons/png-unity/128)
//   Resources/DecoAR/Fonts/   -> font assets TMP « Outfit-Regular SDF », « Outfit-Medium SDF », « Outfit-SemiBold SDF », « Outfit-Bold SDF »
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DecoAR.Design
{
    public enum DecoTheme { Verre, Papier }
    public enum DecoButtonKind { Principal, Commerce, Accent, Secondaire, Fantome }

    public static class DecoUI
    {
        const string UiPath = "DecoAR/UI/";
        const string IconPath = "DecoAR/Icons/dc-icon-";
        const string FontPath = "DecoAR/Fonts/Outfit-";

        // ------------------------------------------------------------------ ressources
        public static TMP_FontAsset Font(int weight)
        {
            string n = weight >= 700 ? "Bold" : weight >= 600 ? "SemiBold" : weight >= 500 ? "Medium" : "Regular";
            var f = Resources.Load<TMP_FontAsset>(FontPath + n + " SDF");
            return f != null ? f : TMP_Settings.defaultFontAsset;
        }

        public static Sprite UiSprite(string name) => Resources.Load<Sprite>(UiPath + name);
        public static Sprite Icon(string name) => Resources.Load<Sprite>(IconPath + name);

        /// <summary>Choisit le sprite arrondi le plus proche et règle le multiplicateur pour obtenir exactement le rayon voulu.</summary>
        public static void SetRadius(Image img, float radius)
        {
            int[] steps = { 12, 20, 28, 40, 56 };
            int best = steps[steps.Length - 1];
            foreach (var s in steps) if (s >= radius) { best = s; break; }
            img.sprite = UiSprite("dc-rect-r" + best);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = best / Mathf.Max(1f, radius);
        }

        // ------------------------------------------------------------------ couleurs par thème
        public static Color TextPrimary(DecoTheme t) => t == DecoTheme.Verre ? ThemeVerre.TextPrimary : ThemePapier.TextPrimary;
        public static Color TextSecondary(DecoTheme t) => t == DecoTheme.Verre ? ThemeVerre.TextSecondary : ThemePapier.TextSecondary;

        static void ButtonColors(DecoButtonKind k, DecoTheme t, out Color bg, out Color fg)
        {
            bool v = t == DecoTheme.Verre;
            switch (k)
            {
                case DecoButtonKind.Principal: bg = v ? ThemeVerre.ActionPrimary : ThemePapier.ActionPrimary; fg = v ? ThemeVerre.ActionOnPrimary : ThemePapier.ActionOnPrimary; break;
                case DecoButtonKind.Commerce: bg = v ? ThemeVerre.ActionCommerce : ThemePapier.ActionCommerce; fg = v ? ThemeVerre.ActionOnCommerce : ThemePapier.ActionOnCommerce; break;
                case DecoButtonKind.Accent: bg = v ? ThemeVerre.ActionAccent : ThemePapier.ActionAccent; fg = v ? ThemeVerre.ActionOnAccent : ThemePapier.ActionOnAccent; break;
                case DecoButtonKind.Secondaire: bg = v ? ThemeVerre.ActionSecondary : ThemePapier.ActionSecondary; fg = v ? ThemeVerre.ActionOnSecondary : ThemePapier.ActionOnSecondary; break;
                default: bg = Color.clear; fg = v ? ThemeVerre.TextPrimary : ThemePapier.ActionLink; break;
            }
        }

        // ------------------------------------------------------------------ briques
        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent != null ? parent.gameObject.layer : 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static TextMeshProUGUI Text(Transform parent, string text, DecoType.Style style, Color color,
                                           TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var rt = Rect(parent, "Texte");
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font(style.Weight);
            t.fontSize = style.Size;
            t.characterSpacing = style.TmpCharacterSpacing;
            t.lineSpacing = style.TmpLineSpacing;
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.raycastTarget = false;
            return t;
        }

        public static Image Surface(Transform parent, string name, Color color, float radius)
        {
            var img = Rect(parent, name).gameObject.AddComponent<Image>();
            SetRadius(img, radius);
            img.color = color;
            return img;
        }

        /// <summary>Surface de verre (thème Verre) : ombre douce + fond 80 % + liseré clair.</summary>
        public static Image Glass(Transform parent, string name, float radius, bool strong = false)
        {
            var root = Rect(parent, name);
            var shadow = Surface(root, "Ombre", new Color(0, 0, 0, 0.38f), radius);
            shadow.sprite = UiSprite(radius > 40 ? "dc-ombre-r56" : "dc-ombre-r28");
            Stretch(shadow.rectTransform, -18, -12, -18, -24);
            shadow.raycastTarget = false;
            var bg = Surface(root, "Fond", strong ? ThemeVerre.SurfaceGlassStrong : ThemeVerre.SurfaceGlass, radius);
            Stretch(bg.rectTransform);
            var line = Surface(root, "Liseré", ThemeVerre.BorderHairline, radius);
            line.sprite = UiSprite("dc-rect-r" + NearestStep(radius) + "-contour");
            Stretch(line.rectTransform);
            line.raycastTarget = false;
            return bg;
        }

        static int NearestStep(float r) { int[] s = { 12, 20, 28, 40, 56 }; foreach (var v in s) if (v >= r) return v; return 56; }

        public static void Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        // ------------------------------------------------------------------ composants
        /// <summary>Bouton pilule. Hauteur 112 (ou 88 en compact), icône optionnelle à gauche.</summary>
        public static UnityEngine.UI.Button Button(Transform parent, string label, DecoButtonKind kind, DecoTheme theme,
                                    string icon = null, bool compact = false)
        {
            ButtonColors(kind, theme, out var bg, out var fg);
            float h = compact ? DecoSize.ButtonHeightCompact : DecoSize.ButtonHeight;
            var img = Surface(parent, "Bouton " + label, bg, h / 2f);
            img.rectTransform.sizeDelta = new Vector2(0, h);
            var btn = img.gameObject.AddComponent<UnityEngine.UI.Button>();
            var cb = btn.colors;
            cb.normalColor = Color.white; cb.highlightedColor = Color.white;
            cb.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f); cb.disabledColor = new Color(1, 1, 1, 0.55f);
            cb.fadeDuration = DecoMotion.Instant;
            btn.colors = cb;

            var row = img.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset((int)DecoSpace.S6, (int)DecoSpace.S6, 0, 0);
            row.spacing = DecoSpace.S2;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var fit = img.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (!string.IsNullOrEmpty(icon)) IconImage(img.transform, icon, fg, DecoSize.IconMd);
            var txt = Text(img.transform, label, DecoType.Bouton, fg, TextAlignmentOptions.Center);
            txt.textWrappingMode = TextWrappingModes.NoWrap;
            var le = img.gameObject.AddComponent<LayoutElement>(); le.minHeight = h; le.preferredHeight = h;
            if (kind == DecoButtonKind.Fantome) img.color = new Color(1, 1, 1, 0); // reste cliquable
            return btn;
        }

        /// <summary>Bouton rond flottant du HUD (verre, 112).</summary>
        public static UnityEngine.UI.Button GlassIconButton(Transform parent, string icon, float size = DecoSize.HudButton)
        {
            var bg = Glass(parent, "Bouton " + icon, size / 2f);
            var root = (RectTransform)bg.transform.parent;
            root.sizeDelta = new Vector2(size, size);
            var btn = bg.gameObject.AddComponent<UnityEngine.UI.Button>();
            var ic = IconImage(bg.transform, icon, ThemeVerre.IconDefault, DecoSize.IconMd);
            ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            return btn;
        }

        public static Image IconImage(Transform parent, string icon, Color color, float size)
        {
            var img = Rect(parent, "Icône " + icon).gameObject.AddComponent<Image>();
            img.sprite = Icon(icon);
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = size; le.minWidth = le.minHeight = size;
            return img;
        }

        /// <summary>Pastille compteur (panier). Affiche « 9+ » au-delà de 9.</summary>
        public static TextMeshProUGUI Badge(Transform parent, int count)
        {
            var img = Surface(parent, "Pastille", ThemeVerre.BadgeBg, DecoSize.Badge / 2f);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-14, -14);
            rt.sizeDelta = new Vector2(DecoSize.Badge, DecoSize.Badge);
            var t = Text(img.transform, count > 9 ? "9+" : count.ToString(), new DecoType.Style(24, 700, 1f, 0f),
                         ThemeVerre.BadgeText, TextAlignmentOptions.Center);
            Stretch(t.rectTransform, 4, 0, 4, 0);
            return t;
        }
    }

    /// <summary>Notification en haut au centre. Usage : DecoToast.Show(canvas.transform, "Monstera ajouté au panier", DecoToast.Kind.Succes);</summary>
    public class DecoToast : MonoBehaviour
    {
        public enum Kind { Info, Succes, Erreur }
        static DecoToast _current;

        public static void Show(Transform canvas, string message, Kind kind = Kind.Info)
        {
            if (_current != null) Destroy(_current.gameObject);
            var bg = DecoUI.Glass(canvas, "Toast", 48f);
            var root = (RectTransform)bg.transform.parent;
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.sizeDelta = new Vector2(760, 96);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            Color dot = kind == Kind.Succes ? ThemeVerre.FeedbackSuccess : kind == Kind.Erreur ? ThemeVerre.FeedbackError : ThemeVerre.FeedbackWarning;
            var d = DecoUI.Surface(bg.transform, "Point", dot, 10);
            d.rectTransform.anchorMin = d.rectTransform.anchorMax = new Vector2(0, 0.5f);
            d.rectTransform.sizeDelta = new Vector2(20, 20);
            d.rectTransform.anchoredPosition = new Vector2(44, 0);
            var t = DecoUI.Text(bg.transform, message, DecoType.Corps, ThemeVerre.TextPrimary, TextAlignmentOptions.Left);
            DecoUI.Stretch(t.rectTransform, 72, 0, 32, 0);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis;

            _current = root.gameObject.AddComponent<DecoToast>();
            _current.StartCoroutine(_current.Run(root, group));
        }

        IEnumerator Run(RectTransform rt, CanvasGroup g)
        {
            float top = -DecoSize.ScreenMargin, start = top + 60f;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / DecoMotion.Base)
            {
                float e = DecoMotion.EaseEntree(t);
                rt.anchoredPosition = new Vector2(0, Mathf.Lerp(start, top, e)); g.alpha = e; yield return null;
            }
            rt.anchoredPosition = new Vector2(0, top); g.alpha = 1;
            yield return new WaitForSecondsRealtime(DecoMotion.ToastDuree);
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / DecoMotion.Rapide)
            { g.alpha = 1f - DecoMotion.EaseSortie(t); yield return null; }
            Destroy(gameObject);
        }
    }

    /// <summary>« Respiration » des boutons d'action (±2,5 %). À ajouter sur le bouton.</summary>
    public class DecoBreathe : MonoBehaviour
    {
        public float amplitude = DecoMotion.RespirationAmplitude;
        public float periode = DecoMotion.RespirationPeriode;
        Vector3 _base;
        void OnEnable() { _base = transform.localScale; }
        void OnDisable() { transform.localScale = _base; }
        void Update()
        {
            float s = 1f + amplitude * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / periode);
            transform.localScale = _base * s;
        }
    }

    /// <summary>Rebond amorti (pastille compteur, ajout au panier). Appeler Play().</summary>
    public class DecoPop : MonoBehaviour
    {
        public float force = 0.28f;
        public void Play() { StopAllCoroutines(); StartCoroutine(Run()); }
        IEnumerator Run()
        {
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / DecoMotion.Lent)
            {
                float k = 1f + force * (1f - DecoMotion.Ressort(t));
                transform.localScale = Vector3.one * k; yield return null;
            }
            transform.localScale = Vector3.one;
        }
    }
}
