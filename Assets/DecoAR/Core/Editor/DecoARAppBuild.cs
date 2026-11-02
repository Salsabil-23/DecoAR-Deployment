using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DecoAR.Core.EditorTools
{
    // Application complete, dans l'ordre du parcours du cahier des charges :
    // connexion (Auth) -> creer son personnage (M1) -> boutique (caisse M5) -> rayon (M3) -> AR (M4) -> caisse.
    // Les reglages partages du projet ne sont modifies que le temps du build.
    public static class DecoARAppBuild
    {
        public static readonly string[] Scenes =
        {
            "Assets/DecoAR/Auth/Scenes/Auth.unity",                         // inscription / connexion / Face ID
            "Assets/DecoAR/Auth/Scenes/Profile.unity",                      // profil (bouton rond en haut a gauche)
            "Assets/DecoAR/M1_Player/Scenes/M1_Personnalisation.unity",     // choix de l'avatar, couleurs, scan
            "Assets/DecoAR/M1_Player/Scenes/M1_ScanVisage.unity",           // scan 3D du visage
            "Assets/Checkout/Scenes/caisse.unity",                          // boutique et caisse (SceneNames.Shop)
            "Assets/DecoAR/M3_ProductCatalog/Scenes/M3.unity",              // rayon et fiche produit
            "Assets/DecoAR/M4_AR/Scenes/M4_AR.unity",                       // essai en realite augmentee
        };

        // La caisse change de pipeline en jeu (lumieres d'appoint) : ses variantes de shaders doivent etre compilees
        const string ShopPipelinePath = "Assets/Checkout/Settings/M5_URP.asset";

        [MenuItem("DecoAR/Application complete/1. Mettre la liste des scenes du parcours")]
        public static void SetSceneList()
        {
            var missing = Scenes.Where(s => !File.Exists(s)).ToArray();
            if (missing.Length > 0)
            {
                EditorUtility.DisplayDialog("DecoAR", "Scenes introuvables :\n" + string.Join("\n", missing), "OK");
                return;
            }
            var others = EditorBuildSettings.scenes.Where(s => !Scenes.Contains(s.path) && File.Exists(s.path))
                .Select(s => new EditorBuildSettingsScene(s.path, false));
            EditorBuildSettings.scenes = Scenes.Select(s => new EditorBuildSettingsScene(s, true)).Concat(others).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[DecoAR] Liste des scenes : " + string.Join(" -> ", Scenes.Select(Path.GetFileNameWithoutExtension)));
        }

        [MenuItem("DecoAR/Application complete/2. Construire et lancer sur le telephone")]
        public static void BuildAndRun()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            if (target != BuildTarget.Android && target != BuildTarget.iOS)
            {
                EditorUtility.DisplayDialog("DecoAR", "Choisissez d'abord la plateforme du telephone :\nFile > Build Profiles > Android (ou iOS) > Switch Platform.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SetSceneList();

            string output = target == BuildTarget.Android ? "Builds/DecoAR.apk" : "Builds/DecoAR_iOS";
            Directory.CreateDirectory("Builds");
            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = output,
                target = target,
                options = target == BuildTarget.Android ? BuildOptions.AutoRunPlayer : BuildOptions.None,
            };

            var shop = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(ShopPipelinePath);
            var previous = QualitySettings.renderPipeline;
            // Ecrans M1 en portrait, boutique en paysage (la caisse impose le paysage elle-meme)
            var orient = PlayerSettings.defaultInterfaceOrientation;
            bool p = PlayerSettings.allowedAutorotateToPortrait, pu = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            bool l = PlayerSettings.allowedAutorotateToLandscapeLeft, r = PlayerSettings.allowedAutorotateToLandscapeRight;
            if (shop) QualitySettings.renderPipeline = shop;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeRight = true;
            BuildReport report;
            try { report = BuildPipeline.BuildPlayer(options); }
            finally
            {
                if (shop) QualitySettings.renderPipeline = previous;
                PlayerSettings.defaultInterfaceOrientation = orient;
                PlayerSettings.allowedAutorotateToPortrait = p; PlayerSettings.allowedAutorotateToPortraitUpsideDown = pu;
                PlayerSettings.allowedAutorotateToLandscapeLeft = l; PlayerSettings.allowedAutorotateToLandscapeRight = r;
                AssetDatabase.SaveAssets();
            }

            if (report.summary.result != BuildResult.Succeeded)
                Debug.LogError($"[DecoAR] Build echoue ({report.summary.totalErrors} erreur(s)) : voir la Console.");
            else if (target == BuildTarget.iOS)
                Debug.Log($"[DecoAR] Projet Xcode pret : {output}");
            else
                Debug.Log($"[DecoAR] APK installe et lance sur le telephone ({output}).");
        }
    }
}
