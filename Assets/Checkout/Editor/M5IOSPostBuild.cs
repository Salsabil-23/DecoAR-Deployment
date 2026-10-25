#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace DecoAR.M5
{
    // Projet Xcode : texte d'autorisation de la position (bouton « Utiliser ma position » de la caisse)
    public static class M5IOSPostBuild
    {
        [PostProcessBuild(101)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
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
