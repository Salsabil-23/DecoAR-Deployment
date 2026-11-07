using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DecoAR.Design
{
    /// Le logo partout (docs/07-logo.md), sans toucher aux scènes des autres modules :
    ///  - l'ancien rond « Logo » (connexion) devient le symbole « Maison AR » ;
    ///  - chaque texte « DecoAR » (interface et enseignes 3D) prend le mot-symbole : « Deco » encre ou crème,
    ///    « AR » terracotta, Outfit SemiBold ; à l'écran, le symbole se place devant le nom.
    public static class DecoBrand
    {
        const string Symbol = "DecoAR/Brand/decoar-symbole-sans-ombre-256";
        static Sprite symbol;

        public static Sprite SymbolSprite => symbol ? symbol : symbol = Resources.Load<Sprite>(Symbol);

        /// « Deco » + « AR » aux couleurs de la marque, selon que le fond est clair ou sombre.
        public static string Wordmark(bool onDark) => onDark
            ? $"<color=#{ColorUtility.ToHtmlStringRGB(DecoPalette.Lin100)}>Deco</color><color=#{ColorUtility.ToHtmlStringRGB(DecoPalette.Terracotta300)}>AR</color>"
            : $"<color=#{ColorUtility.ToHtmlStringRGB(DecoPalette.Lin900)}>Deco</color><color=#{ColorUtility.ToHtmlStringRGB(DecoPalette.Terracotta500)}>AR</color>";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded += (s, _) => Apply(s);
            Apply(SceneManager.GetActiveScene());
        }

        static void Apply(Scene scene)
        {
            if (SymbolSprite == null) return;
            foreach (var root in scene.GetRootGameObjects())
            {
                // Ancien logo rond (connexion)
                foreach (var img in root.GetComponentsInChildren<Image>(true))
                {
                    if (img.name != "Logo" || img.sprite == null || img.sprite.name != "Circle") continue;
                    img.sprite = SymbolSprite;
                    img.color = Color.white;
                    img.preserveAspect = true;
                    foreach (Transform c in img.transform) c.gameObject.SetActive(false);
                }

                // Nom de la marque
                foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t.text.Trim() != "DecoAR") continue;
                    var c = t.color;
                    bool onDark = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b > 0.6f; // texte clair = fond sombre
                    t.text = Wordmark(onDark);
                    t.color = Color.white;
                    var f = DecoUI.Font(600);
                    if (f != null) t.font = f;
                    t.fontStyle &= ~FontStyles.Bold;
                    if (t is TextMeshProUGUI ui) AddSymbol(ui);
                }
            }
        }

        /// Symbole devant le nom, à la hauteur du texte.
        static void AddSymbol(TextMeshProUGUI t)
        {
            if (t.transform.Find("Symbole DecoAR") != null) return;
            float h = t.fontSize * 1.1f;
            // Texte calé à gauche (souvent contre le bord de l'écran) : il se décale pour laisser la place au symbole
            bool left = (t.alignment & (TextAlignmentOptions)0x1) != 0 || t.alignment.ToString().Contains("Left");
            if (left) { var m = t.margin; m.x += h * 1.2f; t.margin = m; }
            t.ForceMeshUpdate();
            var img = new GameObject("Symbole DecoAR", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(t.transform, false);
            img.sprite = SymbolSprite; img.preserveAspect = true; img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(h, h);
            var b = t.textBounds;
            rt.localPosition = new Vector3(b.min.x - h * 0.62f, b.center.y, 0f); // textBounds : repère local du texte
        }
    }
}
