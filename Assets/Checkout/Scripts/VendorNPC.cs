using TMPro;
using UnityEngine;

namespace DecoAR.M5
{
    /// PNJ vendeur : accueil (Greet), attente (Idle), parole (Talking), confirmation (Confirm).
    public class VendorNPC : MonoBehaviour
    {
        public static VendorNPC Instance { get; private set; }

        public Animator animator;
        [Header("Détection joueur")]
        public float detectRadius = 2.5f;
        [Tooltip("En AR, la caméra = le joueur : détection par distance à Camera.main.")]
        public bool useCameraProximity = true;
        public float greetCooldown = 5f;
        public float turnSpeed = 4f;
        [Header("Bulle")]
        public bool showBubble = true;
        public string greetText = "Bonjour ! Prêt à passer commande ?";
        public string confirmText = "Merci pour votre commande !";
        public float bubbleHeight = 2.1f;
        public float bubbleDuration = 4f;

        static readonly int GreetH = Animator.StringToHash("Greet");
        static readonly int ConfirmH = Animator.StringToHash("Confirm");
        static readonly int TalkingH = Animator.StringToHash("Talking");

        Quaternion initialRot;
        Transform target;
        float lastGreet = -999f, bubbleHideAt;
        bool triggerInside, camInside;
        TextMeshPro bubble;

        void Awake()
        {
            Instance = this;
            if (!animator) animator = GetComponentInChildren<Animator>();
            initialRot = transform.rotation;
            if (showBubble) CreateBubble();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        static bool IsPlayer(Collider o) => o.CompareTag("Player") || o.GetComponentInParent<CharacterController>();

        void OnTriggerEnter(Collider o) { if (!IsPlayer(o)) return; triggerInside = true; OnPlayerEnter(o.transform); }
        void OnTriggerExit(Collider o) { if (!IsPlayer(o)) return; triggerInside = false; if (!camInside) OnPlayerExit(); }

        void Update()
        {
            var cam = Camera.main;
            if (useCameraProximity && !triggerInside && cam)
            {
                var d = cam.transform.position - transform.position; d.y = 0;
                bool inside = d.magnitude < detectRadius;
                if (inside && !camInside) { camInside = true; OnPlayerEnter(cam.transform); }
                else if (!inside && camInside) { camInside = false; OnPlayerExit(); }
            }

            var want = initialRot;
            if (target)
            {
                var dir = target.position - transform.position; dir.y = 0;
                if (dir.sqrMagnitude > 0.001f) want = Quaternion.LookRotation(dir);
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, want, Time.deltaTime * turnSpeed);

            if (bubble && bubble.gameObject.activeSelf)
            {
                if (Time.time > bubbleHideAt) bubble.gameObject.SetActive(false);
                else if (cam) bubble.transform.rotation = Quaternion.LookRotation(bubble.transform.position - cam.transform.position);
            }
        }

        void OnPlayerEnter(Transform player) { target = player; PlayGreet(); }

        void OnPlayerExit()
        {
            target = null;
            if (animator) animator.ResetTrigger(GreetH);
            SetTalking(false);
            if (bubble) bubble.gameObject.SetActive(false);
        }

        public void PlayGreet()
        {
            if (Time.time - lastGreet < greetCooldown) return;
            lastGreet = Time.time;
            if (animator) animator.SetTrigger(GreetH);
            ShowBubble(greetText);
        }

        public void PlayConfirm()
        {
            if (animator) animator.SetTrigger(ConfirmH);
            M5ShopAudio.Applause();
            ShowBubble(confirmText);
        }

        public void SetTalking(bool talking) { if (animator) animator.SetBool(TalkingH, talking); }

        public void ShowBubble(string text, float duration = -1f)
        {
            if (!bubble) return;
            bubble.text = text;
            bubble.gameObject.SetActive(true);
            bubbleHideAt = Time.time + (duration > 0 ? duration : bubbleDuration);
        }

        void CreateBubble()
        {
            var go = new GameObject("M5_Bubble");
            go.transform.SetParent(transform, false);
            float s = Mathf.Max(transform.lossyScale.x, 0.0001f);
            go.transform.localScale = Vector3.one / s;
            go.transform.localPosition = Vector3.up * (bubbleHeight / s);
            bubble = go.AddComponent<TextMeshPro>();
            bubble.rectTransform.sizeDelta = new Vector2(1.6f, 0.35f);
            bubble.alignment = TextAlignmentOptions.Center;
            bubble.enableAutoSizing = true; bubble.fontSizeMin = 0.1f; bubble.fontSizeMax = 3f;
            bubble.fontStyle = FontStyles.Bold;
            bubble.color = new Color(1f, 0.97f, 0.9f);
            go.SetActive(false);
        }
    }
}
