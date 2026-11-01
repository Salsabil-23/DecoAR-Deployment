using System.Linq;
using DecoAR.M3;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace DecoAR.AR.EditorTools
{
    // Menu DecoAR > M4 : (re)cree dans M4_AR les objets DecoAR (placement, lumiere, photo, interface)
    // et rebranche l'evenement "Object Spawn Triggered" du template XRI sur M4PlacementGate.
    // Peut etre relance sans risque : les objets existants sont reutilises.
    public static class M4SceneSetup
    {
        const string Root = "Assets/DecoAR/M4_AR";
        const string ScenePath = Root + "/Scenes/M4_AR.unity";
        const string HolderFloorPath = Root + "/Prefabs/M4_Holder_Floor.prefab";
        const string HolderWallPath = Root + "/Prefabs/M4_Holder_Wall.prefab";
        const string ShadowMatPath = Root + "/Materials/ShadowCatcher_Mat.mat";
        const string CatalogPath = "Assets/DecoAR/M3_ProductCatalog/Data/M3_ProductCatalog.asset";
        const string TemplatePlanePath = "Assets/MobileARTemplateAssets/Prefabs/ARFeatheredOcclusionPlane.prefab";
        const string LogoPlanePath = Root + "/Prefabs/M4_LogoPlane.prefab";
        const string LogoPlaneMatPath = Root + "/Materials/M4_LogoPlane.mat";
        const string LogoPath = "Assets/DecoAR/Core/Icons/DecoAR_Icon.png";

        static readonly Color Dark = new Color(0.17f, 0.14f, 0.11f, 0.85f);
        static readonly Color Cream = new Color(0.98f, 0.95f, 0.89f);
        static readonly Color Brass = new Color(0.80f, 0.63f, 0.30f);
        static readonly Color Sage = new Color(0.45f, 0.58f, 0.43f);
        static readonly Color Ink = new Color(0.20f, 0.17f, 0.14f);

        [MenuItem("DecoAR/M4/Reparer et completer la scene AR")]
        public static void Setup()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // --- placement
            var gate = FindOrCreate("M4_PlacementGate").GetOrAdd<M4PlacementGate>();
            Set(gate, "holderFloorPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(HolderFloorPath));
            Set(gate, "holderWallPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(HolderWallPath));
            Set(gate, "shadowCatcherMaterial", AssetDatabase.LoadAssetAtPath<Material>(ShadowMatPath));
            var catalog = AssetDatabase.LoadAssetAtPath<M3ProductCatalog>(CatalogPath);
            Set(gate, "catalog", catalog);
            if (catalog != null && catalog.Get("TAB-01") is ProductOption test)
            {
                Set(gate, "productPrefab", test.prefab);
                var so = new SerializedObject(gate);
                so.FindProperty("placementType").enumValueIndex = (int)test.placement;
                so.FindProperty("sizeCm").vector3Value = test.sizeCm;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var actions = gate.gameObject.GetOrAdd<M4ShopActions>();
            Set(actions, "gate", gate);

            // --- light estimation (F4.4)
            var camManager = Object.FindAnyObjectByType<ARCameraManager>();
            var sun = Object.FindObjectsByType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
            if (camManager != null && sun != null)
            {
                var matcher = FindOrCreate("M4_LightMatcher").GetOrAdd<M4LightMatcher>();
                Set(matcher, "cameraManager", camManager);
                Set(matcher, "sun", sun);
                sun.shadows = LightShadows.Soft;   // ombre portee sur le shadow catcher (F4.3)
            }
            else Debug.LogWarning("[M4] ARCameraManager ou lumiere directionnelle introuvable : M4_LightMatcher non configure");

            // --- surfaces detectees : logo DecoAR a la place des points du template (F4.1)
            var planeManager = Object.FindAnyObjectByType<ARPlaneManager>();
            var logoPlane = BuildLogoPlanePrefab();
            if (planeManager != null && logoPlane != null) Set(planeManager, "m_PlanePrefab", logoPlane);
            else Debug.LogWarning("[M4] ARPlaneManager ou prefab de plan introuvable : visuel des surfaces inchange");

            // --- interface
            var old = GameObject.Find("M4_Canvas");
            if (old != null) Object.DestroyImmediate(old);
            var canvas = NewCanvas("M4_Canvas");
            var ct = canvas.transform;

            var bar = Rect("M4_MessageBar", ct, new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(920, 90));
            bar.gameObject.AddComponent<Image>().color = Dark;
            var message = Text(bar, "M4_Message", "", Vector2.one * 0.5f, Vector2.zero, new Vector2(890, 90), 36, Cream);
            var product = Text(ct, "M4_ProductLabel", "", new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(920, 60), 30, Cream);
            product.fontStyle = FontStyles.Bold;

            // colonne de boutons a gauche (le template XRI occupe le bas et la droite)
            var column = Rect("M4_Buttons", ct, new Vector2(0, 0), new Vector2(170, 380), new Vector2(280, 680));
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var photo = FindOrCreate("M4_Photo").GetOrAdd<M4Photo>();
            Set(photo, "uiCanvas", canvas);
            Set(photo, "gate", gate);

            var backCatalog = Button(column, "M4_BackToCatalog", "‹ Rayon", Cream, Ink);
            UnityEventTools.AddPersistentListener(backCatalog.onClick, actions.BackToCatalog);
            var backShop = Button(column, "M4_BackToShop", "Boutique", Cream, Ink);
            UnityEventTools.AddPersistentListener(backShop.onClick, actions.BackToShop);
            var variant = Button(column, "M4_VariantButton", "Variante", Cream, Ink);
            UnityEventTools.AddPersistentListener(variant.onClick, gate.NextVariant);
            var reset = Button(column, "M4_ResetButton", "Taille réelle", Cream, Ink);
            UnityEventTools.AddPersistentListener(reset.onClick, gate.ResetToRealSize);
            var photoBtn = Button(column, "M4_PhotoButton", "Photo", Brass, Color.white);
            UnityEventTools.AddPersistentListener(photoBtn.onClick, photo.TakePhoto);
            var cart = Button(column, "M4_AddToCart", "Ajouter au panier", Sage, Color.white);
            UnityEventTools.AddPersistentListener(cart.onClick, actions.AddToCart);

            Set(gate, "messageText", message);
            Set(gate, "messageBackground", bar.gameObject);
            Set(gate, "resetButton", reset.gameObject);
            Set(gate, "variantButton", variant.gameObject);
            Set(gate, "productLabel", product);

            // --- evenement du template XRI : un tap sur un plan appelle OnSpawnTriggered(position, normale)
            WireSpawnTrigger(gate);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[M4] Scene AR completee : " + ScenePath);
        }

        // Variante du plan du template (bord adouci, fondu, occlusion conserves) avec le materiau logo en premier
        static GameObject BuildLogoPlanePrefab()
        {
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePlanePath);
            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
            var shader = Shader.Find("DecoAR/M4 Logo Plane");
            if (template == null || logo == null || shader == null)
            {
                Debug.LogError($"[M4] Plan logo impossible : template={template != null} logo={logo != null} shader={shader != null}");
                return null;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(LogoPlaneMatPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, LogoPlaneMatPath);
            }
            mat.shader = shader;
            mat.SetTexture("_LogoTex", logo);
            EditorUtility.SetDirty(mat);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
            var renderer = instance.GetComponent<MeshRenderer>();
            var mats = renderer.sharedMaterials;
            mats[0] = mat;
            renderer.sharedMaterials = mats;
            instance.name = "M4_LogoPlane";
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, LogoPlanePath);
            Object.DestroyImmediate(instance);
            Debug.Log("[M4] Plan logo : " + LogoPlanePath);
            return prefab;
        }

        static void WireSpawnTrigger(M4PlacementGate gate)
        {
            var trigger = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include)
                .FirstOrDefault(m => m != null && m.GetType().Name == "ARInteractorSpawnTrigger");
            if (trigger == null)
            {
                Debug.LogError("[M4] ARInteractorSpawnTrigger introuvable : placement non branche");
                return;
            }

            var so = new SerializedObject(trigger);
            var calls = so.FindProperty("m_ObjectSpawnTriggered.m_PersistentCalls.m_Calls");
            calls.arraySize = 1;
            var call = calls.GetArrayElementAtIndex(0);
            call.FindPropertyRelative("m_Target").objectReferenceValue = gate;
            call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = typeof(M4PlacementGate).FullName + ", Assembly-CSharp";
            call.FindPropertyRelative("m_MethodName").stringValue = nameof(M4PlacementGate.OnSpawnTriggered);
            call.FindPropertyRelative("m_Mode").intValue = (int)PersistentListenerMode.EventDefined;
            call.FindPropertyRelative("m_CallState").intValue = (int)UnityEventCallState.RuntimeOnly;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[M4] Object Spawn Triggered -> M4PlacementGate.OnSpawnTriggered");
        }

        // ---------------------------------------------------------------- utilitaires

        static T GetOrAdd<T>(this GameObject go) where T : Component
            => go.TryGetComponent<T>(out var c) ? c : go.AddComponent<T>();

        static GameObject FindOrCreate(string name)
        {
            var go = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == name);
            return go != null ? go : new GameObject(name);
        }

        static Canvas NewCanvas(string name)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;   // au-dessus de l'interface du template
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static TextMeshProUGUI Text(Transform parent, string name, string text, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize, Color color)
        {
            var label = Rect(name, parent, anchor, pos, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        static Button Button(Transform parent, string name, string text, Color background, Color textColor)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.zero, new Vector2(280, 90));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = background;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = le.minHeight = 90;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var label = Text(rt, "Texte", text, Vector2.one * 0.5f, Vector2.zero, new Vector2(270, 90), 34, textColor);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.sizeDelta = Vector2.zero;
            return b;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[M4] Champ introuvable : {target.GetType().Name}.{field}"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
