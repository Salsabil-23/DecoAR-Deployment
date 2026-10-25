using DecoAR.Core;
using DecoAR.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DecoAR.M3
{
    // Lien M1/M2 -> M3 : l'interaction avec une etagere de la boutique ouvre le rayon de sa zone.
    // Id de l'Interactable : "etagere" (premiere zone), "etagere_tableaux", "etagere_luminaires", "etagere_deco".
    public static class M3ShelfEntrance
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            Interactable.AnyInteracted -= OnInteracted;
            Interactable.AnyInteracted += OnInteracted;
        }

        static void OnInteracted(Interactable it)
        {
            if (it == null || !it.Id.StartsWith("etagere")) return;

            var gm = GameManager.Instance;
            if (gm != null) gm.CurrentCategory = CategoryFromId(it.Id);

            // Retour a la meme place apres le rayon ou l'AR
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null) player.SaveShopPose();

            SceneManager.LoadScene(SceneNames.Catalog);
        }

        static string CategoryFromId(string id)
        {
            string n = M3ProductCatalog.Normalize(id);
            if (n.Contains("tableau")) return M3Categories.Tableaux;
            if (n.Contains("lumin") || n.Contains("lampe")) return M3Categories.Luminaires;
            if (n.Contains("deco") || n.Contains("objet")) return M3Categories.ObjetsDeco;
            return "";
        }
    }
}
