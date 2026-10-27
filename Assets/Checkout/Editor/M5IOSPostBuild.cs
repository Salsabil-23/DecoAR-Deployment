#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace DecoAR.M5
{
    // Projet Xcode : WebKit (paiement dans l'app) + texte d'autorisation de la position (bouton « Utiliser ma position »)
    public static class M5IOSPostBuild
    {
        [PostProcessBuild(101)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;

            // WebKit : page de paiement dans l'app (Plugins/iOS/M5WebView.mm)
            string projPath = PBXProject.GetPBXProjectPath(path);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);
            proj.AddFrameworkToProject(proj.GetUnityFrameworkTargetGuid(), "WebKit.framework", false);
            proj.WriteToFile(projPath);

            string plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            var root = plist.root;
            if (!root.values.ContainsKey("NSLocationWhenInUseUsageDescription") || string.IsNullOrEmpty(root["NSLocationWhenInUseUsageDescription"].AsString()))
                root.SetString("NSLocationWhenInUseUsageDescription",
                    "DecoAR utilise votre position uniquement pour remplir votre adresse de livraison.");
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
