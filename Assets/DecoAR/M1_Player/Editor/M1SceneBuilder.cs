using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using static DecoAR.Player.EditorTools.M1UIKit;

namespace DecoAR.Player.EditorTools
{
    // Menu DecoAR > M1 : genere le controleur d'animation, le profil du personnage,
    // le prefab M1_PlayerRig et les scenes, sans rien construire a la main.
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
        const string ProfilePath = DataFolder + "/M1_CharacterProfile.asset";
        const string RigPrefabPath = PrefabFolder + "/M1_PlayerRig.prefab";
        const string TestScenePath = SceneFolder + "/M1_Test.unity";
        const string CustomizationScenePath = SceneFolder + "/M1_Personnalisation.unity";
        const string FaceScanScenePath = SceneFolder + "/M1_ScanVisage.unity";
        const string OldSelectionScenePath = SceneFolder + "/M1_AvatarSelection.unity";

        const int IgnoreRaycastLayer = 2;

        // ---------------------------------------------------------------- menus

        [MenuItem("DecoAR/M1/Tout generer")]
        public static void BuildAll()
        {
            BuildTestScene();
            BuildCustomizationScene();
            BuildFaceScanScene();
            Debug.Log("[M1] Generation terminee");
        }

        // Build telephone du seul module M1 : Personnalisation en premier, les autres scenes decochees.
        // La liste d'origine est gardee pour "Restaurer la liste de l'equipe".
        const string SavedBuildListKey = "DecoAR.M1.SavedBuildList";
        const int TestMinAndroidApi = 31; // Android 12 (Samsung A12)

        [MenuItem("DecoAR/M1/Build telephone - preparer le test M1")]
        public static void PrepareM1PhoneBuild()
        {
            var current = EditorBuildSettings.scenes;
            if (!EditorPrefs.HasKey(SavedBuildListKey))
                EditorPrefs.SetString(SavedBuildListKey, string.Join("|", current.Select(s => (s.enabled ? "1" : "0") + s.path)));

            var first = new[] { CustomizationScenePath, TestScenePath, FaceScanScenePath };
            var list = first.Select(p => new EditorBuildSettingsScene(p, true)).ToList();
            list.AddRange(current.Where(s => !first.Contains(s.path)).Select(s => new EditorBuildSettingsScene(s.path, false)));
            EditorBuildSettings.scenes = list.ToArray();

            if ((int)PlayerSettings.Android.minSdkVersion > TestMinAndroidApi)
                PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)TestMinAndroidApi;
            // Le serveur de tete 3D (M1_FaceServer) tourne en HTTP sur le reseau local
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            // Materiaux et profil a jour (materiau des cheveux 3D...)
            EnsureFolders();
            EnsureProfile(EnsureController());

            AssetDatabase.SaveAssets();
            Debug.Log("[M1] Build pret : M1_Personnalisation puis M1_Test, Android minimum API 31. Lance Build And Run.");
        }

        [MenuItem("DecoAR/M1/Build telephone - restaurer la liste de l'equipe")]
        public static void RestoreTeamBuildList()
        {
            if (!EditorPrefs.HasKey(SavedBuildListKey))
            {
                Debug.Log("[M1] Rien a restaurer");
                return;
            }
            EditorBuildSettings.scenes = EditorPrefs.GetString(SavedBuildListKey).Split('|')
                .Where(e => e.Length > 1)
                .Select(e => new EditorBuildSettingsScene(e.Substring(1), e[0] == '1'))
                .ToArray();
            EditorPrefs.DeleteKey(SavedBuildListKey);
            AssetDatabase.SaveAssets();
            Debug.Log("[M1] Liste des scenes de l'equipe restauree");
        }

        [MenuItem("DecoAR/M1/Mettre a jour animations et personnage")]
        public static void UpdateAssets()
        {
            EnsureFolders();
            EnsureProfile(EnsureController());
            AssetDatabase.SaveAssets();
        }

        [MenuItem("DecoAR/M1/Creer la scene de test (boutique vide)")]
        public static void BuildTestScene()
        {
            EnsureFolders();
            EnsureSprites();
            var profile = EnsureProfile(EnsureController());

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddSun(Quaternion.Euler(50f, -30f, 0f), 1.2f, Color.white, true);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Sol";
            floor.transform.localScale = new Vector3(2f, 1f, 2f);
            Paint(floor, "M1_Sol", new Color(0.75f, 0.72f, 0.68f));

            var walls = new GameObject("Murs").transform;
            var wallColor = new Color(0.92f, 0.9f, 0.86f);
            Box("Mur Nord", walls, new Vector3(0, 1.5f, 10), new Vector3(20, 3, 0.3f), "M1_Mur", wallColor);
            Box("Mur Sud", walls, new Vector3(0, 1.5f, -10), new Vector3(20, 3, 0.3f), "M1_Mur", wallColor);
            Box("Mur Est", walls, new Vector3(10, 1.5f, 0), new Vector3(0.3f, 3, 20), "M1_Mur", wallColor);
            Box("Mur Ouest", walls, new Vector3(-10, 1.5f, 0), new Vector3(0.3f, 3, 20), "M1_Mur", wallColor);

            var furniture = new GameObject("Meubles").transform;
            Box("Table", furniture, new Vector3(2, 0.4f, -1), new Vector3(1.6f, 0.8f, 1f), "M1_Meuble", new Color(0.55f, 0.38f, 0.25f));
            Box("Pilier", furniture, new Vector3(-3, 1.5f, -2), new Vector3(0.6f, 3f, 0.6f), "M1_Meuble", new Color(0.55f, 0.38f, 0.25f));

            var shelf = Box("Etagere (test)", null, new Vector3(-5, 1f, 8.5f), new Vector3(3f, 2f, 0.6f), "M1_Etagere", new Color(0.35f, 0.5f, 0.7f));
            AddInteractable(shelf, "etagere", "Voir l'étagère");

            var counter = Box("Comptoir caisse (test)", null, new Vector3(5, 0.55f, 8.5f), new Vector3(2.5f, 1.1f, 0.8f), "M1_Comptoir", new Color(0.7f, 0.45f, 0.35f));
            AddInteractable(counter, "caisse", "Passer à la caisse");

            var rig = BuildPlayerRig(profile);
            rig.transform.position = new Vector3(0, 0, -6);
            PrefabUtility.SaveAsPrefabAssetAndConnect(rig, RigPrefabPath, InteractionMode.AutomatedAction);

            AddEventSystem();
            SaveScene(TestScenePath);
        }

        [MenuItem("DecoAR/M1/Creer la scene de personnalisation")]
        public static void BuildCustomizationScene()
        {
            EnsureFolders();
            EnsureSprites();
            var profile = EnsureProfile(EnsureController());

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.78f, 0.74f, 0.70f);
            AddSun(Quaternion.Euler(40f, 150f, 0f), 1.15f, new Color(1f, 0.95f, 0.88f), true);
            AddSun(Quaternion.Euler(15f, -150f, 0f), 0.4f, new Color(0.85f, 0.9f, 1f), false);

            BuildShowroom();
            var previewRoot = new GameObject("PreviewRoot").transform;
            previewRoot.position = new Vector3(0f, 0.08f, 0f);

            // Camera : plan large (vetements) et gros plan (visage)
            var bodyView = new GameObject("Vue Corps").transform;
            bodyView.SetPositionAndRotation(new Vector3(-0.95f, 1.0f, 4.4f), Quaternion.Euler(5f, 180f, 0f));
            var faceView = new GameObject("Vue Visage").transform;
            faceView.SetPositionAndRotation(new Vector3(-0.32f, 1.6f, 1.25f), Quaternion.Euler(2f, 180f, 0f));
            // Portrait : le personnage occupe le haut de l'ecran, centre
            var bodyViewPortrait = new GameObject("Vue Corps (portrait)").transform;
            bodyViewPortrait.SetPositionAndRotation(new Vector3(0f, 0.95f, 3.5f), Quaternion.Euler(3f, 180f, 0f));
            var faceViewPortrait = new GameObject("Vue Visage (portrait)").transform;
            faceViewPortrait.SetPositionAndRotation(new Vector3(0f, 1.6f, 1.0f), Quaternion.Euler(2f, 180f, 0f));

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Cream;
            cam.fieldOfView = 35f;
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.SetPositionAndRotation(bodyView.position, bodyView.rotation);

            var canvas = MakeCanvas("UI", null);
            var ct = canvas.transform;

            // Zone de rotation (moitie gauche, derriere le reste)
            var drag = Node("ZoneRotation", ct);
            drag.anchorMin = Vector2.zero;
            drag.anchorMax = new Vector2(0.55f, 1f);
            drag.offsetMin = drag.offsetMax = Vector2.zero;
            Fill(drag, new Color(0, 0, 0, 0));
            Set(drag.gameObject.AddComponent<PreviewDragArea>(), "target", previewRoot);

            LabelOn(Place(Node("Marque", ct), new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -44), new Vector2(600, 70)),
                "DecoAR", 56, Accent, TextAlignmentOptions.Left, true);
            LabelOn(Place(Node("Slogan", ct), new Vector2(0, 1), new Vector2(0, 1), new Vector2(66, -112), new Vector2(700, 40)),
                "Ta boutique déco, à ton image", 28, Muted, TextAlignmentOptions.Left);

            var tip = Place(Node("Astuce", ct), new Vector2(0, 0), new Vector2(0, 0), new Vector2(64, 52), new Vector2(600, 64));
            Fill(tip, Alpha(Card, 0.85f), Rounded, 2.5f).raycastTarget = false;
            ChildLabel(tip, "Fais glisser pour faire tourner ton personnage", 24, Muted);

            // Fond sous le panneau en portrait (la camera 3D n'occupe alors que le haut de l'ecran)
            var portraitBg = Place(Node("FondPortrait", ct), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            Fill(portraitBg, Cream).raycastTarget = false;

            var ui = BuildCustomizationPanel(ct);
            var scan = BuildScanOverlay(ct, profile);

            var portraitPanelTop = 0.56f;
            var card = (RectTransform)scan.transform.Find("Fenetre");
            var responsive = canvas.gameObject.AddComponent<ResponsiveLayout>();
            responsive.scaler = canvas.GetComponent<CanvasScaler>();
            responsive.worldCamera = cam;
            responsive.portraitViewport = new Rect(0f, portraitPanelTop, 1f, 1f - portraitPanelTop);
            responsive.landscapeOnly = new[] { tip.gameObject };
            responsive.portraitOnly = new[] { portraitBg.gameObject };
            responsive.rects = new[]
            {
                Responsive(portraitBg, Stretched(new Vector2(0, 0), new Vector2(1, portraitPanelTop), 0, 0, 0, 0)),
                Responsive(drag, Stretched(new Vector2(0, portraitPanelTop), Vector2.one, 0, 0, 0, 0)),
                Responsive(ui.panel, Stretched(Vector2.zero, new Vector2(1, portraitPanelTop), 24, 24, 12, 24)),
                Responsive(ui.shadow, Stretched(Vector2.zero, new Vector2(1, portraitPanelTop), 30, 18, 20, 16)),
                Responsive(card, Centered(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940, 1780))),
                Responsive((RectTransform)card.Find("Cadre"), Centered(new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(720, 720))),
                Responsive((RectTransform)card.Find("Infos"), Centered(new Vector2(0.5f, 1), new Vector2(0, -880), new Vector2(800, 860))),
            };
            EditorUtility.SetDirty(responsive);

            var customizer = canvas.gameObject.AddComponent<CharacterCustomizer>();
            Set(customizer, "profile", profile);
            Set(customizer, "previewRoot", previewRoot);
            Set(customizer, "previewCamera", cam);
            Set(customizer, "bodyView", bodyView);
            Set(customizer, "faceView", faceView);
            Set(customizer, "bodyViewPortrait", bodyViewPortrait);
            Set(customizer, "faceViewPortrait", faceViewPortrait);
            SetArray(customizer, "tabButtons", ui.tabs);
            Set(customizer, "sectionTitle", ui.sectionTitle);
            Set(customizer, "sectionHint", ui.sectionHint);
            Set(customizer, "swatchGrid", ui.grid);
            Set(customizer, "swatchTemplate", ui.swatch);
            Set(customizer, "scanButton", ui.scan);
            Set(customizer, "randomButton", ui.random);
            Set(customizer, "resetButton", ui.reset);
            Set(customizer, "confirmButton", ui.confirm);
            Set(customizer, "scanner", scan);
            SetString(customizer, "shopSceneName", Path.GetFileNameWithoutExtension(TestScenePath));

            scan.gameObject.SetActive(false);

            AddEventSystem();
            SaveScene(CustomizationScenePath);
            RemoveFromBuild(OldSelectionScenePath);
        }

        // ---------------------------------------------------------------- scan 3D du visage (AR)

        const string FaceMaskMaterialPath = DataFolder + "/M1_FaceMask.mat";
        const string FaceTrackingMaterialPath = DataFolder + "/M1_FaceTracking.mat";
        const string FaceTrackedPrefabPath = PrefabFolder + "/M1_VisageSuivi.prefab";

        [MenuItem("DecoAR/M1/Creer la scene de scan 3D du visage")]
        public static void BuildFaceScanScene()
        {
            EnsureFolders();
            EnsureSprites();
            var profile = EnsureProfile(EnsureController());
            var facePrefab = EnsureTrackedFacePrefab();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // AR : session + XR Origin avec la camera frontale. Inactif tant que l'explication n'est pas validee.
            var arRoot = new GameObject("AR");
            var session = new GameObject("AR Session");
            session.transform.SetParent(arRoot.transform, false);
            session.AddComponent<ARSession>();
            session.AddComponent<ARInputManager>();

            var originGo = new GameObject("XR Origin");
            originGo.transform.SetParent(arRoot.transform, false);
            var origin = originGo.AddComponent<XROrigin>();
            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(originGo.transform, false);

            var camGo = new GameObject("AR Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(offset.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 20f;
            camGo.AddComponent<ARCameraManager>().requestedFacingDirection = CameraFacingDirection.User;
            camGo.AddComponent<ARCameraBackground>();
            var pose = camGo.AddComponent<TrackedPoseDriver>();
            pose.positionInput = new InputActionProperty(new InputAction("Position", binding: "<HandheldARInputDevice>/devicePosition"));
            pose.rotationInput = new InputActionProperty(new InputAction("Rotation", binding: "<HandheldARInputDevice>/deviceRotation"));
            origin.Camera = cam;
            origin.CameraFloorOffsetObject = offset;

            var faces = originGo.AddComponent<ARFaceManager>();
            faces.facePrefab = facePrefab;
            faces.requestedMaximumFaceCount = 1;
            arRoot.SetActive(false);

            // UI
            var canvas = MakeCanvas("UI", null);
            var ct = canvas.transform;
            var responsive = canvas.gameObject.AddComponent<ResponsiveLayout>();
            responsive.scaler = canvas.GetComponent<CanvasScaler>();

            var top = Node("Titre", ct);
            top.anchorMin = new Vector2(0, 1);
            top.anchorMax = Vector2.one;
            top.pivot = new Vector2(0.5f, 1);
            top.sizeDelta = new Vector2(-64, 100);
            top.anchoredPosition = new Vector2(0, -32);
            Fill(top, Alpha(Night, 0.6f), Rounded, 1.6f).raycastTarget = false;
            ChildLabel(top, "Scan 3D du visage", 40, Color.white, TextAlignmentOptions.Center, true);

            var card = Node("Panneau", ct);
            card.anchorMin = Vector2.zero;
            card.anchorMax = new Vector2(1, 0);
            card.pivot = new Vector2(0.5f, 0);
            card.sizeDelta = new Vector2(-64, 300);
            card.anchoredPosition = new Vector2(0, 32);
            Fill(card, Alpha(Card, 0.96f), Rounded);
            Column(card, 40, 18);
            var status = LabelOn(Node("Statut", card), "", 30, Ink, TextAlignmentOptions.Left);
            Size(status, 84);
            var bar = Node("Progression", card);
            Fill(bar, Linen, Rounded, 4f);
            Size(bar, 22);
            var fill = Fill(Stretch(Node("Remplissage", bar)), Accent, Rounded);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            var cancel = MakeButton(card, "Annuler", "Annuler", Linen, Ink, 28, out _);
            Size(cancel, 92);

            // Explication avant d'ouvrir la camera (cahier des charges 9.2)
            var intro = Stretch(Node("Intro", ct));
            Fill(intro, Alpha(Night, 0.94f));
            var introCol = Column(intro, 90, 30);
            introCol.childAlignment = TextAnchor.MiddleCenter;
            Size(LabelOn(Node("Titre", intro), "Ton visage en 3D", 52, Color.white, TextAlignmentOptions.Center, true), 70);
            Size(LabelOn(Node("Explication", intro),
                "Le suivi du visage de ton téléphone mesure la forme 3D de ton visage, et la caméra frontale sa couleur. " +
                "Tout reste sur ton téléphone.", 30, Alpha(Color.white, 0.85f)), 170);
            Size(LabelOn(Node("Conseils", intro),
                "Téléphone à hauteur des yeux (pas en dessous du menton), face à une lumière douce comme une fenêtre. " +
                "Retire lunettes et casquette, bouche fermée, cheveux bien visibles.",
                24, Alpha(Color.white, 0.6f)), 110);
            // Adresse du serveur de tete 3D (PC sur le meme Wi-Fi), retenue sur le telephone
            Size(LabelOn(Node("ServeurTitre", intro), "Serveur tête 3D (optionnel, ton PC) :", 24, Alpha(Color.white, 0.75f)), 36);
            var server = MakeInputField(intro, "Vide = visage calculé sur le téléphone");
            Size(server, 84);
            var start = MakeButton(intro, "Activer", "Activer la caméra", Accent, Color.white, 32, out _);
            Size(start, 110);

            // Telephone sans suivi du visage : repli sur le scan photo
            var unsupported = Stretch(Node("NonCompatible", ct));
            Fill(unsupported, Alpha(Night, 0.94f));
            var unCol = Column(unsupported, 90, 30);
            unCol.childAlignment = TextAnchor.MiddleCenter;
            Size(LabelOn(Node("Message", unsupported),
                "Ton téléphone ne gère pas le suivi 3D du visage (ARCore / ARKit).\nTu peux utiliser le scan photo à la place.",
                32, Color.white), 200);
            var photo = MakeButton(unsupported, "ScanPhoto", "Utiliser le scan photo", Sage, Color.white, 30, out _);
            Size(photo, 110);
            var back = MakeButton(unsupported, "Retour", "Retour", Linen, Ink, 28, out _);
            Size(back, 96);

            var scan = canvas.gameObject.AddComponent<FaceScan3D>();
            Set(scan, "profile", profile);
            Set(scan, "arRoot", arRoot);
            Set(scan, "faceManager", faces);
            Set(scan, "arCamera", cam);
            Set(scan, "canvas", canvas);
            Set(scan, "introGroup", intro.gameObject);
            Set(scan, "startButton", start);
            Set(scan, "status", status);
            Set(scan, "progressFill", fill);
            Set(scan, "cancelButton", cancel);
            Set(scan, "unsupportedGroup", unsupported.gameObject);
            Set(scan, "photoScanButton", photo);
            Set(scan, "backButton", back);
            Set(scan, "serverField", server);
            SetString(scan, "returnScene", Path.GetFileNameWithoutExtension(CustomizationScenePath));

            AddEventSystem();
            SaveScene(FaceScanScenePath);
        }

        // Champ texte TMP aux couleurs du theme
        static TMP_InputField MakeInputField(Transform parent, string placeholder)
        {
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.name = "ChampServeur";
            go.transform.SetParent(parent, false);
            var field = go.GetComponent<TMP_InputField>();
            var bg = go.GetComponent<Image>();
            bg.sprite = Rounded;
            bg.type = Image.Type.Sliced;
            bg.color = Card;
            field.textComponent.fontSize = 28;
            field.textComponent.color = Ink;
            field.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            if (field.placeholder is TMP_Text ph) { ph.fontSize = 28; ph.text = placeholder; }
            field.contentType = TMP_InputField.ContentType.Standard;
            field.text = "";
            return field;
        }

        // Visage suivi en direct : maillage AR semi-transparent, pour montrer que le scan est en 3D
        static GameObject EnsureTrackedFacePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(FaceTrackedPrefabPath);
            if (existing != null) return existing;

            var go = new GameObject("M1_VisageSuivi");
            go.AddComponent<ARFace>();
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>().sharedMaterial =
                EnsureFaceMaterial(FaceTrackingMaterialPath, "Universal Render Pipeline/Unlit", Alpha(Accent, 0.35f));
            go.AddComponent<ARFaceMeshVisualizer>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, FaceTrackedPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // Visage 3D : shader DecoAR/FaceMask (passe devant le visage du personnage, derriere les cheveux)
        static Material EnsureFaceMaskMaterial()
        {
            var shader = Shader.Find("DecoAR/FaceMask");
            if (shader == null)
            {
                Debug.LogError("[M1] Shader DecoAR/FaceMask introuvable");
                return null;
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(FaceMaskMaterialPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, FaceMaskMaterialPath);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
                EditorUtility.SetDirty(mat);
            }
            mat.renderQueue = -1; // celui du shader
            return mat;
        }

        // Materiau URP transparent (le contour du visage se fond dans la peau du personnage)
        static Material EnsureFaceMaterial(string path, string shaderName, Color color)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            mat = new Material(Shader.Find(shaderName));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.25f);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // ---------------------------------------------------------------- personnalisation

        struct PanelRefs
        {
            public RectTransform panel, shadow;
            public Button[] tabs;
            public TMP_Text sectionTitle, sectionHint;
            public Transform grid;
            public Button swatch, scan, random, reset, confirm;
        }

        static PanelRefs BuildCustomizationPanel(Transform ct)
        {
            var refs = new PanelRefs();

            var shadow = Node("Ombre", ct);
            shadow.anchorMin = new Vector2(1, 0);
            shadow.anchorMax = new Vector2(1, 1);
            shadow.pivot = new Vector2(1, 0.5f);
            shadow.sizeDelta = new Vector2(820, -72);
            shadow.anchoredPosition = new Vector2(-30, -8);
            Fill(shadow, new Color(0.23f, 0.18f, 0.16f, 0.08f), Rounded).raycastTarget = false;

            var panel = Node("Panneau", ct);
            panel.anchorMin = new Vector2(1, 0);
            panel.anchorMax = new Vector2(1, 1);
            panel.pivot = new Vector2(1, 0.5f);
            panel.sizeDelta = new Vector2(820, -72);
            panel.anchoredPosition = new Vector2(-36, 0);
            Fill(panel, Card, Rounded);
            Column(panel, 52, 18);
            refs.panel = panel;
            refs.shadow = shadow;

            Size(LabelOn(Node("Titre", panel), "Crée ton personnage", 50, Ink, TextAlignmentOptions.Left, true), 64);
            Size(LabelOn(Node("Sous-titre", panel), "Choisis ton style avant d'entrer dans la boutique.", 26, Muted, TextAlignmentOptions.Left), 40);
            Size(Node("Espace", panel), 4);

            // Onglets
            var tabsBg = Node("Onglets", panel);
            Fill(tabsBg, Linen, Rounded, 1.4f);
            var tabRow = Row(tabsBg, 8, 8);
            tabRow.childForceExpandWidth = true;
            Size(tabsBg, 84);
            string[] names = { "Peau", "Cheveux", "T-shirt", "Pantalon", "Chaussures" };
            refs.tabs = names.Select(n => MakeButton(tabsBg, "Onglet " + n, n, new Color(1, 1, 1, 0), Ink, 25, out _, 1.8f)).ToArray();

            refs.sectionTitle = LabelOn(Node("Section", panel), "Couleur de peau", 36, Ink, TextAlignmentOptions.Left, true);
            Size(refs.sectionTitle, 50);
            refs.sectionHint = LabelOn(Node("Aide", panel), "", 22, Muted, TextAlignmentOptions.TopLeft);
            Size(refs.sectionHint, 58);

            // Pastilles de couleur
            var grid = Node("Couleurs", panel);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(96, 96);
            gl.spacing = new Vector2(24, 24);
            gl.padding = new RectOffset(4, 4, 12, 4);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 6;
            Size(grid, 240, -1, -1, 1);
            refs.grid = grid;

            var swatch = Node("Pastille", grid);
            var swImg = Fill(swatch, Color.white, Circle);
            refs.swatch = swatch.gameObject.AddComponent<Button>();
            refs.swatch.targetGraphic = swImg;
            Fill(Stretch(Node("Contour", swatch)), new Color(0, 0, 0, 0.14f), Ring).raycastTarget = false;
            var sel = Stretch(Node("Selection", swatch), -11, -11, -11, -11);
            Fill(sel, Accent, Ring).raycastTarget = false;
            sel.gameObject.SetActive(false);

            // Carte scan facial
            var card = Node("CarteScan", panel);
            Fill(card, SageLight, Rounded, 1.4f);
            var cardRow = Row(card, 22, 20);
            cardRow.childForceExpandHeight = false;
            cardRow.childAlignment = TextAnchor.MiddleLeft;
            Size(card, 150);

            var icon = Node("Icone", card);
            Fill(icon, Sage, Circle);
            Size(icon, 96, 96);
            Fill(Stretch(Node("Objectif", icon), 26, 26, 26, 26), Color.white, Ring).raycastTarget = false;

            var texts = Node("Textes", card);
            Column(texts, 0, 4);
            Size(texts, -1, -1, 1);
            Size(LabelOn(Node("Titre", texts), "Scanner mon visage en 3D", 30, Ink, TextAlignmentOptions.Left, true), 40);
            Size(LabelOn(Node("Description", texts), "La caméra mesure ton visage en 3D et le pose sur ton personnage.", 22, Muted, TextAlignmentOptions.TopLeft), 58);

            refs.scan = MakeButton(card, "BoutonScan", "Scanner", Sage, Color.white, 28, out _, 1.6f);
            Size(refs.scan, 84, 190);

            // Actions
            var actions = Node("Actions", panel);
            Row(actions, 0, 16);
            Size(actions, 100);
            refs.random = MakeButton(actions, "Aleatoire", "Aléatoire", Linen, Ink, 27, out _);
            Size(refs.random, -1, -1, 1);
            refs.reset = MakeButton(actions, "Reinitialiser", "Réinitialiser", Linen, Ink, 27, out _);
            Size(refs.reset, -1, -1, 1);
            refs.confirm = MakeButton(actions, "Entrer", "Entrer dans la boutique", Accent, Color.white, 29, out _);
            Size(refs.confirm, -1, -1, 2.2f);

            return refs;
        }

        static FaceScanner BuildScanOverlay(Transform ct, CharacterProfile profile)
        {
            var overlay = Stretch(Node("ScanVisage", ct));
            Fill(overlay, Alpha(Night, 0.9f));

            var card = Place(Node("Fenetre", overlay), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1560, 860));
            Fill(card, Card, Rounded);

            // Cadre camera
            var frame = Place(Node("Cadre", card), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(60, 0), new Vector2(720, 720));
            Fill(frame, Night, Rounded);
            frame.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            var video = Place(Node("Video", frame), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 720))
                .gameObject.AddComponent<RawImage>();
            video.raycastTarget = false;
            video.enabled = false;

            var ovalSize = new Vector2(720 * FaceScanner.OvalRx * 2f, 720 * FaceScanner.OvalRy * 2f);
            var oval = Fill(Place(Node("Ovale", frame), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, ovalSize), Color.white, Ring);
            oval.raycastTarget = false;

            var intro = Stretch(Node("Intro", frame));
            Fill(intro, Alpha(Night, 0.94f));
            var introCol = Column(intro, 70, 26);
            introCol.childAlignment = TextAnchor.MiddleCenter;
            Size(LabelOn(Node("Titre", intro), "Ta caméra, ton style", 40, Color.white, TextAlignmentOptions.Center, true), 56);
            Size(LabelOn(Node("Explication", intro),
                "DecoAR photographie ton visage pour le placer sur ton personnage, avec ta couleur de peau et de cheveux. La photo reste uniquement sur ton téléphone.",
                26, Alpha(Color.white, 0.85f)), 150);
            var enable = MakeButton(intro, "Activer", "Activer la caméra", Accent, Color.white, 30, out _);
            Size(enable, 96);

            // Colonne d'infos
            var info = Place(Node("Infos", card), new Vector2(0, 1), new Vector2(0, 1), new Vector2(840, -70), new Vector2(660, 720));
            Column(info, 0, 20);
            Size(LabelOn(Node("Titre", info), "Scan du visage", 48, Ink, TextAlignmentOptions.Left, true), 60);
            var status = LabelOn(Node("Statut", info), "", 30, Ink, TextAlignmentOptions.TopLeft);
            Size(status, 84);

            var bar = Node("Progression", info);
            Fill(bar, Linen, Rounded, 4f);
            Size(bar, 22);
            var fill = Fill(Stretch(Node("Remplissage", bar)), Accent, Rounded);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;

            Size(LabelOn(Node("Conseils", info),
                "• Mets-toi face à une lumière\n• Retire lunettes et casquette\n• Garde le téléphone à hauteur des yeux",
                24, Muted, TextAlignmentOptions.TopLeft), 120);

            var result = Node("Resultat", info);
            Column(result, 0, 18);
            Size(result, 250);

            var swRow = Node("Couleurs", result);
            var swLayout = Row(swRow, 0, 24);
            swLayout.childControlWidth = swLayout.childControlHeight = false;
            Size(swRow, 136);
            var skin = ResultSwatch(swRow, "Peau", out _);
            var hair = ResultSwatch(swRow, "Cheveux", out var hairLabel);
            var facePreview = FacePreview(swRow);

            var btnRow = Node("Boutons", result);
            Row(btnRow, 0, 16);
            Size(btnRow, 96);
            var retry = MakeButton(btnRow, "Recommencer", "Recommencer", Linen, Ink, 28, out _);
            Size(retry, -1, -1, 1);
            var apply = MakeButton(btnRow, "Appliquer", "Appliquer", Accent, Color.white, 28, out _);
            Size(apply, -1, -1, 1);
            result.gameObject.SetActive(false);

            var close = MakeButton(card, "Fermer", "×", Linen, Ink, 46, out _);
            close.image.sprite = Circle;
            close.image.type = Image.Type.Simple;
            Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -28), new Vector2(80, 80));

            var scanner = overlay.gameObject.AddComponent<FaceScanner>();
            Set(scanner, "profile", profile);
            Set(scanner, "videoFrame", frame);
            Set(scanner, "video", video);
            Set(scanner, "oval", oval);
            Set(scanner, "introGroup", intro.gameObject);
            Set(scanner, "enableCameraButton", enable);
            Set(scanner, "status", status);
            Set(scanner, "progressFill", fill);
            Set(scanner, "resultGroup", result.gameObject);
            Set(scanner, "skinSwatch", skin);
            Set(scanner, "hairSwatch", hair);
            Set(scanner, "hairLabel", hairLabel);
            Set(scanner, "facePreview", facePreview);
            Set(scanner, "applyButton", apply);
            Set(scanner, "retryButton", retry);
            Set(scanner, "closeButton", close);
            return scanner;
        }

        static Image ResultSwatch(Transform parent, string label, out TMP_Text text)
        {
            var item = Node(label, parent);
            item.sizeDelta = new Vector2(220, 136);
            var dot = Fill(Place(Node("Pastille", item), new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(96, 96)), Color.white, Circle);
            Fill(Stretch(Node("Contour", dot.transform)), new Color(0, 0, 0, 0.14f), Ring);
            text = LabelOn(Place(Node("Nom", item), new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(220, 32)), label, 24, Ink);
            return dot;
        }

        // Apercu de la photo detouree qui sera posee sur le personnage
        static RawImage FacePreview(Transform parent)
        {
            var item = Node("Visage", parent);
            item.sizeDelta = new Vector2(140, 136);
            var photo = Place(Node("Photo", item), new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero,
                new Vector2(96 * FaceScanner.OvalRx / FaceScanner.OvalRy, 96)).gameObject.AddComponent<RawImage>();
            photo.raycastTarget = false;
            LabelOn(Place(Node("Nom", item), new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(140, 32)), "Visage", 24, Ink);
            return photo;
        }

        // Petit salon autour du personnage : tapis, podium en bois, mur, cadre et plante
        static void BuildShowroom()
        {
            var room = new GameObject("Decor").transform;
            Shape(PrimitiveType.Plane, "Sol", room, new Vector3(0, 0, 0), new Vector3(2, 1, 2), "M1_ShowroomSol", Hex("E6DCCF"));
            Shape(PrimitiveType.Cube, "Mur", room, new Vector3(0, 2.5f, -2.5f), new Vector3(10, 5, 0.2f), "M1_ShowroomMur", Hex("F1E9DE"));
            Shape(PrimitiveType.Cube, "Plinthe", room, new Vector3(0, 0.06f, -2.38f), new Vector3(10, 0.12f, 0.06f), "M1_Bois", Wood);
            Shape(PrimitiveType.Cylinder, "Tapis", room, new Vector3(0, 0.005f, 0), new Vector3(2.6f, 0.005f, 2.6f), "M1_Tapis", Sand);
            Shape(PrimitiveType.Cylinder, "Podium", room, new Vector3(0, 0.04f, 0), new Vector3(1.3f, 0.04f, 1.3f), "M1_Bois", Wood);
            Shape(PrimitiveType.Cube, "Cadre", room, new Vector3(1.5f, 1.9f, -2.37f), new Vector3(0.9f, 1.2f, 0.05f), "M1_Bois", Wood);
            Shape(PrimitiveType.Cube, "Tableau", room, new Vector3(1.5f, 1.9f, -2.34f), new Vector3(0.74f, 1.04f, 0.04f), "M1_Sauge", Sage);
            Shape(PrimitiveType.Cylinder, "Pot", room, new Vector3(1.75f, 0.25f, -1.6f), new Vector3(0.38f, 0.25f, 0.38f), "M1_Terracotta", Accent);
            Shape(PrimitiveType.Sphere, "Plante", room, new Vector3(1.75f, 0.9f, -1.6f), new Vector3(0.7f, 0.95f, 0.7f), "M1_Sauge", Sage);
        }

        static void Shape(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, string mat, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            Paint(go, mat, color);
        }

        // ---------------------------------------------------------------- joueur

        static GameObject BuildPlayerRig(CharacterProfile profile)
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

            var mount = new GameObject("AvatarMount").transform;
            mount.SetParent(player.transform, false);

            var controller = player.AddComponent<PlayerController>();
            var interactor = player.AddComponent<PlayerInteractor>();
            var loader = player.AddComponent<PlayerAvatarLoader>();
            Set(loader, "profile", profile);
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
            var canvas = MakeCanvas("HUD", rig.transform);
            var ct = canvas.transform;

            var joyBg = Place(Node("Joystick", ct), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(230, 230), new Vector2(320, 320));
            Fill(joyBg, Alpha(Card, 0.35f), Circle);
            var handle = Place(Node("Handle", joyBg), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 140));
            Fill(handle, Alpha(Card, 0.95f), Circle).raycastTarget = false;
            var joystick = joyBg.gameObject.AddComponent<VirtualJoystick>();
            Set(joystick, "background", joyBg);
            Set(joystick, "handle", handle);
            Set(controller, "joystick", joystick);

            var interact = MakeButton(ct, "BoutonInteraction", "Interagir", Accent, Color.white, 34, out var interactLabel);
            Place((RectTransform)interact.transform, new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-240, 230), new Vector2(380, 130));
            Set(interactor, "interactButton", interact);
            Set(interactor, "interactLabel", interactLabel);

            // Barre du haut
            var bar = Place(Node("BarreHaut", ct), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-32, -28), new Vector2(1060, 112));
            Fill(bar, Alpha(Card, 0.94f), Rounded, 1.6f);
            Row(bar, 10, 8).childForceExpandWidth = true;
            var clear = new Color(1, 1, 1, 0);
            var cart = MakeButton(bar, "Panier", "Panier", clear, Ink, 30, out _, 1.8f);
            var map = MakeButton(bar, "MiniCarte", "Carte", clear, Ink, 30, out _, 1.8f);
            var gyro = MakeButton(bar, "Gyro", "Gyro : OFF", clear, Ink, 28, out var gyroLabel, 1.8f);
            var settings = MakeButton(bar, "Reglages", "Réglages", clear, Ink, 30, out _, 1.8f);

            var badge = Place(Node("Badge", cart.transform), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-18, -16), new Vector2(46, 46));
            Fill(badge, Accent, Circle).raycastTarget = false;
            var badgeText = ChildLabel(badge, "0", 24, Color.white, TextAlignmentOptions.Center, true);

            // Panneau parametres
            var panel = Stretch(Node("PanneauReglages", ct));
            Fill(panel, Alpha(Night, 0.6f));
            var box = Place(Node("Fenetre", panel), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 600));
            Fill(box, Card, Rounded);
            Column(box, 52, 22);
            Size(LabelOn(Node("Titre", box), "Paramètres", 46, Ink, TextAlignmentOptions.Left, true), 70);
            var haptics = MakeButton(box, "Vibrations", "Vibrations : ON", Linen, Ink, 30, out var hapticsLabel);
            Size(haptics, 104);
            var recal = MakeButton(box, "Recalibrer", "Recalibrer le gyroscope", Linen, Ink, 30, out _);
            Size(recal, 104);
            Size(Node("Espace", box), -1, -1, -1, 1);
            var close = MakeButton(box, "Fermer", "Fermer", Accent, Color.white, 30, out _);
            Size(close, 104);

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

            // Portrait : la barre du haut prend toute la largeur
            var responsive = canvas.gameObject.AddComponent<ResponsiveLayout>();
            responsive.scaler = canvas.GetComponent<CanvasScaler>();
            responsive.rects = new[] { Responsive(bar, Stretched(new Vector2(0, 1), Vector2.one, 24, 24, 24, -136)) };

            return rig;
        }

        // ---------------------------------------------------------------- portrait / paysage

        // La disposition actuelle du RectTransform devient la version paysage
        static ResponsiveRect Responsive(RectTransform rt, RectLayout portrait) =>
            new ResponsiveRect { target = rt, landscape = RectLayout.Of(rt), portrait = portrait };

        static RectLayout Stretched(Vector2 anchorMin, Vector2 anchorMax, float left, float right, float top, float bottom) => new RectLayout
        {
            anchorMin = anchorMin,
            anchorMax = anchorMax,
            pivot = new Vector2(0.5f, 0.5f),
            anchoredPosition = new Vector2((left - right) / 2f, (bottom - top) / 2f),
            sizeDelta = new Vector2(-left - right, -top - bottom),
        };

        static RectLayout Centered(Vector2 anchor, Vector2 pos, Vector2 size) => new RectLayout
        {
            anchorMin = anchor,
            anchorMax = anchor,
            pivot = anchor,
            anchoredPosition = pos,
            sizeDelta = size,
        };

        static void AddInteractable(GameObject go, string id, string prompt)
        {
            var it = go.AddComponent<Interactable>();
            SetString(it, "id", id);
            SetString(it, "prompt", prompt);
        }

        // ---------------------------------------------------------------- animations / personnage

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

        static CharacterProfile EnsureProfile(RuntimeAnimatorController controller)
        {
            var profile = AssetDatabase.LoadAssetAtPath<CharacterProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CharacterProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            profile.animatorController = controller;
            if (profile.faceDecalMaterial == null) profile.faceDecalMaterial = EnsureFaceDecalMaterial();
            profile.faceMaskMaterial = EnsureFaceMaskMaterial();
            profile.hairMaterial = EnsureHairMaterial();
            EnsureDecalFeature();

            // Le personnage : Remy de preference, sinon le premier modele de Mixamo/Avatars
            if (profile.prefab == null && AssetDatabase.IsValidFolder(AvatarsFolder))
            {
                var models = AssetDatabase.FindAssets("t:Model", new[] { AvatarsFolder })
                    .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(m => m != null)
                    .ToList();
                profile.prefab = models.FirstOrDefault(m => m.name == "Remy") ?? models.FirstOrDefault();
            }

            EditorUtility.SetDirty(profile);
            Debug.Log($"[M1] Personnage : {Name(profile.prefab)}");
            return profile;
        }

        // Cheveux 3D de la tete scannee : URP Lit a decoupe alpha (bord en meches), visible des deux cotes.
        // Un vrai asset, pour que le build garde la variante "alpha test" du shader.
        const string HairMaterialPath = DataFolder + "/M1_HairFLAME.mat";

        static Material EnsureHairMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(HairMaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, HairMaterialPath);
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetFloat("_Cull", (float)CullMode.Off);
            // Mat : des cheveux brillants refletent le ciel bleu de l'environnement
            mat.SetFloat("_Smoothness", 0.08f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_EnvironmentReflections", 0f);
            mat.SetFloat("_SpecularHighlights", 0f);
            mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------------------------------------------------------------- visage scanne (decal URP)

        const string FaceDecalMaterialPath = DataFolder + "/M1_FaceDecal.mat";

        static Material EnsureFaceDecalMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(FaceDecalMaterialPath);
            if (mat != null) return mat;

            var shader = Shader.Find("Shader Graphs/Decal");
            if (shader == null)
            {
                Debug.LogError("[M1] Shader URP 'Shader Graphs/Decal' introuvable : le visage scanne ne sera pas projete");
                return null;
            }
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, FaceDecalMaterialPath);
            return mat;
        }

        // Les decals URP ne s'affichent que si le renderer a la feature "Decal" (ajoutee une seule fois)
        static void EnsureDecalFeature()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
            {
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data == null || data.rendererFeatures.Any(f => f is DecalRendererFeature)) continue;

                var feature = ScriptableObject.CreateInstance<DecalRendererFeature>();
                feature.name = "Decal";
                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

                var so = new SerializedObject(data);
                var features = so.FindProperty("m_RendererFeatures");
                var map = so.FindProperty("m_RendererFeatureMap");
                features.arraySize++;
                features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
                Debug.Log($"[M1] Feature Decal ajoutee a {data.name} (projection du visage scanne)");
            }
        }

        // ---------------------------------------------------------------- utilitaires scene

        static void AddSun(Quaternion rotation, float intensity, Color color, bool shadows)
        {
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            sun.intensity = intensity;
            sun.color = color;
            sun.transform.rotation = rotation;
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

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
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

        static void RemoveFromBuild(string path)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToArray();
            if (scenes.Length != EditorBuildSettings.scenes.Length) EditorBuildSettings.scenes = scenes;
        }

        static void EnsureFolders()
        {
            foreach (var f in new[] { AnimFolder, DataFolder, MatFolder, PrefabFolder, SceneFolder, AvatarsFolder, ClipsFolder, UIFolder })
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
