using UnityEngine;
using UnityEngine.EventSystems;

namespace DecoAR.Player
{
    // Zone d'ecran transparente : glisser le doigt fait tourner le personnage
    public class PreviewDragArea : MonoBehaviour, IDragHandler
    {
        [SerializeField] Transform target;
        [SerializeField] float degreesPerPixel = 0.35f;

        public void OnDrag(PointerEventData e)
        {
            if (target != null) target.Rotate(0f, -e.delta.x * degreesPerPixel, 0f);
        }
    }
}
