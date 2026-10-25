using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DecoAR.M5
{
    public static class M5VendorSetup
    {
        static readonly string[] IdleK = { "idle", "breath", "standing" };
        static readonly string[] GreetK = { "wav", "greet", "salut", "hello", "bonjour" };
        static readonly string[] TalkK = { "talk", "parl" };
        static readonly string[] ConfirmK = { "clap", "thumb", "nod", "cheer", "bow", "victory" };
        static readonly string[] NotCharacter = { "desk", "basket", "panier", "comptoir", "counter", "caisse" };

        [MenuItem("DecoAR/M5/1. Setup Vendeur (Animation)")]
        public static void Run()
        {
            var vendor = M5EditorUtil.FindVendor();
            if (!vendor) return;
            if (M5AjSetup.IsAj(vendor)) { Debug.Log("[M5] Le vendeur est Aj → menu 5."); M5AjSetup.Run(); return; }

            // 1. FBX du vendeur + FBX d'animation
            var src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(vendor);
            string modelPath = src ? AssetDatabase.GetAssetPath(src) : null;
            var paths = AssetDatabase.FindAssets("t:Model", new[] { M5EditorUtil.Root })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                .Where(p => p == modelPath || IsCharacterFbx(p)).Distinct().ToList();
            if (string.IsNullOrEmpty(modelPath) || !modelPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                modelPath = paths.FirstOrDefault();
            if (modelPath == null) { Debug.LogWarning("[M5] ⚠️ Aucun FBX de personnage trouvé dans " + M5EditorUtil.Root); return; }
            paths.Remove(modelPath); paths.Insert(0, modelPath);
            Debug.Log("[M5] FBX vendeur/animations : " + string.Join(", ", paths));

            Avatar avatar = null;
            foreach (var p in paths)
            {
                if (!(AssetImporter.GetAtPath(p) is ModelImporter imp)) continue;
                imp.animationType = ModelImporterAnimationType.Human;
                if (p == modelPath || !avatar) imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                else { imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther; imp.sourceAvatar = avatar; }
                imp.importAnimation = true;
                imp.SaveAndReimport(); // génère les clips humanoïdes par défaut

                var clips = imp.clipAnimations;
                if (clips == null || clips.Length == 0) clips = imp.defaultClipAnimations;
                foreach (var c in clips)
                {
                                        c.loopTime = true; c.loopPose = true;
                    c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
                    c.keepOriginalPositionY = false; c.heightFromFeet = true;
                }
                imp.clipAnimations = clips;
                imp.SaveAndReimport();

                if (p == modelPath)
                {
                    avatar = AssetDatabase.LoadAllAssetsAtPath(p).OfType<Avatar>().FirstOrDefault();
                    if (!avatar || !avatar.isHuman) Debug.LogWarning("[M5] ⚠️ Avatar humanoïde invalide pour " + p);
                }
            }

            // 2. Clip Waving (joué en boucle en permanence)
            var greetClip = paths.SelectMany(p => AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview__")).Select(c => (clip: c, key: c.name + " " + Path.GetFileName(p))))
                .FirstOrDefault(x => M5EditorUtil.Has(x.key, GreetK)).clip;
            if (!greetClip) { Debug.LogWarning("[M5] ⚠️ clip Waving introuvable dans " + M5EditorUtil.Root); return; }
            Debug.Log($"[M5] Clip Waving : {AssetDatabase.GetAssetPath(greetClip)}");

            // 3. Animator Controller : un seul état Wave en boucle
            string dir = M5EditorUtil.Root + "/Animations";
            M5EditorUtil.EnsureFolder(dir);
            string cpath = dir + "/VendorAnimator.controller";
            AssetDatabase.DeleteAsset(cpath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(cpath);
            ctrl.AddParameter("Greet", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Confirm", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Talking", AnimatorControllerParameterType.Bool);
            var sm = ctrl.layers[0].stateMachine;
            var wave = sm.AddState("Wave", new Vector3(300, 0));
            wave.motion = greetClip;
            sm.defaultState = wave;
            AssetDatabase.SaveAssets();

            // 4. Animator
            var animator = vendor.GetComponentInChildren<Animator>();
            if (!animator) animator = Undo.AddComponent<Animator>(vendor);
            Undo.RecordObject(animator, "M5 Animator");
            animator.runtimeAnimatorController = ctrl;
            if (avatar) animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            // 5. VendorNPC + trigger
            if (!vendor.GetComponent<VendorNPC>()) Undo.AddComponent<VendorNPC>(vendor);
            var npc = vendor.GetComponent<VendorNPC>();
            Undo.RecordObject(npc, "M5 NPC"); npc.animator = animator;
            var sc = vendor.GetComponent<SphereCollider>(); if (!sc) sc = Undo.AddComponent<SphereCollider>(vendor);
            Undo.RecordObject(sc, "M5 Trigger");
            float s = Mathf.Max(vendor.transform.lossyScale.x, 0.0001f);
            sc.isTrigger = true; sc.radius = 2.5f / s; sc.center = Vector3.up * (1f / s);
            var rb = vendor.GetComponent<Rigidbody>(); if (!rb) rb = Undo.AddComponent<Rigidbody>(vendor);
            Undo.RecordObject(rb, "M5 RB"); rb.isKinematic = true; rb.useGravity = false;

            EditorSceneManager.MarkSceneDirty(vendor.scene);
            Selection.activeGameObject = vendor;
            Debug.Log($"[M5] ✅ Vendeur prêt : controller {cpath}, Animator sur '{animator.gameObject.name}'.");
        }

        static bool IsCharacterFbx(string p)
        {
            var n = Path.GetFileName(p);
            if (M5EditorUtil.Has(n, NotCharacter)) return false;
            return n.Contains("@") || M5EditorUtil.Has(n, new[] { "bot", "mixamo", "vendeur", "vendor", "character" })
                || M5EditorUtil.Has(n, IdleK) || M5EditorUtil.Has(n, GreetK) || M5EditorUtil.Has(n, TalkK) || M5EditorUtil.Has(n, ConfirmK);
        }

        static void Trigger(AnimatorState from, AnimatorState to, string param)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false; t.duration = 0.2f;
            t.AddCondition(AnimatorConditionMode.If, 0, param);
        }

        static void ExitBack(AnimatorState from, AnimatorState to)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.2f;
        }
    }
}
