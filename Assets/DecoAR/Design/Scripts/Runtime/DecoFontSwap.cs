using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DecoAR.Design
{
    /// Outfit partout (docs/03-typographie.md) : les textes des scènes qui utilisent encore LiberationSans
    /// passent à Outfit au chargement, en gardant leur graisse (gras → SemiBold). Les textes créés par code
    /// prennent déjà Outfit, police par défaut de TextMeshPro.
    public static class DecoFontSwap
    {
        static TMP_FontAsset regular, semiBold;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            regular = DecoUI.Font(400);
            semiBold = DecoUI.Font(600);
            SceneManager.sceneLoaded += (s, _) => Swap(s);
            Swap(SceneManager.GetActiveScene());
        }

        static void Swap(Scene scene)
        {
            if (regular == null) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t.font == null || !t.font.name.StartsWith("LiberationSans")) continue;
                    bool bold = (t.fontStyle & FontStyles.Bold) != 0;
                    t.font = bold ? semiBold : regular;
                    if (bold) t.fontStyle &= ~FontStyles.Bold; // la graisse vient de la police
                }
        }
    }
}
