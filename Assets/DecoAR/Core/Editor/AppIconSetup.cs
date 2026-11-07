using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
#if UNITY_ANDROID
using UnityEditor.Android;
#endif

namespace DecoAR.Core.EditorTools
{
    // Icone DecoAR (design system, DesignSystem/app-icon) dans les Player Settings : icone par defaut (iOS et autres),
    // et sur Android l'icone adaptative (couches 432 px du design system).
    public static class AppIconSetup
    {
        const string Folder = "Assets/DecoAR/Core/Icons/";

        [MenuItem("DecoAR/Appliquer l'icone de l'application")]
        public static void Apply()
        {
            var main = Load("DecoAR_Icon.png");
            if (main == null) return;

            // Icone par defaut, utilisee par toutes les plateformes sans icone propre (iOS refuse la transparence)
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { main }, IconKind.Any);

#if UNITY_ANDROID
            // Android (Unity 6) : seule l'icone adaptative reste, calque 0 = fond, calque 1 = dessin
            var android = NamedBuildTarget.Android;
            var background = Load("DecoAR_Icon_Background.png");
            var foreground = Load("DecoAR_Icon_Foreground.png");
            var adaptive = PlayerSettings.GetPlatformIcons(android, AndroidPlatformIconKind.Adaptive);
            foreach (var icon in adaptive) icon.SetTextures(background, foreground);
            PlayerSettings.SetPlatformIcons(android, AndroidPlatformIconKind.Adaptive, adaptive);
#endif

            AssetDatabase.SaveAssets();
            Debug.Log("[DecoAR] Icone de l'application appliquee (Player Settings > Icon).");
        }

        static Texture2D Load(string file)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + file);
            if (texture == null)
                Debug.LogError($"[DecoAR] {Folder}{file} introuvable : copier les icones de DesignSystem/app-icon");
            return texture;
        }
    }
}
