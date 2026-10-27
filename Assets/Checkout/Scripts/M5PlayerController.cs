using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TPhase = UnityEngine.InputSystem.TouchPhase;

namespace DecoAR.M5
{
    /// Joueur 1ère personne. PC : ZQSD/flèches + clic droit maintenu pour regarder. Mobile : moitié gauche = joystick, moitié droite = regarder.
    [RequireComponent(typeof(CharacterController))]
    public class M5PlayerController : MonoBehaviour
    {
        public Transform cam;
        public float moveSpeed = 2f;
        public float mouseLook = 0.15f;
        public float touchLook = 0.15f;
        public float gravity = -9.81f;
        public float minPitch = -60f, maxPitch = 70f;

        public bool JoystickActive => moveTouch >= 0;
        public Vector2 JoystickOrigin => moveStart;
        public Vector2 JoystickVector => moveVec;
        public float JoystickRadius => Screen.height * 0.1f;

        CharacterController cc;
        float yaw, pitch, vy;
        int moveTouch = -1, lookTouch = -1;
        Vector2 moveStart, moveVec, lastLook;
        Coroutine focus;
        static readonly List<RaycastResult> hits = new List<RaycastResult>();

        void OnEnable() => EnhancedTouchSupport.Enable();

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (!cam && Camera.main) cam = Camera.main.transform;
            yaw = transform.eulerAngles.y;
            if (cam) { pitch = cam.localEulerAngles.x; if (pitch > 180f) pitch -= 360f; }
        }

        void Update()
        {
            Vector2 move = Vector2.zero, look = Vector2.zero;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1;
            }
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed) look += mouse.delta.ReadValue() * mouseLook;

            foreach (var t in ETouch.activeTouches)
            {
                int id = t.touchId; var pos = t.screenPosition;
                switch (t.phase)
                {
                    case TPhase.Began:
                        if (IsOverUI(pos)) break;
                        if (pos.x < Screen.width * 0.5f && moveTouch < 0) { moveTouch = id; moveStart = pos; moveVec = Vector2.zero; }
                        else if (lookTouch < 0) { lookTouch = id; lastLook = pos; }
                        break;
                    case TPhase.Moved:
                    case TPhase.Stationary:
                        if (id == moveTouch) moveVec = Vector2.ClampMagnitude((pos - moveStart) / JoystickRadius, 1f);
                        else if (id == lookTouch) { look += (pos - lastLook) * touchLook; lastLook = pos; }
                        break;
                    default:
                        if (id == moveTouch) { moveTouch = -1; moveVec = Vector2.zero; }
                        if (id == lookTouch) lookTouch = -1;
                        break;
                }
            }

            move = Vector2.ClampMagnitude(move + moveVec, 1f);
            if (look.sqrMagnitude > 0f && focus != null) { StopCoroutine(focus); focus = null; }
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
            ApplyRotation();

            var v = (transform.forward * move.y + transform.right * move.x) * moveSpeed;
            vy = cc.isGrounded ? -1f : vy + gravity * Time.deltaTime;
            v.y = vy;
            cc.Move(v * Time.deltaTime);
        }

        void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            if (cam) cam.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        /// Oriente doucement la vue vers un point (ex. le panier).
        public void FocusOn(Vector3 point, float duration = 0.8f)
        {
            if (focus != null) StopCoroutine(focus);
            focus = StartCoroutine(FocusRoutine(point, duration));
        }

        IEnumerator FocusRoutine(Vector3 point, float duration)
        {
            var eye = cam ? cam.position : transform.position;
            var d = point - eye;
            float ty = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float tp = Mathf.Clamp(-Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg, minPitch, maxPitch);
            float y0 = yaw, p0 = pitch;
            for (float e = 0; e < duration; e += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, e / duration);
                yaw = Mathf.LerpAngle(y0, ty, k); pitch = Mathf.Lerp(p0, tp, k);
                ApplyRotation();
                yield return null;
            }
            focus = null;
        }

        static bool IsOverUI(Vector2 p)
        {
            var es = EventSystem.current;
            if (!es) return false;
            hits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = p }, hits);
            return hits.Count > 0;
        }
    }
}
