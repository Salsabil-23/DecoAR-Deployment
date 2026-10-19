using System.Runtime.InteropServices;
using UnityEngine;

namespace DecoAR.AR
{
    public static class M4Haptics
    {
        public enum Strength { Light = 0, Medium = 1, Strong = 2 }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void _DecoARM4_Impact(int style);
#endif

        public static void Play(Strength strength)
        {
            // Interrupteur des parametres (cle a aligner avec M5)
            if (PlayerPrefs.GetInt("haptics_enabled", 1) == 0) return;
#if UNITY_IOS && !UNITY_EDITOR
            _DecoARM4_Impact((int)strength);
#elif UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        public static void Medium() => Play(Strength.Medium);
    }
}