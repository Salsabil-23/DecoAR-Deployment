using DecoAR.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DecoAR.M6
{
    // Liaison du module M6 (generation 3D d'un produit a partir d'une photo) avec le reste de l'application,
    // sans toucher aux scenes : les boutons manquants sont ajoutes au chargement.
    //  Boutique (caisse) : PC derrière le comptoir (M5AdminTerminal) -> AdminScene
    //  Profil (Auth), comptes admin seulement -> "Espace administration (3D)" -> AdminScene
    //  AdminScene / M6_Generator : interface et retours construits par M6AdminUI
    // L'espace admin demande un compte admin ET une confirmation par le visage (AuthOverlay.ConfirmAdmin) :
    // sans jeton admin valide, ces scenes renvoient a la boutique.
    public static class M6Navigation
    {
        public const string AdminScene = "AdminScene";
        public const string GeneratorScene = "M6_Generator";
        const string ProfileScene = "Profile";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded += (scene, _) => Hook(scene.name);
            Hook(SceneManager.GetActiveScene().name);
            // Le profil recharge le compte depuis le serveur : le role admin peut arriver apres l'ouverture
            AuthSession.Changed += _ => { if (SceneManager.GetActiveScene().name == ProfileScene) HookProfile(); };
        }

        static void Hook(string scene)
        {
            if (scene == ProfileScene) HookProfile();
            else if ((scene == AdminScene || scene == GeneratorScene) && !AuthSession.HasAdminAccess)
            {
                Debug.LogWarning("[M6] Espace admin sans confirmation : retour a la boutique");
                SceneManager.LoadScene(DecoAR.Core.SceneNames.Shop);
            }
            // AdminScene et M6_Generator : navigation fournie par M6AdminUI
        }

        /// Ouvre l'espace admin apres la confirmation par le visage (ou directement si elle est encore valide).
        public static void OpenAdmin()
        {
            if (AuthOverlay.Instance == null) return;
            AuthOverlay.Instance.ConfirmAdmin(ok => { if (ok) SceneManager.LoadScene(AdminScene); });
        }

        // Profil d'un compte admin : une entree de plus sous "Changer le mot de passe"
        static void HookProfile()
        {
            if (!AuthSession.IsAdmin || GameObject.Find("Bouton Espace administration") != null) return;
            var template = Find("Bouton Changer le mot de passe");
            if (template == null) return;
            var button = Clone(template, template.transform.parent, "Bouton Espace administration", "Espace administration (3D)");
            button.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            button.onClick.AddListener(OpenAdmin);
        }

        static Button Find(string name)
        {
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (b.name == name) return b;
            Debug.LogWarning($"[M6] Bouton '{name}' introuvable : liaison de scene ignoree");
            return null;
        }

        static Button Clone(Button template, Transform parent, string name, string label)
        {
            var button = Object.Instantiate(template, parent);
            button.name = name;
            button.onClick = new Button.ButtonClickedEvent(); // sans les actions du bouton copie
            button.gameObject.SetActive(true);
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null) text.text = label;
            return button;
        }
    }
}
