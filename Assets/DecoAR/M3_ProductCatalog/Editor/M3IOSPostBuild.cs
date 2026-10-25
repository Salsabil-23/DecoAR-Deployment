#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace DecoAR.M3.EditorTools
{
    // Projet Xcode : framework Speech pour la recherche vocale (F3.7) et textes d'autorisation en francais (9.2)
    public static class M3IOSPostBuild
    {
        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;

            string projPath = PBXProject.GetPBXProjectPath(path);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);
            string framework = proj.GetUnityFrameworkTargetGuid();
            proj.AddFrameworkToProject(framework, "Speech.framework", false);
            proj.AddFrameworkToProject(framework, "AVFoundation.framework", false);
            proj.WriteToFile(projPath);

            string plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            var root = plist.root;
            root.SetString("NSSpeechRecognitionUsageDescription",
                "DecoAR utilise la reconnaissance vocale pour trouver les produits que vous nommez.");
            root.SetString("NSMicrophoneUsageDescription",
                "DecoAR utilise le micro uniquement pendant la recherche vocale de produits.");
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
