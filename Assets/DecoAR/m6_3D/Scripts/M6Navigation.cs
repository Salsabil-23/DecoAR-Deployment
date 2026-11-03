using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DecoAR.M6
{
    // Liaison du module M6 (generation 3D d'un produit a partir d'une photo) avec le reste de l'application,
    // sans toucher aux scenes : les boutons manquants sont ajoutes au chargement.
    //  Connexion (Auth) -> "Espace administration (catalogue 3D)" -> AdminScene (retour : "‹ Connexion")
    //  Profil (Auth) -> "Espace administration (3D)" -> AdminScene
    //  AdminScene / M6_Generator : interface et retours construits par M6AdminUI
    public static class M6Navigation
    {
        public const string AdminScene = "AdminScene";
        public const string GeneratorScene = "M6_Generator";
        const string ProfileScene = "Profile";
        const string AuthScene = "Auth";
        /// Espace admin ouvert depuis l'écran de connexion (pas encore connecté) : retour « ‹ Connexion ».
        public static bool FromLogin { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded += (scene, _) => Hook(scene.name);
            Hook(SceneManager.GetActiveScene().name);
        }

        static void Hook(string scene)
        {
            if (scene == AuthScene) HookLogin();
            else if (scene == ProfileScene) HookProfile();
            // AdminScene et M6_Generator : navigation fournie par M6AdminUI
        }

        // Connexion : un lien sous "Pas encore de compte ? S'inscrire"
        static void HookLogin()
        {
            var screen = Object.FindAnyObjectByType<DecoAR.Auth.AuthScreen>();
            var field = typeof(DecoAR.Auth.AuthScreen).GetField("goToRegisterButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var template = screen != null && field != null ? field.GetValue(screen) as Button : null;
            if (template == null) { Debug.LogWarning("[M6] Lien d'inscription introuvable : bouton admin non ajouté"); return; }
            var button = Clone(template, template.transform.parent, "Bouton Espace administration (connexion)", "<b><color=#C8734A>Espace administration</color></b>  (catalogue 3D) ›");
            button.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            button.onClick.AddListener(() => { FromLogin = true; SceneManager.LoadScene(AdminScene); });
        }

        // Profil : une entree de plus sous "Changer le mot de passe"
        static void HookProfile()
        {
            var template = Find("Bouton Changer le mot de passe");
            if (template == null) return;
            var button = Clone(template, template.transform.parent, "Bouton Espace administration", "Espace administration (3D)");
            button.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            button.onClick.AddListener(() => { FromLogin = false; SceneManager.LoadScene(AdminScene); });
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
