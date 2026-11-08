using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using static DecoAR.M5.M5EditorUtil;

namespace DecoAR.M5
{
    /// Build boutique (caisse) + catalogue M3 + AR M4 pour tester sur téléphone (Android : installé et lancé directement ; iOS : projet Xcode).
    /// Les réglages partagés du projet ne sont pas modifiés (pipeline M5 utilisé seulement le temps du build).
    public static class M5MobileBuild
    {
        const string ScenePath = Root + "/Scenes/caisse.unity";
        const string PipelinePath = Root + "/Settings/M5_URP.asset";

        [MenuItem("DecoAR/M5/8. Lancer la caisse sur le téléphone")]
        public static void Run()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            if (target != BuildTarget.Android && target != BuildTarget.iOS)
            {
                EditorUtility.DisplayDialog("DecoAR M5", "Choisissez d'abord la plateforme du téléphone :\nFile > Build Profiles > Android (ou iOS) > Switch Platform.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string output = target == BuildTarget.Android ? "Builds/DecoAR_Caisse.apk" : "Builds/DecoAR_Caisse_iOS";
            Directory.CreateDirectory("Builds");
            var options = new BuildPlayerOptions
            {
                // la boutique (caisse) d'abord, puis le catalogue M3 et l'AR M4 qu'elle ouvre
                scenes = new[] { ScenePath, "Assets/DecoAR/M3_ProductCatalog/Scenes/M3.unity", "Assets/DecoAR/M4_AR/Scenes/M4_AR.unity" },
                locationPathName = output,
                target = target,
                options = target == BuildTarget.Android ? BuildOptions.AutoRunPlayer : BuildOptions.None,
            };

            // Les lumières d'appoint du pipeline M5 doivent être compilées : on le déclare le temps du build
            var m5 = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);
            var previous = QualitySettings.renderPipeline;
            if (m5) QualitySettings.renderPipeline = m5;
            // La caisse est en paysage uniquement, dès l'écran de lancement
            var orient = PlayerSettings.defaultInterfaceOrientation;
            bool p = PlayerSettings.allowedAutorotateToPortrait, pu = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            bool l = PlayerSettings.allowedAutorotateToLandscapeLeft, r = PlayerSettings.allowedAutorotateToLandscapeRight;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeRight = true;
            BuildReport report;
            try { report = BuildPipeline.BuildPlayer(options); }
            finally
            {
                if (m5) QualitySettings.renderPipeline = previous;
                PlayerSettings.defaultInterfaceOrientation = orient;
                PlayerSettings.allowedAutorotateToPortrait = p; PlayerSettings.allowedAutorotateToPortraitUpsideDown = pu;
                PlayerSettings.allowedAutorotateToLandscapeLeft = l; PlayerSettings.allowedAutorotateToLandscapeRight = r;
                AssetDatabase.SaveAssets(); // le build a écrit les réglages temporaires sur le disque : on réécrit ceux de l'équipe
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[M5] ❌ Build échoué ({report.summary.totalErrors} erreur(s)) : voir la Console.");
                return;
            }
            if (target == BuildTarget.iOS)
            {
                EditorUtility.RevealInFinder(output);
                Debug.Log($"[M5] ✅ Projet Xcode prêt : {output}/Unity-iPhone.xcodeproj → ouvrez-le, Signing & Capabilities > votre Team, puis ▶.");
            }
            else Debug.Log($"[M5] ✅ APK installé et lancé sur le téléphone ({output}).");
        }
    }
}
