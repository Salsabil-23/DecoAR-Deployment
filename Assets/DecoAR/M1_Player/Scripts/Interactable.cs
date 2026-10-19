using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DecoAR.Player
{
    // A poser sur une etagere (M3), le comptoir de caisse (M5)...
    // Le bouton d'interaction apparait quand l'avatar est a portee (F1.9)
    public class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        // Evenement global : M3 et M5 s'abonnent et filtrent sur Id
        public static event Action<Interactable> AnyInteracted;

        [SerializeField] string id = "etagere";          // ex. "etagere_tableaux", "caisse"
        [SerializeField] string prompt = "Interagir";    // texte du bouton
        [SerializeField] float range = 1.8f;
        [SerializeField] UnityEvent onInteract;

        public string Id => id;
        public string Prompt => prompt;
        public float Range => range;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        // Distance au plus proche bord de l'objet, au sol (ignore la hauteur)
        public float DistanceTo(Vector3 point)
        {
            Vector3 closest = TryGetComponent<Collider>(out var col) ? col.ClosestPoint(point) : transform.position;
            closest.y = point.y;
            return Vector3.Distance(closest, point);
        }

        public void Interact()
        {
            Debug.Log($"[M1] Interaction : {id}");
            onInteract?.Invoke();
            AnyInteracted?.Invoke(this);
        }
    }
}
