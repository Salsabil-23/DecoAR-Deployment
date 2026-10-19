using System;
using DecoAR.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DecoAR.Player
{
    // Deplacement de l'avatar : avancer, reculer, tourner (F1.4), animations (F1.3), vibration de collision (F1.8)
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [SerializeField] VirtualJoystick joystick;
        [SerializeField] Animator animator;

        [Header("Deplacement")]
        [SerializeField] float moveSpeed = 2.2f;
        [SerializeField] float backwardFactor = 0.6f;
        [SerializeField] float turnSpeed = 140f;   // degres / seconde
        [SerializeField] float gravity = -15f;

        [Header("Collision")]
        [SerializeField] float contactReleaseDelay = 0.4f; // sans contact pendant ce delai -> nouvelle vibration possible

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int InteractHash = Animator.StringToHash("Interact");

        CharacterController cc;
        float verticalVelocity;
        Collider currentContact;
        float lastContactTime;

        // M2 (mini-carte, entree de zone) lit la position ; d'autres modules peuvent bloquer les controles
        public Vector3 Position => transform.position;
        public bool InputLocked { get; set; }
        public event Action<Collider> ObstacleHit;

        void Awake()
        {
            Instance = this;
            cc = GetComponent<CharacterController>();

            // Retour de l'AR ou de la caisse : on reprend la ou l'avatar etait
            var gm = GameManager.Instance;
            if (gm != null && gm.TryGetShopPose(out var pos, out var rot))
                Teleport(pos, rot);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetAnimator(Animator a) => animator = a;

        // Utilise par M2 pour la teleportation depuis la mini-carte
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            cc.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            cc.enabled = true;
            verticalVelocity = 0f;
        }

        // A appeler avant de quitter la boutique (Voir en AR, caisse)
        public void SaveShopPose()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SaveShopPose(transform.position, transform.rotation);
        }

        public void PlayInteract()
        {
            if (animator != null) animator.SetTrigger(InteractHash);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector2 input = InputLocked ? Vector2.zero : ReadInput();

            transform.Rotate(0f, input.x * turnSpeed * dt, 0f);

            float forward = input.y < 0f ? input.y * backwardFactor : input.y;
            Vector3 move = transform.forward * (forward * moveSpeed);

            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * dt;
            move.y = verticalVelocity;
            cc.Move(move * dt);

            if (animator != null)
                animator.SetFloat(SpeedHash, Mathf.Max(Mathf.Abs(forward), Mathf.Abs(input.x) * 0.5f), 0.1f, dt);

            if (currentContact != null && Time.time - lastContactTime > contactReleaseDelay)
                currentContact = null;
        }

        Vector2 ReadInput()
        {
            Vector2 v = joystick != null ? joystick.Value : Vector2.zero;

            // Clavier pour tester dans l'editeur
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y = 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y = -1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x = -1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x = 1f;
            }
            return Vector2.ClampMagnitude(v, 1f);
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.5f) return; // le sol n'est pas un obstacle

            lastContactTime = Time.time;
            if (hit.collider == currentContact) return; // deja en contact : une seule vibration

            currentContact = hit.collider;
            HapticManager.Vibrate(HapticIntensity.Courte);
            ObstacleHit?.Invoke(hit.collider);
        }
    }
}
