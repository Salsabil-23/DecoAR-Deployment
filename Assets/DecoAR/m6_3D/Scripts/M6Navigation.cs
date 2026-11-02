using DecoAR.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DecoAR.M6
{
    // Liaison du module M6 (generation 3D d'un produit a partir d'une photo) avec le reste de l'application,
    // sans toucher aux scenes : les boutons manquants sont ajoutes au chargement.
    //  Profil (Auth) -> "Espace administration (3D)" -> AdminScene
    //  AdminScene : "Generer 3D" -> M6_Generator ; "‹ Boutique" -> boutique (SceneNames.Shop)
    //  M6_Generator : "‹ Administration" -> AdminScene
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
        }

        static void Hook(string scene)
        {
            if (scene == ProfileScene) HookProfile();
            else if (scene == AdminScene) HookAdmin();
            else if (scene == GeneratorScene) HookGenerator();
        }

        // Profil : une entree de plus sous "Changer le mot de passe"
        static void HookProfile()
        {
            var template = Find("Bouton Changer le mot de passe");
            if (template == null) return;
            var button = Clone(template, template.transform.parent, "Bouton Espace administration", "Espace administration (3D)");
            button.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            button.onClick.AddListener(() => SceneManager.LoadScene(AdminScene));
        }

        static void HookAdmin()
        {
            var generate = Find("Generate3DButton");
            if (generate == null) return;
            generate.onClick.AddListener(() => SceneManager.LoadScene(GeneratorScene));
            var back = Clone(generate, RootCanvas(generate), "BoutonRetourBoutique", "‹ Boutique");
            PlaceTopLeft(back);
            back.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Shop));
        }

        static void HookGenerator()
        {
            var template = Find("GalleryButton");
            if (template == null) return;
            var back = Clone(template, RootCanvas(template), "BoutonRetourAdministration", "‹ Administration");
            PlaceTopLeft(back);
            back.onClick.AddListener(() => SceneManager.LoadScene(AdminScene));
        }

        static Button Find(string name)
        {
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (b.name == name) return b;
            Debug.LogWarning($"[M6] Bouton '{name}' introuvable : liaison de scene ignoree");
            return null;
        }

        static Transform RootCanvas(Component c)
        {
            var canvas = c.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas.transform : c.transform.parent;
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

        static void PlaceTopLeft(Button button)
        {
            var element = button.GetComponent<LayoutElement>();
            if (element == null) element = button.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;
            var rt = (RectTransform)button.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(24f, -24f);
            rt.sizeDelta = new Vector2(320f, 90f);
        }
    }
}
