using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DecoAR.Player;

namespace DecoAR.M5
{
    /// Vue 1ère / 3ème personne. Le personnage créé dans M1 (même modèle, mêmes couleurs, même visage) est posé
    /// sur le joueur ; en 3ème personne la caméra recule derrière lui sans traverser les murs. Les contrôles
    /// (joystick, glisser, gyroscope) restent ceux de M5PlayerController. Le choix est mémorisé.
    [RequireComponent(typeof(M5PlayerController))]
    public class M5ThirdPerson : MonoBehaviour
    {
        [Tooltip("Profil du personnage de M1 (Assets/DecoAR/M1_Player/Data/M1_CharacterProfile).")]
        public CharacterProfile profile;
        public float distance = 2.6f;
        public float pivotHeight = 1.5f;
        [Tooltip("Décalage vers la droite : le personnage est un peu à gauche de l'écran, la vue reste dégagée.")]
        public float shoulder = 0.35f;
        public float collisionRadius = 0.2f;
        public float blendTime = 0.35f;

        const string PrefKey = "DecoAR.M5.VueTroisiemePersonne";
        static readonly int SpeedHash = Animator.StringToHash("Speed");

        public bool ThirdPerson { get; private set; }
        public bool HasAvatar => avatar != null;

        M5PlayerController player;
        CharacterController cc;
        Transform cam;
        Vector3 eyeLocal;
        float blend, currentDistance;
        GameObject avatar;
        Animator animator;
        Renderer[] renderers = new Renderer[0];
        Behaviour[] decals = new Behaviour[0];
        bool avatarShown = true;
        readonly RaycastHit[] hits = new RaycastHit[16];

        void Awake()
        {
            player = GetComponent<M5PlayerController>();
            cc = GetComponent<CharacterController>();
            ThirdPerson = PlayerPrefs.GetInt(PrefKey, 0) == 1;
            blend = ThirdPerson ? 1f : 0f;
        }

        void Start()
        {
            cam = player.cam;
            if (cam) eyeLocal = cam.localPosition;
            currentDistance = distance;
            SpawnAvatar();
            if (!HasAvatar) { ThirdPerson = false; blend = 0f; }
            ShowAvatar(blend > 0.5f, force: true);
        }

        void SpawnAvatar()
        {
            if (profile == null) { Debug.LogWarning("[M5] Profil du personnage M1 non assigné : pas de vue 3ème personne"); return; }
            var gm = DecoAR.Core.GameManager.Instance;
            var mount = new GameObject("M5_Personnage").transform;
            mount.SetParent(transform, false);
            mount.localPosition = cc.center + Vector3.down * (cc.height * 0.5f); // pieds au bas du CharacterController
            animator = CharacterAppearance.Spawn(profile, gm != null ? gm.Look : new DecoAR.Core.AvatarLook(), mount);
            if (animator == null) { Destroy(mount.gameObject); return; }
            avatar = mount.gameObject;
            foreach (var c in avatar.GetComponentsInChildren<Collider>(true)) Destroy(c); // ne gêne ni le joueur ni la caméra
            renderers = avatar.GetComponentsInChildren<Renderer>(true);
            var list = new List<Behaviour>();
            foreach (var b in avatar.GetComponentsInChildren<Behaviour>(true))
                if (b.GetType().Name == "DecalProjector") list.Add(b); // visage photo projeté (M1)
            decals = list.ToArray();
        }

        /// Bouton du HUD : passe d'une vue à l'autre. Retourne la nouvelle vue (vrai = 3ème personne).
        public bool Toggle()
        {
            if (!HasAvatar) return false;
            ThirdPerson = !ThirdPerson;
            PlayerPrefs.SetInt(PrefKey, ThirdPerson ? 1 : 0);
            PlayerPrefs.Save();
            return ThirdPerson;
        }

        void Update()
        {
            if (animator == null) return;
            var v = cc.velocity; v.y = 0f;
            animator.SetFloat(SpeedHash, Mathf.Clamp01(v.magnitude / Mathf.Max(player.moveSpeed, 0.01f)), 0.1f, Time.deltaTime);
        }

        // Après M5PlayerController (qui oriente la caméra) : on ne fait que la placer
        void LateUpdate()
        {
            if (!cam) return;
            blend = Mathf.MoveTowards(blend, ThirdPerson ? 1f : 0f, Time.deltaTime / Mathf.Max(blendTime, 0.01f));
            float k = Mathf.SmoothStep(0f, 1f, blend);
            var eye = transform.TransformPoint(eyeLocal);
            if (k <= 0f) { cam.position = eye; ShowAvatar(false); return; }

            var pivot = transform.position + Vector3.up * pivotHeight + cam.right * shoulder;
            var back = -cam.forward;
            float wanted = distance;
            int n = Physics.SphereCastNonAlloc(pivot, collisionRadius, back, hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform)) continue;
                if (hits[i].distance > 0f) wanted = Mathf.Min(wanted, hits[i].distance);
            }
            // Rapproche tout de suite (mur), s'éloigne en douceur
            currentDistance = wanted < currentDistance ? wanted : Mathf.MoveTowards(currentDistance, wanted, Time.deltaTime * 3f);
            cam.position = Vector3.Lerp(eye, pivot + back * currentDistance, k);
            // Trop près (dos au mur) : on masque le personnage pour ne pas voir l'intérieur du modèle
            ShowAvatar(k > 0.5f && currentDistance > 0.55f);
        }

        void ShowAvatar(bool show, bool force = false)
        {
            if (show == avatarShown && !force) return;
            avatarShown = show;
            // 1ère personne : seule l'ombre du personnage reste visible
            foreach (var r in renderers) if (r) r.shadowCastingMode = show ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
            foreach (var d in decals) if (d) d.enabled = show;
        }
    }
}
