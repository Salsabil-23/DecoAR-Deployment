using UnityEngine;

namespace DecoAR.Core
{
    // Intensites du tableau 9.1 du cahier des charges
    public enum HapticIntensity { Legere, Courte, Moyenne, Forte }

    // Point d'entree unique pour les vibrations.
    // Version minimale : Handheld.Vibrate ne fait qu'une vibration fixe ;
    // M5 pourra brancher ici un plugin d'haptique pour differencier les intensites.
    public static class HapticManager
    {
        public static void Vibrate(HapticIntensity intensity)
        {
            var gm = GameManager.Instance;
            if (gm != null && !gm.HapticsEnabled) return;

            Debug.Log($"[Haptic] {intensity}");
#if UNITY_ANDROID || UNITY_IOS
            if (!Application.isEditor) Handheld.Vibrate();
#endif
        }
    }
}
