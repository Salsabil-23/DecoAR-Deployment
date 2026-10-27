using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using static DecoAR.M5.M5EditorUtil;

namespace DecoAR.M5
{
    /// Aj (Mixamo) devient le vendeur : skin (textures + matériaux URP), animations Idle / Salut / Applaudit / Joie, remplace X Bot.
    public static class M5AjSetup
    {
        public const string AjPath = Root + "/Characters/Aj.fbx";
        const string CharDir = Root + "/Characters";
        const string TexDir = CharDir + "/Aj_Textures";
        const string MatDir = CharDir + "/Aj_Materials";
        const string IdleSrc = "Assets/DecoAR/M1_Player/Mixamo/Animations/Idle.fbx"; // copié depuis M1 (on ne modifie pas l'autre module)
        const string IdlePath = CharDir + "/Idle.fbx";
        const string CtrlPath = Root + "/Animations/AjAnimator.controller";

        public static bool IsAj(GameObject g) => g && FindInstanceOf(AjPath) == g;

        [MenuItem("DecoAR/M5/5. Personnage Aj (skin + animations)")]
        public static void Run()
        {
            if (!File.Exists(AjPath)) { Debug.LogWarning($"[M5] ⚠️ {AjPath} introuvable."); return; }

            // 1. Modèle + skin
            var avatar = ImportCharacter();
            FixMaterials();

            // 2. Animations
            if (!File.Exists(IdlePath) && File.Exists(IdleSrc)) AssetDatabase.CopyAsset(IdleSrc, IdlePath);
            var idle = ImportClip(IdlePath, "Idle", true);
            var clap = ImportClip(CharDir + "/Clapping.fbx", "Applaudit", true);
            var cheer = ImportClip(CharDir + "/Cheering.fbx", "Joie", false);
            var wave = AssetDatabase.FindAssets("t:Model", new[] { CharDir }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Has(Path.GetFileName(p), new[] { "wav" }))
                .SelectMany(p => AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>())
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (!idle) Debug.LogWarning($"[M5] ⚠️ Idle introuvable ({IdleSrc}) : Aj restera sur l'animation de salut au repos.");
            if (!clap || !cheer) Debug.LogWarning("[M5] ⚠️ Clapping.fbx / Cheering.fbx introuvables dans " + CharDir);

            // 3. Animator
            var ctrl = BuildController(idle ? idle : wave, wave, clap, cheer);

            // 4. Scène
            SetupInScene(avatar, ctrl);
        }

        // ---------- Modèle ----------
        static Avatar ImportCharacter()
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(AjPath);
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = false;
            imp.importCameras = false; // le FBX Mixamo contient une caméra « bottom » qui remplaçait la vue du joueur
            imp.importLights = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            imp.SaveAndReimport();

            // Textures intégrées au FBX Mixamo → fichiers, pour que les matériaux les retrouvent
            if (!AssetDatabase.FindAssets("t:Texture2D", new[] { TexDir }).Any())
            {
                EnsureFolder(TexDir);
                if (imp.ExtractTextures(TexDir)) { AssetDatabase.Refresh(); imp.SaveAndReimport(); }
                else Debug.LogWarning("[M5] ⚠️ Aucune texture intégrée trouvée dans Aj.fbx.");
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexDir }))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                bool normal = ti.assetPath.ToLowerInvariant().Contains("normal");
                bool spec = ti.assetPath.ToLowerInvariant().Contains("spec");
                if ((normal && ti.textureType != TextureImporterType.NormalMap) || (spec && ti.sRGBTexture) || ti.maxTextureSize > 2048)
                {
                    if (normal) ti.textureType = TextureImporterType.NormalMap;
                    if (spec) ti.sRGBTexture = false;
                    ti.maxTextureSize = Mathf.Min(ti.maxTextureSize, 2048);
                    ti.SaveAndReimport();
                }
            }

            // Matériaux extraits (modifiables) puis remappés sur le FBX
            EnsureFolder(MatDir);
            foreach (var m in AssetDatabase.LoadAllAssetsAtPath(AjPath).OfType<Material>().ToList())
            {
                string p = $"{MatDir}/{m.name}.mat";
                if (!File.Exists(p))
                {
                    var err = AssetDatabase.ExtractAsset(m, p);
                    if (!string.IsNullOrEmpty(err)) Debug.LogWarning($"[M5] Extraction {m.name} : {err}");
                }
                else imp.AddRemap(new AssetImporter.SourceAssetIdentifier(m), AssetDatabase.LoadAssetAtPath<Material>(p));
            }
            AssetDatabase.WriteImportSettingsIfDirty(AjPath);
            AssetDatabase.ImportAsset(AjPath, ImportAssetOptions.ForceUpdate);

            var avatar = AssetDatabase.LoadAllAssetsAtPath(AjPath).OfType<Avatar>().FirstOrDefault();
            if (!avatar || !avatar.isHuman) Debug.LogWarning("[M5] ⚠️ Avatar humanoïde invalide pour Aj : vérifiez Rig > Humanoid > Configure.");
            return avatar;
        }

        /// Matériaux Mixamo → URP Lit opaque, textures assignées (les sourcils en découpe alpha).
        static void FixMaterials()
        {
            var tex = AssetDatabase.FindAssets("t:Texture2D", new[] { TexDir }).Select(AssetDatabase.GUIDToAssetPath)
                .ToDictionary(p => Path.GetFileNameWithoutExtension(p).ToLowerInvariant(), AssetDatabase.LoadAssetAtPath<Texture2D>);
            Texture2D Tex(string key) => tex.FirstOrDefault(kv => kv.Key.Contains(key)).Value;
            var lit = Shader.Find("Universal Render Pipeline/Lit");

            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { MatDir }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                string n = m.name.ToLowerInvariant();
                bool body = n.Contains("body"), brows = n.Contains("brow"), eyes = n.Contains("eye");
                var baseMap = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                if (lit) m.shader = lit;
                if (!baseMap) baseMap = body ? Tex("diffuse") : Tex("facial") ?? Tex("diffuse");
                m.SetTexture("_BaseMap", baseMap);
                m.SetColor("_BaseColor", Color.white);
                if (body && Tex("normal")) { m.SetTexture("_BumpMap", Tex("normal")); m.SetFloat("_BumpScale", 1f); }
                m.SetFloat("_Surface", 0f);
                m.SetFloat("_AlphaClip", brows ? 1f : 0f);
                m.SetFloat("_Cutoff", 0.4f);
                m.SetFloat("_Metallic", 0f);
                m.SetFloat("_Smoothness", eyes ? 0.85f : body ? 0.3f : 0.2f);
                m.SetFloat("_Cull", brows ? 0f : 2f);
                m.renderQueue = -1;
                UnityEditor.BaseShaderGUI.SetMaterialKeywords(m, UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.SetMaterialKeywords);
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
        }

        // ---------- Animations ----------
        static AnimationClip ImportClip(string path, string clipName, bool loop)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter imp)) return null;
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = true;
            imp.importCameras = false; imp.importLights = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.SaveAndReimport();

            var clips = imp.clipAnimations;
            if (clips == null || clips.Length == 0) clips = imp.defaultClipAnimations;
            foreach (var c in clips)
            {
                c.name = clipName;
                c.loopTime = loop; c.loopPose = loop;
                c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true; // reste sur place
                c.keepOriginalOrientation = true; c.keepOriginalPositionY = false; c.heightFromFeet = true;
            }
            imp.clipAnimations = clips;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }

        static AnimatorController BuildController(Motion idle, Motion wave, Motion clap, Motion cheer)
        {
            EnsureFolder(Root + "/Animations");
            AssetDatabase.DeleteAsset(CtrlPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CtrlPath);
            ctrl.AddParameter("Greet", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Confirm", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Talking", AnimatorControllerParameterType.Bool);
            var sm = ctrl.layers[0].stateMachine;

            var sIdle = sm.AddState("Idle", new Vector3(300, 0));
            sIdle.motion = idle;
            sm.defaultState = sIdle;

            if (wave)
            {
                var sWave = sm.AddState("Salut", new Vector3(300, -120));
                sWave.motion = wave;
                OnTrigger(sIdle, sWave, "Greet");
                After(sWave, sIdle, 2f); // deux saluts puis repos
            }

            // Commande envoyée : applaudit, saute de joie, puis repos
            AnimatorState sClap = null, sCheer = null;
            if (clap) { sClap = sm.AddState("Applaudit", new Vector3(560, 0)); sClap.motion = clap; }
            if (cheer) { sCheer = sm.AddState("Joie", new Vector3(560, 120)); sCheer.motion = cheer; }
            var first = sClap ?? sCheer;
            if (first)
            {
                var t = sm.AddAnyStateTransition(first);
                t.AddCondition(AnimatorConditionMode.If, 0, "Confirm");
                t.hasExitTime = false; t.duration = 0.2f; t.canTransitionToSelf = false;
            }
            if (sClap && sCheer) { After(sClap, sCheer, 2.5f); After(sCheer, sIdle, 0.92f); }
            else if (sClap) After(sClap, sIdle, 3f);
            else if (sCheer) After(sCheer, sIdle, 0.92f);

            AssetDatabase.SaveAssets();
            return ctrl;
        }

        static void OnTrigger(AnimatorState from, AnimatorState to, string param)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false; t.duration = 0.25f;
            t.AddCondition(AnimatorConditionMode.If, 0, param);
        }

        static void After(AnimatorState from, AnimatorState to, float exitTime)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = true; t.exitTime = exitTime; t.duration = 0.3f;
        }

        // ---------- Scène ----------
        static void SetupInScene(Avatar avatar, AnimatorController ctrl)
        {
            var aj = FindInstanceOf(AjPath);
            var oldNpc = Object.FindObjectsByType<VendorNPC>(FindObjectsInactive.Include)
                .FirstOrDefault(v => v.gameObject != aj);
            var old = oldNpc ? oldNpc.gameObject : null;

            if (!aj)
            {
                aj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AjPath));
                Undo.RegisterCreatedObjectUndo(aj, "M5 Aj");
                if (old)
                {
                    // Même place et même taille que l'ancien vendeur
                    aj.transform.SetPositionAndRotation(old.transform.position, old.transform.rotation);
                    float hOld = GetBounds(old).size.y, hNew = GetBounds(aj).size.y;
                    if (hOld > 0.5f && hNew > 0.01f) aj.transform.localScale *= hOld / hNew;
                }
                else
                {
                    var counter = FindCounter();
                    if (counter)
                    {
                        var cb = GetBounds(counter);
                        var front = FrontOf(cb, counter, null);
                        LocalExtents(cb, front, out _, out float hd);
                        aj.transform.SetPositionAndRotation(new Vector3(cb.center.x, cb.min.y, cb.center.z) - front * (hd + 0.5f), Quaternion.LookRotation(front));
                    }
                }
                Debug.Log("[M5] Aj ajouté dans la scène" + (old ? $" à la place de '{old.name}'." : "."));
            }
            Undo.RecordObject(aj, "M5 Aj"); aj.name = "Aj (vendeur)";

            // Pieds posés sur le sol du joueur (sinon sur le bas du comptoir)
            float? floorY = null;
            var sol = GameObject.Find("M5_SolCollider");
            if (sol && sol.GetComponent<Collider>()) floorY = sol.GetComponent<Collider>().bounds.max.y;
            else if (FindCounter() is GameObject counter) floorY = GetBounds(counter).min.y;
            if (floorY.HasValue)
            {
                Undo.RecordObject(aj.transform, "M5 Aj sol");
                var pos = aj.transform.position;
                aj.transform.position = new Vector3(pos.x, floorY.Value, pos.z);
            }

            // Animator
            var animator = aj.GetComponent<Animator>();
            if (!animator) animator = Undo.AddComponent<Animator>(aj);
            Undo.RecordObject(animator, "M5 Animator");
            animator.runtimeAnimatorController = ctrl;
            if (avatar) animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            foreach (var r in aj.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Undo.RecordObject(r, "M5 Ombres");
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            // Logique vendeur : reprend les réglages de l'ancien vendeur (textes, rayon…)
            var npc = aj.GetComponent<VendorNPC>();
            if (!npc) npc = Undo.AddComponent<VendorNPC>(aj);
            Undo.RecordObject(npc, "M5 NPC");
            if (oldNpc) EditorUtility.CopySerialized(oldNpc, npc);
            npc.animator = animator;
            npc.confirmText = "Merci ! Votre commande est en route !";
            float s = Mathf.Max(aj.transform.lossyScale.x, 0.0001f);
            var sc = aj.GetComponent<SphereCollider>(); if (!sc) sc = Undo.AddComponent<SphereCollider>(aj);
            Undo.RecordObject(sc, "M5 Trigger");
            sc.isTrigger = true; sc.radius = npc.detectRadius / s; sc.center = Vector3.up * (1f / s);
            var rb = aj.GetComponent<Rigidbody>(); if (!rb) rb = Undo.AddComponent<Rigidbody>(aj);
            Undo.RecordObject(rb, "M5 RB"); rb.isKinematic = true; rb.useGravity = false;

            // Ancien vendeur masqué (pas supprimé : réactivable dans la Hierarchy)
            if (old && old.activeSelf)
            {
                Undo.RecordObject(old, "M5 X Bot");
                old.SetActive(false);
                Debug.Log($"[M5] Ancien vendeur '{old.name}' désactivé (cochez-le dans l'Inspector pour le réafficher).");
            }

            // Références de la caisse → Aj
            foreach (var ui in Object.FindObjectsByType<M5CheckoutUI>(FindObjectsInactive.Include))
            { Undo.RecordObject(ui, "M5 UI"); ui.vendor = npc; EditorUtility.SetDirty(ui); }
            foreach (var p in Object.FindObjectsByType<ParcelAnimation>(FindObjectsInactive.Include))
            { Undo.RecordObject(p, "M5 Colis"); p.vendor = npc; EditorUtility.SetDirty(p); }

            EditorSceneManager.MarkSceneDirty(aj.scene);
            Selection.activeGameObject = aj;
            Debug.Log($"[M5] ✅ Aj prêt : skin URP, Animator {CtrlPath} (Idle → Salut à l'approche → Applaudit + Joie après la commande).");
        }
    }
}
