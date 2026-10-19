using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DecoAR.Player.EditorTools
{
    // Menu DecoAR > M1 : genere le controleur d'animation, le catalogue d'avatars,
    // le prefab M1_PlayerRig et les scenes de test, sans rien construire a la main.
    public static class M1SceneBuilder
    {
        const string Root = "Assets/DecoAR/M1_Player";
        const string AnimFolder = Root + "/Animations";
        const string DataFolder = Root + "/Data";
        const string MatFolder = Root + "/Materials";
        const string PrefabFolder = Root + "/Prefabs";
        const string SceneFolder = Root + "/Scenes";
        const string AvatarsFolder = M1MixamoImporter.MixamoFolder + "/Avatars";
        const string ClipsFolder = M1MixamoImporter.MixamoFolder + "/Animations";

        const string ControllerPath = AnimFolder + "/M1_Avatar.controller";
        const string CatalogPath = DataFolder + "/M1_AvatarCatalog.asset";
        const string RigPrefabPath = PrefabFolder + "/M1_PlayerRig.prefab";
        const string TestScenePath = SceneFolder + "/M1_Test.unity";
        const string SelectionScenePath = SceneFolder + "/M1_AvatarSelection.unity";

        const int IgnoreRaycastLayer = 2;

        // ---------------------------------------------------------------- menus

        [MenuItem("DecoAR/M1/Tout generer")]
        public static void BuildAll()
        {
            BuildTestScene();
            BuildSelectionScene();
            Debug.Log("[M1] Generation terminee");
        }

        [MenuItem("DecoAR/M1/Mettre a jour animations et catalogue")]
        public static void UpdateAssets()
        {
            EnsureFolders();
            EnsureCatalog(EnsureController());
            AssetDatabase.SaveAssets();
        }

        [MenuItem("DecoAR/M1/Creer la scene de test (boutique vide)")]
        public static void BuildTestScene()
        {
            EnsureFolders();
            var catalog = EnsureCatalog(EnsureController());

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddSun();

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Sol";
            floor.transform.localScale = new Vector3(2f, 1f, 2f);
            Paint(floor, "M1_Sol", new Color(0.75f, 0.72f, 0.68f));

            var walls = new GameObject("Murs").transform;
            Box("Mur Nord", walls, new Vector3(0, 1.5f, 10), new Vector3(20, 3, 0.3f), "M1_Mur", new Color(0.92f, 0.9f, 0.86f));
            Box("Mur Sud", walls, new Vector3(0, 1.5f, -10), new Vector3(20, 3, 0.3f), "M1_Mur", new Color(0.92f, 0.9f, 0.86f));
            Box("Mur Est", walls, new Vector3(10, 1.5f, 0), new Vector3(0.3f, 3, 20), "M1_Mur", new Color(0.92f, 0.9f, 0.86f));
            Box("Mur Ouest", walls, new Vector3(-10, 1.5f, 0), new Vector3(0.3f, 3, 20), "M1_Mur", new Color(0.92f, 0.9f, 0.86f));

            var furniture = new GameObject("Meubles").transform;
            Box("Table", furniture, new Vector3(2, 0.4f, -1), new Vector3(1.6f, 0.8f, 1f), "M1_Meuble", new Color(0.55f, 0.38f, 0.25f));
            Box("Pilier", furniture, new Vector3(-3, 1.5f, -2), new Vector3(0.6f, 3f, 0.6f), "M1_Meuble", new Color(0.55f, 0.38f, 0.25f));

            var shelf = Box("Etagere (test)", null, new Vector3(-5, 1f, 8.5f), new Vector3(3f, 2f, 0.6f), "M1_Etagere", new Color(0.35f, 0.5f, 0.7f));
            AddInteractable(shelf, "etagere", "Voir l'étagère");

            var counter = Box("Comptoir caisse (test)", null, new Vector3(5, 0.55f, 8.5f), new Vector3(2.5f, 1.1f, 0.8f), "M1_Comptoir", new Color(0.7f, 0.45f, 0.35f));
            AddInteractable(counter, "caisse", "Passer à la caisse");

            var rig = BuildPlayerRig(catalog);
            rig.transform.position = new Vector3(0, 0, -6);
            PrefabUtility.SaveAsPrefabAssetAndConnect(rig, RigPrefabPath, InteractionMode.AutomatedAction);

            AddEventSystem();
            SaveScene(TestScenePath);
        }

        [MenuItem("DecoAR/M1/Creer la scene de choix d'avatar")]
        public static void BuildSelectionScene()
        {
            EnsureFolders();
            var catalog = EnsureCatalog(EnsureController());

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddSun();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.17f, 0.2f);
            camGo.AddComponent<AudioListener>();
            camGo.transform.SetPositionAndRotation(new Vector3(0, 1.1f, 3.4f), Quaternion.Euler(6f, 180f, 0f));

            var podium = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            podium.name = "Podium";
            podium.transform.position = new Vector3(0, -0.05f, 0);
            podium.transform.localScale = new Vector3(1.6f, 0.05f, 1.6f);
            Paint(podium, "M1_Podium", new Color(0.85f, 0.83f, 0.8f));

            var previewRoot = new GameObject("PreviewRoot").transform;

            var canvas = NewCanvas("UI", null);
            var ct = canvas.transform;

            var title = MakeLabel(MakeRect("Titre", ct, new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(1200, 120)), "Choisis ton avatar", 64);
            title.fontStyle = FontStyles.Bold;

            var prev = MakeButton(ct, "Precedent", "<", new Vector2(0, 0.5f), new Vector2(150, 0), new Vector2(140, 140), out _);
            var next = MakeButton(ct, "Suivant", ">", new Vector2(1, 0.5f), new Vector2(-150, 0), new Vector2(140, 140), out _);

            var nameText = MakeLabel(MakeRect("Nom", ct, new Vector2(0.5f, 1), new Vector2(0, -190), new Vector2(900, 90)), "Avatar", 48);

            var colors = MakeRect("Couleurs", ct, new Vector2(0.5f, 0), new Vector2(0, 180), new Vector2(900, 110));
            var layout = colors.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var template = MakeButton(colors, "Couleur", "", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100), out _);
            template.image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var confirm = MakeButton(ct, "Valider", "Entrer dans la boutique", new Vector2(1, 0), new Vector2(-260, 90), new Vector2(440, 120), out _);
            confirm.image.color = new Color(0.25f, 0.6f, 0.4f);

            var menu = canvas.gameObject.AddComponent<AvatarSelectionMenu>();
            Set(menu, "catalog", catalog);
            Set(menu, "previewRoot", previewRoot);
            Set(menu, "nameText", nameText);
            Set(menu, "previousButton", prev);
            Set(menu, "nextButton", next);
            Set(menu, "colorButtonsParent", colors);
            Set(menu, "colorButtonTemplate", template);
            Set(menu, "confirmButton", confirm);
            SetString(menu, "shopSceneName", Path.GetFileNameWithoutExtension(TestScenePath));

            AddEventSystem();
            SaveScene(SelectionScenePath);
        }

        // ---------------------------------------------------------------- joueur

        static GameObject BuildPlayerRig(AvatarCatalog catalog)
        {
            var rig = new GameObject("M1_PlayerRig");

            // Avatar
            var player = new GameObject("Player") { layer = IgnoreRaycastLayer }; // la camera ne doit pas buter dessus
            player.transform.SetParent(rig.transform, false);
            var cc = player.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.9f, 0);
            cc.height = 1.8f;
            cc.radius = 0.3f;

            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            placeholder.name = "Placeholder";
            Object.DestroyImmediate(placeholder.GetComponent<Collider>());
            placeholder.layer = IgnoreRaycastLayer;
            placeholder.transform.SetParent(player.transform, false);
            placeholder.transform.localPosition = new Vector3(0, 0.9f, 0);
            placeholder.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            Paint(placeholder, "M1_Placeholder", new Color(0.95f, 0.6f, 0.3f));
            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube); // indique l'avant
            nose.name = "Avant";
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.transform.SetParent(placeholder.transform, false);
            nose.transform.localPosition = new Vector3(0, 0.55f, 0.5f);
            nose.transform.localScale = new Vector3(0.5f, 0.15f, 0.3f);

            var mount = new GameObject("AvatarMount").transform;
            mount.SetParent(player.transform, false);

            var controller = player.AddComponent<PlayerController>();
            var interactor = player.AddComponent<PlayerInteractor>();
            var loader = player.AddComponent<PlayerAvatarLoader>();
            Set(loader, "catalog", catalog);
            Set(loader, "avatarMount", mount);
            Set(loader, "placeholder", placeholder);

            // Camera
            var camGo = new GameObject("PlayerCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(rig.transform, false);
            camGo.transform.localPosition = new Vector3(0, 2.3f, -3.1f);
            camGo.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            camGo.AddComponent<Camera>().nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            var tpc = camGo.AddComponent<ThirdPersonCamera>();
            Set(tpc, "target", player.transform);

            // HUD
            var canvas = NewCanvas("HUD", rig.transform);
            var ct = canvas.transform;

            var joyBg = MakeRect("Joystick", ct, Vector2.zero, new Vector2(230, 230), new Vector2(320, 320));
            var bgImg = joyBg.gameObject.AddComponent<Image>();
            bgImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            bgImg.color = new Color(1, 1, 1, 0.25f);
            var handle = MakeRect("Handle", joyBg, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 140));
            var hImg = handle.gameObject.AddComponent<Image>();
            hImg.sprite = bgImg.sprite;
            hImg.color = new Color(1, 1, 1, 0.7f);
            hImg.raycastTarget = false;
            var joystick = joyBg.gameObject.AddComponent<VirtualJoystick>();
            Set(joystick, "background", joyBg);
            Set(joystick, "handle", handle);
            Set(controller, "joystick", joystick);

            var interact = MakeButton(ct, "BoutonInteraction", "Interagir", new Vector2(1, 0), new Vector2(-240, 230), new Vector2(360, 140), out var interactLabel);
            interact.image.color = new Color(0.25f, 0.6f, 0.4f);
            Set(interactor, "interactButton", interact);
            Set(interactor, "interactLabel", interactLabel);

            var top = new Vector2(1, 1);
            var settings = MakeButton(ct, "Reglages", "Réglages", top, new Vector2(-140, -80), new Vector2(220, 110), out _);
            var gyro = MakeButton(ct, "Gyro", "Gyro : OFF", top, new Vector2(-420, -80), new Vector2(300, 110), out var gyroLabel);
            var map = MakeButton(ct, "MiniCarte", "Carte", top, new Vector2(-700, -80), new Vector2(220, 110), out _);
            var cart = MakeButton(ct, "Panier", "Panier", top, new Vector2(-960, -80), new Vector2(240, 110), out _);

            var badge = MakeRect("Badge", cart.transform, new Vector2(1, 1), new Vector2(-10, -10), new Vector2(60, 60));
            var badgeImg = badge.gameObject.AddComponent<Image>();
            badgeImg.sprite = bgImg.sprite;
            badgeImg.color = new Color(0.85f, 0.2f, 0.2f);
            badgeImg.raycastTarget = false;
            var badgeText = MakeLabel(badge, "0", 34);

            // Panneau parametres
            var panel = MakeRect("PanneauReglages", ct, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            var box = MakeRect("Fenetre", panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 620));
            box.gameObject.AddComponent<Image>().color = new Color(0.97f, 0.96f, 0.94f);
            var pTitle = MakeLabel(MakeRect("Titre", box, new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(700, 90)), "Paramètres", 52);
            pTitle.color = Color.black;
            var haptics = MakeButton(box, "Vibrations", "Vibrations : ON", new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(560, 110), out var hapticsLabel);
            var recal = MakeButton(box, "Recalibrer", "Recalibrer le gyroscope", new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(560, 110), out _);
            var close = MakeButton(box, "Fermer", "Fermer", new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(300, 110), out _);

            var hud = canvas.gameObject.AddComponent<HUDController>();
            Set(hud, "cartButton", cart);
            Set(hud, "cartBadge", badge.gameObject);
            Set(hud, "cartCountText", badgeText);
            Set(hud, "minimapButton", map);
            Set(hud, "gyroButton", gyro);
            Set(hud, "gyroLabel", gyroLabel);
            Set(hud, "settingsButton", settings);
            Set(hud, "settingsPanel", panel.gameObject);
            Set(hud, "hapticsButton", haptics);
            Set(hud, "hapticsLabel", hapticsLabel);
            Set(hud, "recalibrateButton", recal);
            Set(hud, "closeSettingsButton", close);
            Set(hud, "playerCamera", tpc);

            return rig;
        }

        static void AddInteractable(GameObject go, string id, string prompt)
        {
            var it = go.AddComponent<Interactable>();
            SetString(it, "id", id);
            SetString(it, "prompt", prompt);
        }

        // ---------------------------------------------------------------- animations / catalogue

        static AnimatorController EnsureController()
        {
            AnimationClip idle = FindClip("idle");
            AnimationClip walk = FindClip("walk");
            AnimationClip interact = FindClip("interact", "reach", "pick", "grab", "button");

            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (ctrl == null)
            {
                ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
                ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                ctrl.AddParameter("Interact", AnimatorControllerParameterType.Trigger);

                var sm = ctrl.layers[0].stateMachine;
                var loco = ctrl.CreateBlendTreeInController("Locomotion", out var tree, 0);
                tree.blendParameter = "Speed";
                tree.useAutomaticThresholds = false;
                tree.AddChild(idle, 0f);
                tree.AddChild(walk, 1f);

                var reach = sm.AddState("Interact");
                var toReach = loco.AddTransition(reach);
                toReach.AddCondition(AnimatorConditionMode.If, 0f, "Interact");
                toReach.hasExitTime = false;
                toReach.duration = 0.15f;
                var back = reach.AddTransition(loco);
                back.hasExitTime = true;
                back.exitTime = 0.85f;
                back.duration = 0.2f;
            }

            // Mise a jour des clips si de nouvelles animations Mixamo ont ete ajoutees
            foreach (var state in ctrl.layers[0].stateMachine.states.Select(s => s.state))
            {
                if (state.motion is BlendTree bt && state.name == "Locomotion")
                {
                    var children = bt.children;
                    if (children.Length >= 2)
                    {
                        if (idle != null) children[0].motion = idle;
                        if (walk != null) children[1].motion = walk;
                        bt.children = children;
                    }
                }
                else if (state.name == "Interact" && interact != null)
                {
                    state.motion = interact;
                }
            }

            EditorUtility.SetDirty(ctrl);
            Debug.Log($"[M1] Animator : idle={Name(idle)} walk={Name(walk)} interaction={Name(interact)}");
            return ctrl;
        }

        static string Name(Object o) => o != null ? o.name : "manquant";

        // Cherche dans Mixamo/Animations un FBX dont le nom contient un des mots-cles
        static AnimationClip FindClip(params string[] keywords)
        {
            if (!AssetDatabase.IsValidFolder(ClipsFolder)) return null;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ClipsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (!keywords.Any(file.Contains)) continue;

                var clip = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                if (clip != null) return clip;
            }
            return null;
        }

        static AvatarCatalog EnsureCatalog(RuntimeAnimatorController controller)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AvatarCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AvatarCatalog>();
                catalog.avatars = Enumerable.Range(1, 3).Select(i => new AvatarOption { displayName = "Avatar " + i }).ToArray();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.animatorController = controller;

            // Remplit les emplacements vides avec les modeles de Mixamo/Avatars
            if (AssetDatabase.IsValidFolder(AvatarsFolder))
            {
                var models = AssetDatabase.FindAssets("t:Model", new[] { AvatarsFolder })
                    .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(m => m != null && catalog.avatars.All(a => a.prefab != m))
                    .ToList();

                foreach (var slot in catalog.avatars.Where(a => a.prefab == null))
                {
                    if (models.Count == 0) break;
                    slot.prefab = models[0];
                    slot.displayName = models[0].name;
                    models.RemoveAt(0);
                }
            }

            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        // ---------------------------------------------------------------- utilitaires scene

        static void AddSun()
        {
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        static void AddEventSystem()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, string mat, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            Paint(go, mat, color);
            return go;
        }

        static void Paint(GameObject go, string matName, Color color)
        {
            string path = $"{MatFolder}/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.SetColor("_BaseColor", color);
                mat.color = color;
                AssetDatabase.CreateAsset(mat, path);
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static Canvas NewCanvas(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        // RectTransform ancre en un point (anchor), positionne par rapport a ce point
        static RectTransform MakeRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static TextMeshProUGUI MakeLabel(RectTransform rt, string text, float size)
        {
            var label = new GameObject("Texte", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            var lrt = label.rectTransform;
            lrt.SetParent(rt, false);
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        static Button MakeButton(Transform parent, string name, string text, Vector2 anchor, Vector2 pos, Vector2 size, out TMP_Text label)
        {
            var rt = MakeRect(name, parent, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.2f, 0.22f, 0.27f, 0.9f);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            label = MakeLabel(rt, text, 40);
            return b;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SaveScene(string path)
        {
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[M1] Scene enregistree : {path}");
        }

        static void EnsureFolders()
        {
            foreach (var f in new[] { AnimFolder, DataFolder, MatFolder, PrefabFolder, SceneFolder, AvatarsFolder, ClipsFolder })
                EnsureFolder(f);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
