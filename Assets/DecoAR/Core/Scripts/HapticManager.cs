using System.Runtime.InteropServices;
using UnityEngine;

namespace DecoAR.Core
{
    // Intensites du tableau 9.1 du cahier des charges
    public enum HapticIntensity { Legere, Courte, Moyenne, Forte }

    // Point d'entree unique pour les vibrations.
    // iOS : UIImpactFeedbackGenerator (Plugins/iOS/DecoARM4Haptics.mm) pour differencier les intensites ;
    // Android : Handheld.Vibrate ne fait qu'une vibration fixe.
    public static class HapticManager
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void _DecoARM4_Impact(int style);
#endif

        public static void Vibrate(HapticIntensity intensity)
        {
            var gm = GameManager.Instance;
            if (gm != null && !gm.HapticsEnabled) return;

            Debug.Log($"[Haptic] {intensity}");
#if UNITY_IOS && !UNITY_EDITOR
            // 0 = leger, 1 = moyen, 2 = fort
            int style = intensity == HapticIntensity.Forte ? 2 : intensity == HapticIntensity.Moyenne ? 1 : 0;
            _DecoARM4_Impact(style);
#elif UNITY_ANDROID
            if (!Application.isEditor) Handheld.Vibrate();
#endif
        }
    }
}
