using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DecoAR.M5
{
    /// Colis kraft créé par code : rabats qui se ferment, scotch, rebond, puis envol. Appeler Play() après confirmation.
    public class ParcelAnimation : MonoBehaviour
    {
        public Material kraftMaterial;
        public Material tapeMaterial;
        public Vector3 size = new Vector3(0.3f, 0.2f, 0.25f);
        public float flapOpenAngle = 110f;
        public float closeDuration = 0.5f;
        [Tooltip("Direction locale de départ du colis.")]
        public Vector3 flyDirection = new Vector3(0f, 0.6f, -1f);
        public float flyDistance = 3f;
        public float flyDuration = 0.9f;
        public bool clearBasket = true;
        public bool rebuildAfter = true;
        [Tooltip("Modèle de colis déjà posé sur le comptoir (ex. CartTon). Vide = colis généré par code.")]
        public Transform existingParcel;
        [Header("Feedback")]
        public AudioSource audioSource;
        public AudioClip confirmClip;
        public VendorNPC vendor;
        public UnityEvent onConfirmed;
        public UnityEvent onFinished;

        Transform parcel, flapL, flapR, tape;
        bool playing;
        Vector3 oPos, oScale; Quaternion oRot;
        public bool IsPlaying => playing;

        void Awake()
        {
            if (!audioSource) audioSource = GetComponent<AudioSource>();
            if (existingParcel) { oPos = existingParcel.position; oRot = existingParcel.rotation; oScale = existingParcel.localScale; }
            Build();
        }

        public void Build()
        {
            if (existingParcel)
            {
                parcel = existingParcel;
                parcel.SetPositionAndRotation(oPos, oRot);
                parcel.localScale = oScale;
                parcel.gameObject.SetActive(true);
                return;
            }
            if (parcel) Destroy(parcel.gameObject);
            parcel = new GameObject("M5_ParcelBox").transform;
            parcel.SetParent(transform, false);
            Cube("Corps", parcel, new Vector3(0, size.y / 2f, 0), size, kraftMaterial);

            flapL = new GameObject("Rabat_G").transform;
            flapL.SetParent(parcel, false);
            flapL.localPosition = new Vector3(-size.x / 2f, size.y, 0);
            Cube("Carton", flapL, new Vector3(size.x / 4f, 0.003f, 0), new Vector3(size.x / 2f, 0.006f, size.z), kraftMaterial);
            flapL.localRotation = Quaternion.Euler(0, 0, flapOpenAngle);

            flapR = new GameObject("Rabat_D").transform;
            flapR.SetParent(parcel, false);
            flapR.localPosition = new Vector3(size.x / 2f, size.y, 0);
            Cube("Carton", flapR, new Vector3(-size.x / 4f, 0.003f, 0), new Vector3(size.x / 2f, 0.006f, size.z), kraftMaterial);
            flapR.localRotation = Quaternion.Euler(0, 0, -flapOpenAngle);

            tape = Cube("Scotch", parcel, new Vector3(0, size.y + 0.008f, 0), new Vector3(0.06f, 0.004f, size.z + 0.01f), tapeMaterial).transform;
            tape.gameObject.SetActive(false);
        }

        static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            if (m) g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }

        [ContextMenu("Test : Play")]
        public void Play() { if (!playing) StartCoroutine(PlayRoutine()); }

        IEnumerator PlayRoutine()
        {
            playing = true;
            if (!parcel) Build();

            var baseScale = parcel.localScale;
            if (!existingParcel) {
            // 1. Rabats qui se ferment (le droit légèrement décalé)
            float total = closeDuration * 1.3f;
            for (float e = 0; e < total; e += Time.deltaTime)
            {
                float kl = Ease(Mathf.Clamp01(e / closeDuration));
                float kr = Ease(Mathf.Clamp01((e - closeDuration * 0.3f) / closeDuration));
                flapL.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(flapOpenAngle, 0, kl));
                flapR.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-flapOpenAngle, 0, kr));
                yield return null;
            }
            flapL.localRotation = flapR.localRotation = Quaternion.identity;

            // 2. Scotch
            tape.gameObject.SetActive(true);
            var tapeScale = tape.localScale;
            for (float e = 0; e < 0.25f; e += Time.deltaTime)
            {
                tape.localScale = new Vector3(tapeScale.x, tapeScale.y, tapeScale.z * Ease(e / 0.25f));
                yield return null;
            }
            tape.localScale = tapeScale;
            }

            // 3. Feedback : vendeur, vibration forte, son
            var npc = vendor ? vendor : VendorNPC.Instance;
            if (npc) npc.PlayConfirm();
            M5Haptics.Strong();
            if (audioSource && confirmClip) audioSource.PlayOneShot(confirmClip);
            onConfirmed?.Invoke();

            // 4. Rebond
            for (float e = 0; e < 0.3f; e += Time.deltaTime)
            {
                float s = 1f + Mathf.Sin(e / 0.3f * Mathf.PI) * 0.15f;
                parcel.localScale = Vector3.Scale(baseScale, new Vector3(s, 2f - s, s));
                yield return null;
            }
            parcel.localScale = baseScale;
            yield return new WaitForSeconds(0.25f);

            // 5. Envol hors champ
            if (clearBasket && Basket3D.Instance) Basket3D.Instance.Clear();
            var start = parcel.position;
            var dir = transform.TransformDirection(flyDirection.normalized);
            var startRot = parcel.rotation;
            for (float e = 0; e < flyDuration; e += Time.deltaTime)
            {
                float k = Mathf.Clamp01(e / flyDuration);
                parcel.position = start + dir * (flyDistance * k * k);
                parcel.rotation = startRot * Quaternion.Euler(0, 0, -20f * k);
                parcel.localScale = baseScale * Mathf.Lerp(1f, 0.3f, k);
                yield return null;
            }
            if (existingParcel) parcel.gameObject.SetActive(false); else Destroy(parcel.gameObject);
            parcel = null;
            onFinished?.Invoke();

            if (rebuildAfter) { yield return new WaitForSeconds(1f); Build(); }
            playing = false;
        }

        static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
    }
}
