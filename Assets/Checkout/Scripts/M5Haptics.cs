using UnityEngine;

namespace DecoAR.M5
{
    /// Haptique M5 (aucun HapticManager commun trouvé dans le projet → Handheld.Vibrate).
    public static class M5Haptics
    {
        public static void Strong()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
