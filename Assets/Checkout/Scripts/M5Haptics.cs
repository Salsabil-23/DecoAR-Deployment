using DecoAR.Core;

namespace DecoAR.M5
{
    /// Haptique M5 → HapticManager commun (respecte l'option « vibrations » du GameManager).
    public static class M5Haptics
    {
        public static void Strong() => HapticManager.Vibrate(HapticIntensity.Forte);

        /// Retour léger (toucher un écran) : iOS uniquement, sur Android la seule vibration disponible est trop forte.
        public static void Light()
        {
#if UNITY_IOS
            HapticManager.Vibrate(HapticIntensity.Legere);
#endif
        }
    }
}
