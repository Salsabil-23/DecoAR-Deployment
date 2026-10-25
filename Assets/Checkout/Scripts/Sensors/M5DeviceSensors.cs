using UnityEngine;
using UnityEngine.InputSystem;

namespace DecoAR.M5
{
    /// Capteurs du téléphone (Input System). Gyroscope = AttitudeSensor (orientation complète), sinon gravité / accéléromètre (inclinaison seule).
    /// Les valeurs sont déjà compensées selon l'orientation de l'écran (InputSettings.compensateForScreenOrientation).
    public static class M5DeviceSensors
    {
        public static bool HasAttitude => AttitudeSensor.current != null;
        public static bool HasGravity => GravitySensor.current != null || Accelerometer.current != null;
        public static bool HasAny => HasAttitude || HasGravity;

        public static void Enable()
        {
            if (AttitudeSensor.current != null) { Enable(AttitudeSensor.current); return; }
            if (GravitySensor.current != null) Enable(GravitySensor.current);
            else if (Accelerometer.current != null) Enable(Accelerometer.current);
        }

        public static void Disable()
        {
            if (AttitudeSensor.current != null && AttitudeSensor.current.enabled) InputSystem.DisableDevice(AttitudeSensor.current);
            if (GravitySensor.current != null && GravitySensor.current.enabled) InputSystem.DisableDevice(GravitySensor.current);
            if (Accelerometer.current != null && Accelerometer.current.enabled) InputSystem.DisableDevice(Accelerometer.current);
        }

        static void Enable(Sensor s)
        {
            if (!s.enabled) InputSystem.EnableDevice(s);
            try { s.samplingFrequency = 60f; } catch (System.NotSupportedException) { } // fréquence imposée par certains téléphones
        }

        /// Rotation monde de la caméra d'après le gyroscope (cap arbitraire : à recaler par le joueur).
        public static bool TryGetCameraRotation(out Quaternion rot)
        {
            rot = Quaternion.identity;
            var s = AttitudeSensor.current;
            if (s == null || !s.enabled) return false;
            var q = s.attitude.ReadValue();
            if (q.x == 0f && q.y == 0f && q.z == 0f && q.w == 0f) return false; // pas encore de mesure
            // Repère capteur (main droite, Z vertical) → repère Unity (main gauche, Y vertical)
            rot = Quaternion.Euler(90f, 0f, 0f) * new Quaternion(q.x, q.y, -q.z, -q.w);
            return true;
        }

        /// Inclinaison avant/arrière (degrés, + = regarder vers le bas) d'après la gravité.
        public static bool TryGetGravityPitch(out float pitch)
        {
            pitch = 0f;
            Vector3 g;
            if (GravitySensor.current != null && GravitySensor.current.enabled) g = GravitySensor.current.gravity.ReadValue();
            else if (Accelerometer.current != null && Accelerometer.current.enabled) g = Accelerometer.current.acceleration.ReadValue();
            else return false;
            if (g.sqrMagnitude < 0.01f) return false;
            pitch = Mathf.Atan2(-g.z, -g.y) * Mathf.Rad2Deg;
            return true;
        }
    }
}
