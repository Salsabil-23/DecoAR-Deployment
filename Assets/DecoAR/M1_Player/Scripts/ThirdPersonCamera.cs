using System.Collections.Generic;
using DecoAR.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace DecoAR.Player
{
    // Camera 3e personne qui ne traverse pas les murs (F1.5),
    // orientable au doigt ou au gyroscope (F1.6)
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 pivotOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Distance et angles")]
        [SerializeField] float distance = 3.2f;
        [SerializeField] float minDistance = 0.4f;
        [SerializeField] float defaultPitch = 15f;
        [SerializeField] float minPitch = -20f;
        [SerializeField] float maxPitch = 60f;

        [Header("Doigt / souris")]
        [SerializeField] float dragSensitivity = 0.2f;  // degres par pixel
        [SerializeField] float recenterDelay = 2f;      // sans geste, la camera revient derriere l'avatar
        [SerializeField] float recenterSpeed = 90f;

        [Header("Gyroscope")]
        [SerializeField] float gyroSensitivity = 1f;
        [SerializeField] float gyroMaxYaw = 70f;
        [SerializeField] float gyroMaxPitch = 35f;
        [SerializeField] bool invertGyroPitch = false;

        [Header("Collision")]
        [SerializeField] float collisionRadius = 0.25f;
        [SerializeField] LayerMask obstacleMask = Physics.DefaultRaycastLayers;

        float yawOffset;
        float pitch;
        float currentDistance;
        int dragTouchId = -1;
        float lastDragTime = -999f;

        bool gyroActive;
        Quaternion gyroReference = Quaternion.identity;

        readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        public static bool GyroAvailable => AttitudeSensor.current != null;

        void Awake()
        {
            pitch = defaultPitch;
            currentDistance = distance;
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.GyroChanged += SetGyro;
                SetGyro(gm.GyroEnabled);
            }
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            var gm = GameManager.Instance;
            if (gm != null) gm.GyroChanged -= SetGyro;
            SetGyro(false);
        }

        public void SetTarget(Transform t) => target = t;

        void SetGyro(bool on)
        {
            var sensor = AttitudeSensor.current;
            if (on && sensor != null)
            {
                InputSystem.EnableDevice(sensor);
                gyroActive = true;
                Recalibrate();
            }
            else
            {
                gyroActive = false;
                if (sensor != null && sensor.enabled) InputSystem.DisableDevice(sensor);
            }
        }

        // L'orientation actuelle du telephone devient la position "neutre"
        public void Recalibrate()
        {
            if (gyroActive) gyroReference = ReadAttitude();
        }

        static Quaternion ReadAttitude()
        {
            Quaternion q = AttitudeSensor.current.attitude.ReadValue();
            // Repere du telephone (main droite) -> repere Unity (main gauche)
            return Quaternion.Euler(90f, 0f, 0f) * new Quaternion(q.x, q.y, -q.z, -q.w);
        }

        void LateUpdate()
        {
            if (target == null) return;

            HandleDrag();

            float yaw = target.eulerAngles.y + yawOffset;
            float p = pitch;

            if (gyroActive && AttitudeSensor.current != null)
            {
                Vector3 e = (Quaternion.Inverse(gyroReference) * ReadAttitude()).eulerAngles;
                yaw += Mathf.Clamp(Mathf.DeltaAngle(0f, e.y) * gyroSensitivity, -gyroMaxYaw, gyroMaxYaw);
                float gp = Mathf.Clamp(Mathf.DeltaAngle(0f, e.x) * gyroSensitivity, -gyroMaxPitch, gyroMaxPitch);
                p += invertGyroPitch ? -gp : gp;
            }

            p = Mathf.Clamp(p, minPitch, maxPitch);
            Quaternion rot = Quaternion.Euler(p, yaw, 0f);
            Vector3 pivot = target.position + pivotOffset;
            Vector3 back = rot * Vector3.back;

            // Mur entre l'avatar et la camera : on rapproche la camera
            float wanted = distance;
            if (Physics.SphereCast(pivot, collisionRadius, back, out var hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
                wanted = Mathf.Max(minDistance, hit.distance);

            currentDistance = wanted < currentDistance
                ? wanted                                                     // on rentre tout de suite
                : Mathf.Lerp(currentDistance, wanted, 1f - Mathf.Exp(-6f * Time.deltaTime)); // on ressort en douceur

            transform.SetPositionAndRotation(pivot + back * currentDistance, rot);
        }

        void HandleDrag()
        {
            Vector2 delta = Vector2.zero;

            foreach (var t in Touch.activeTouches)
            {
                if (t.phase == TouchPhase.Began && dragTouchId < 0 && !IsOverUI(t.screenPosition))
                    dragTouchId = t.touchId;

                if (t.touchId != dragTouchId) continue;

                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    dragTouchId = -1;
                else
                    delta += t.delta;
            }

            // Souris (clic droit) pour l'editeur
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
                delta += mouse.delta.ReadValue();

            if (delta != Vector2.zero)
            {
                yawOffset += delta.x * dragSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * dragSensitivity, minPitch, maxPitch);
                lastDragTime = Time.time;
            }
            else if (dragTouchId < 0 && Time.time - lastDragTime > recenterDelay)
            {
                yawOffset = Mathf.MoveTowardsAngle(yawOffset, 0f, recenterSpeed * Time.deltaTime);
                pitch = Mathf.MoveTowards(pitch, defaultPitch, recenterSpeed * 0.5f * Time.deltaTime);
            }
        }

        bool IsOverUI(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            uiHits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = screenPos }, uiHits);
            return uiHits.Count > 0;
        }
    }
}
