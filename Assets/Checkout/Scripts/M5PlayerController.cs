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
    /// Joueur 1ère personne. PC : ZQSD/flèches + clic droit maintenu pour regarder. Mobile : moitié gauche = joystick, moitié droite = tourner,
    /// et gyroscope : on tourne le téléphone pour regarder autour de soi (comme en AR).
    [RequireComponent(typeof(CharacterController))]
    public class M5PlayerController : MonoBehaviour
    {
        public Transform cam;
        public float moveSpeed = 2f;
        public float mouseLook = 0.15f;
        public float touchLook = 0.15f;
        public float gravity = -9.81f;
        public float minPitch = -60f, maxPitch = 70f;
        [Header("Gyroscope (mobile)")]
        public bool useGyro = true;
        [Tooltip("Lissage de l'inclinaison quand seul l'accéléromètre est disponible (plus haut = plus réactif).")]
        public float gyroSmoothing = 20f;
        [Tooltip("Filtre adaptatif : fréquence de coupure au repos (Hz). Plus bas = plus stable, plus lent.")]
        public float gyroMinCutoff = 0.8f;
        [Tooltip("Réactivité ajoutée selon la vitesse de rotation (Hz par degré/s).")]
        public float gyroSpeedGain = 0.04f;
        [Tooltip("Tremblements ignorés en dessous de cet angle (degrés).")]
        public float gyroDeadzone = 0.25f;
        [Tooltip("Garde l'horizon droit (ignore l'inclinaison latérale du téléphone).")]
        public bool levelHorizon = true;
        [Header("Mobile")]
        public bool forceLandscape = true;
        public int targetFrameRate = 60;

        public bool JoystickActive => moveTouch >= 0;
        public Vector2 JoystickOrigin => moveStart;
        public Vector2 JoystickVector => moveVec;
        public float JoystickRadius => Screen.height * 0.1f;
        /// Téléphone uniquement : dans l'éditeur, la simulation XR du projet AR crée de faux capteurs.
        public bool GyroAvailable => Application.isMobilePlatform && !Application.isEditor && M5DeviceSensors.HasAny;
        public bool GyroActive => useGyro && GyroAvailable;

        CharacterController cc;
        float yaw, pitch, vy, yawOffset, gyroSpeed;
        bool gyroCalibrated;
        Quaternion gyroRot;
        int moveTouch = -1, lookTouch = -1;
        Vector2 moveStart, moveVec, lastLook;
        Coroutine focus;
        bool rotP, rotPU, rotL, rotR;
        static readonly List<RaycastResult> hits = new List<RaycastResult>();

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            if (useGyro && GyroAvailable) M5DeviceSensors.Enable();
        }

        void OnDisable() { if (GyroAvailable) M5DeviceSensors.Disable(); }

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (!cam && Camera.main) cam = Camera.main.transform;
            yaw = transform.eulerAngles.y;
            if (cam) { pitch = cam.localEulerAngles.x; if (pitch > 180f) pitch -= 360f; }
            if (Application.isMobilePlatform)
            {
                Application.targetFrameRate = targetFrameRate;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                if (forceLandscape)
                {
                    rotP = Screen.autorotateToPortrait; rotPU = Screen.autorotateToPortraitUpsideDown;
                    rotL = Screen.autorotateToLandscapeLeft; rotR = Screen.autorotateToLandscapeRight;
                    Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;
                    Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = true;
                    // Paysage immédiat, même si le téléphone est tenu en portrait ; puis rotation libre entre les deux paysages
                    Screen.orientation = Screen.orientation == ScreenOrientation.LandscapeRight ? ScreenOrientation.LandscapeRight : ScreenOrientation.LandscapeLeft;
                    StartCoroutine(AllowLandscapeRotation());
                }
            }
        }

        IEnumerator AllowLandscapeRotation()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        void OnDestroy()
        {
            if (!Application.isMobilePlatform || !forceLandscape) return;
            Screen.autorotateToPortrait = rotP; Screen.autorotateToPortraitUpsideDown = rotPU;
            Screen.autorotateToLandscapeLeft = rotL; Screen.autorotateToLandscapeRight = rotR;
        }

        /// Active / coupe le gyroscope (bouton du HUD).
        public void SetGyro(bool on)
        {
            useGyro = on;
            gyroCalibrated = false;
            if (!GyroAvailable) return;
            if (on) M5DeviceSensors.Enable(); else M5DeviceSensors.Disable();
            if (DecoAR.Core.GameManager.Instance != null) DecoAR.Core.GameManager.Instance.SetGyroEnabled(on);
        }

        /// Recale le gyroscope : la direction actuelle redevient « devant ».
        public void Recenter() => gyroCalibrated = false;

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

            if (GyroActive && M5DeviceSensors.TryGetCameraRotation(out var att))
            {
                // Gyroscope : le téléphone donne l'orientation, le glissé horizontal ajoute un décalage de cap
                if (!gyroCalibrated) { yawOffset = yaw - Heading(att); gyroRot = Quaternion.Euler(0, yawOffset, 0) * att; gyroCalibrated = true; }
                yawOffset += look.x;
                var target = Quaternion.Euler(0, yawOffset, 0) * att;
                if (levelHorizon) target = Level(target);
                gyroRot = Quaternion.Slerp(gyroRot, target, GyroFilter(Quaternion.Angle(gyroRot, target)));
                ApplyWorldRotation(gyroRot);
            }
            else
            {
                yaw += look.x;
                if (GyroActive && M5DeviceSensors.TryGetGravityPitch(out float gp))
                    pitch = Mathf.Lerp(pitch, Mathf.Clamp(gp, minPitch, maxPitch), 1f - Mathf.Exp(-gyroSmoothing * 0.5f * Time.deltaTime));
                else
                    pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
                ApplyRotation();
            }

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

        /// Le corps suit le cap de la caméra (pour avancer dans la direction regardée), la caméra garde inclinaison et roulis.
        void ApplyWorldRotation(Quaternion world)
        {
            yaw = Heading(world);
            var f = world * Vector3.forward;
            pitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f)) * Mathf.Rad2Deg, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            if (cam) cam.localRotation = Quaternion.Inverse(transform.rotation) * world;
        }

        /// Filtre adaptatif (type « 1€ ») : très lissé quand le téléphone est presque immobile (tremblements),
        /// réactif quand on tourne vite. Retourne la part du chemin à parcourir cette image.
        float GyroFilter(float angle)
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * 4f * dt);
            gyroSpeed = Mathf.Lerp(gyroSpeed, angle / dt, a); // vitesse lissée (deg/s)
            float cutoff = gyroMinCutoff + gyroSpeedGain * gyroSpeed;
            float k = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff * dt);
            if (angle < gyroDeadzone) k *= angle / gyroDeadzone * 0.3f; // micro-mouvements quasi ignorés
            return k;
        }

        /// Même direction de regard, sans roulis.
        static Quaternion Level(Quaternion q)
        {
            var f = q * Vector3.forward;
            if (Mathf.Abs(f.y) > 0.98f) return q; // regard vertical : roulis indéfini, on garde tel quel
            return Quaternion.LookRotation(f, Vector3.up);
        }

        static float Heading(Quaternion q)
        {
            var f = q * Vector3.forward; f.y = 0;
            if (f.sqrMagnitude < 0.01f) { f = q * Vector3.up; f.y = 0; } // regard vertical : on prend le haut de l'écran
            return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
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
            float y0 = yaw, p0 = pitch, o0 = yawOffset, turn = Mathf.DeltaAngle(yaw, ty);
            for (float e = 0; e < duration; e += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, e / duration);
                if (GyroActive && gyroCalibrated) yawOffset = o0 + turn * k; // avec le gyroscope seul le cap est guidé
                else { yaw = Mathf.LerpAngle(y0, ty, k); pitch = Mathf.Lerp(p0, tp, k); ApplyRotation(); }
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
