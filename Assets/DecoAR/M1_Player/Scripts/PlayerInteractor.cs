using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DecoAR.Player
{
    // Affiche le bouton contextuel pres d'un Interactable et declenche l'interaction (F1.9)
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] Button interactButton;
        [SerializeField] TMP_Text interactLabel;

        PlayerController player;

        public Interactable Current { get; private set; }

        void Awake()
        {
            player = GetComponent<PlayerController>();
            if (interactButton != null)
            {
                interactButton.onClick.AddListener(Interact);
                interactButton.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            Interactable best = null;
            float bestDist = float.MaxValue;
            Vector3 p = transform.position;

            foreach (var it in Interactable.All)
            {
                float d = it.DistanceTo(p);
                if (d <= it.Range && d < bestDist)
                {
                    best = it;
                    bestDist = d;
                }
            }

            if (best != Current)
            {
                Current = best;
                if (interactButton != null) interactButton.gameObject.SetActive(best != null);
                if (best != null && interactLabel != null) interactLabel.text = best.Prompt;
            }

            // Touche E pour l'editeur
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame) Interact();
        }

        public void Interact()
        {
            if (Current == null || player.InputLocked) return;

            // L'avatar se tourne vers l'objet puis tend la main
            Vector3 dir = Current.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);

            player.PlayInteract();
            Current.Interact();
        }
    }
}
