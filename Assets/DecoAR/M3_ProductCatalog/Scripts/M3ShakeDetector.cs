using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DecoAR.M3
{
    // F3.8 : secouer le telephone repose le produit inspecte.
    // Seuil d'acceleration + delai anti-rebond (cahier 11.2) pour qu'un geste normal ne declenche rien.
    // Simulation clavier dans l'editeur : touche R.
    public class M3ShakeDetector : MonoBehaviour
    {
        [SerializeField] float thresholdG = 2.2f;     // ~1 g au repos (gravite comprise)
        [SerializeField] int samplesAbove = 2;         // pics consecutifs requis
        [SerializeField] float cooldown = 1.2f;

        public event Action Shaken;

        int above;
        float nextAllowed;

        void OnEnable()
        {
            if (Accelerometer.current != null && !Accelerometer.current.enabled)
                InputSystem.EnableDevice(Accelerometer.current);
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                Fire();
                return;
            }

            var acc = Accelerometer.current;
            if (acc == null || !acc.enabled) return;

            float g = acc.acceleration.ReadValue().magnitude;
            above = g > thresholdG ? above + 1 : 0;
            if (above >= samplesAbove) Fire();
        }

        void Fire()
        {
            above = 0;
            if (Time.unscaledTime < nextAllowed) return;
            nextAllowed = Time.unscaledTime + cooldown;
            Debug.Log("[M3] Secousse detectee");
            Shaken?.Invoke();
        }
    }
}
