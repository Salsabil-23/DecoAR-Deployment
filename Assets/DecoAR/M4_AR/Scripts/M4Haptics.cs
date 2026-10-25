using DecoAR.Core;

namespace DecoAR.AR
{
    // Raccourci M4 vers le HapticManager commun (respecte l'interrupteur des parametres du GameManager)
    public static class M4Haptics
    {
        public enum Strength { Light = 0, Medium = 1, Strong = 2 }

        public static void Play(Strength strength)
        {
            HapticManager.Vibrate(strength == Strength.Strong ? HapticIntensity.Forte
                : strength == Strength.Medium ? HapticIntensity.Moyenne
                : HapticIntensity.Legere);
        }

        public static void Medium() => Play(Strength.Medium);
    }
}
