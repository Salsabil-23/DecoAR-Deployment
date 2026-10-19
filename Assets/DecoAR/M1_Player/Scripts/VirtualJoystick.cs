using UnityEngine;
using UnityEngine.EventSystems;

namespace DecoAR.Player
{
    // Joystick tactile (F1.4). Value : x = tourner, y = avancer / reculer, dans [-1, 1]
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform background;  // pivot au centre
        [SerializeField] RectTransform handle;      // enfant de background, ancre au centre
        [SerializeField, Range(0f, 0.5f)] float deadZone = 0.1f;

        public Vector2 Value { get; private set; }

        public void OnPointerDown(PointerEventData e) => OnDrag(e);

        public void OnDrag(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, e.position, e.pressEventCamera, out var local))
                return;

            float radius = background.rect.width * 0.5f;
            Vector2 v = Vector2.ClampMagnitude(local / radius, 1f);
            handle.anchoredPosition = v * radius;
            Value = v.magnitude < deadZone ? Vector2.zero : v;
        }

        public void OnPointerUp(PointerEventData e) => ResetStick();

        void OnDisable() => ResetStick();

        void ResetStick()
        {
            Value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }
    }
}
