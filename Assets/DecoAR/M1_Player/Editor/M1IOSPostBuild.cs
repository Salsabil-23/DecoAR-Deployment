#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace DecoAR.Player.EditorTools
{
    // Projet Xcode : le scan envoie les photos au serveur de tete 3D (M1_FaceServer) sur le PC du meme Wi-Fi.
    // iOS bloque l'acces au reseau local sans ce texte d'autorisation, et le HTTP sans exception ATS.
    public static class M1IOSPostBuild
    {
        [PostProcessBuild(101)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;

            string plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            var root = plist.root;
            root.SetString("NSLocalNetworkUsageDescription",
                "DecoAR envoie les photos de ton scan à l'ordinateur de ton réseau Wi-Fi qui crée ta tête 3D.");

            var ats = root["NSAppTransportSecurity"] as PlistElementDict ?? root.CreateDict("NSAppTransportSecurity");
            ats.SetBoolean("NSAllowsLocalNetworking", true);
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
