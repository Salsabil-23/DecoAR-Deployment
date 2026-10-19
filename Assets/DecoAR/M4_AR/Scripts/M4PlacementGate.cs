using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

namespace DecoAR.AR
{
    public enum PlacementType { Sol, Mur, Surface }

    public class M4PlacementGate : MonoBehaviour
    {
        [Header("Porte-produits (copies de CubeVariant)")]
        [SerializeField] GameObject holderFloorPrefab;
        [SerializeField] GameObject holderWallPrefab;

        [Header("Produit (sera fourni par M3 plus tard)")]
        [SerializeField] GameObject productPrefab;
        [SerializeField] PlacementType placementType = PlacementType.Sol;
        [SerializeField] Vector3 sizeCm = new Vector3(50, 50, 50); // x=largeur, y=HAUTEUR, z=profondeur
        [SerializeField] bool flipFront = false;

        [Header("Ombre")]
        [SerializeField] Material shadowCatcherMaterial;

        [Header("Interface (messages en francais)")]
        [SerializeField] TMP_Text messageText;
        [SerializeField] GameObject resetButton;

        GameObject placed;
        ARPlaneManager planeManager;
        float tempMessageUntil;

        void Awake()
        {
            planeManager = FindFirstObjectByType<ARPlaneManager>();
            if (resetButton != null) resetButton.SetActive(false);
            if (messageText != null) messageText.text = "";
        }

        void Update()
        {
            if (messageText == null) return;
            if (Time.time < tempMessageUntil) return; // un message temporaire est affiche

            if (placed != null)
            {
                messageText.text = "";
                return;
            }

            if (HasMatchingPlane())
                messageText.text = "Touchez la surface pour poser le produit";
            else
                messageText.text = placementType == PlacementType.Mur
                    ? "Balayez lentement le mur"
                    : "Balayez lentement le sol";
        }

        bool HasMatchingPlane()
        {
            if (planeManager == null) return true;
            PlaneAlignment wanted = placementType == PlacementType.Mur
                ? PlaneAlignment.Vertical
                : PlaneAlignment.HorizontalUp;
            foreach (var plane in planeManager.trackables)
                if (plane.alignment == wanted) return true;
            return false;
        }

        public void ShowTemporary(string msg, float seconds = 2.5f)
        {
            if (messageText == null) return;
            messageText.text = msg;
            tempMessageUntil = Time.time + seconds;
        }

        // Appelee par le bouton "Taille réelle"
        public void ResetToRealSize()
        {
            if (placed == null) return;
            placed.transform.localScale = Vector3.one; // le modele enfant est deja a sa taille reelle
            ShowTemporary("Taille réelle");
        }

        // Appelee par l'evenement "Object Spawn Triggered (Vector3, Vector3)"
        public void OnSpawnTriggered(Vector3 position, Vector3 normal)
        {
            if (productPrefab == null) return;

            bool isWall = Mathf.Abs(normal.y) < 0.3f;
            bool isHorizontalUp = normal.y > 0.7f;
            bool valid = placementType == PlacementType.Mur ? isWall : isHorizontalUp;
            if (!valid)
            {
                Debug.Log($"[M4] Placement refuse : {placementType} sur une surface incompatible");
                ShowTemporary(placementType == PlacementType.Mur
                    ? "Ce produit se pose sur un mur"
                    : "Ce produit se pose sur le sol ou une table");
                return;
            }

            Transform cam = Camera.main.transform;
            Quaternion rot;
            if (placementType == PlacementType.Mur)
            {
                Vector3 n = new Vector3(normal.x, 0f, normal.z).normalized;
                if (Vector3.Dot(n, cam.position - position) < 0f) n = -n;
                if (flipFront) n = -n;
                rot = Quaternion.LookRotation(n, Vector3.up);
            }
            else
            {
                Vector3 toCam = cam.position - position;
                toCam.y = 0f;
                if (toCam.sqrMagnitude < 0.001f) toCam = Vector3.forward;
                rot = Quaternion.LookRotation(toCam.normalized, Vector3.up);
            }

            if (placed == null)
            {
                placed = BuildProduct();
                if (placed == null) return;
                if (resetButton != null) resetButton.SetActive(true);
            }
            placed.transform.SetPositionAndRotation(position, rot);

            M4Haptics.Medium();
        }

        GameObject BuildProduct()
        {
            GameObject holderPrefab = placementType == PlacementType.Mur ? holderWallPrefab : holderFloorPrefab;
            if (holderPrefab == null)
            {
                Debug.LogError("[M4] Porte-produit manquant dans M4_PlacementGate");
                return null;
            }

            // 1. Le produit, a l'origine, a sa taille reelle
            GameObject model = Instantiate(productPrefab, Vector3.zero, Quaternion.identity);
            M4Sizing.FitToRealSize(model, sizeCm);

            // Avertissement si les proportions du modele different du catalogue
            Vector3 got = M4Sizing.GetBounds(model).size * 100f;
            if (Mathf.Abs(got.x - sizeCm.x) > sizeCm.x * 0.15f || Mathf.Abs(got.z - sizeCm.z) > sizeCm.z * 0.15f)
                Debug.LogWarning($"[M4] Proportions differentes du catalogue : modele {got.x:F0}x{got.y:F0}x{got.z:F0} cm, catalogue {sizeCm.x:F0}x{sizeCm.y:F0}x{sizeCm.z:F0} cm");

            // 2. On cale le produit sur le pivot du porte-produit
            Bounds b = M4Sizing.GetBounds(model);
            Vector3 offset = placementType == PlacementType.Mur
                ? new Vector3(-b.center.x, -b.center.y, -b.min.z)   // dos du tableau contre le mur
                : new Vector3(-b.center.x, -b.min.y, -b.center.z);  // dessous du produit sur le sol
            model.transform.position += offset;
            b.center += offset;

            // 3. Porte-produit prepare sous un parent INACTIF
            GameObject staging = new GameObject("M4_Staging");
            staging.SetActive(false);

            GameObject holder = Instantiate(holderPrefab, staging.transform);
            holder.transform.localPosition = Vector3.zero;
            holder.transform.localRotation = Quaternion.identity;
            holder.transform.localScale = Vector3.one;

            foreach (var r in holder.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            foreach (var c in holder.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);

            // 4. Le modele devient enfant, avec un collider a sa taille
            model.transform.SetParent(holder.transform, true);
            var box = holder.AddComponent<BoxCollider>();
            box.center = b.center;
            box.size = Vector3.Max(b.size, Vector3.one * 0.05f);

            if (placementType != PlacementType.Mur) AddShadowCatcher(holder, b);

            // 5. On libere le porte-produit
            holder.transform.SetParent(null, true);
            Destroy(staging);

            Debug.Log($"[M4] Produit pret | taille = {b.size}");
            return holder;
        }

        void AddShadowCatcher(GameObject holder, Bounds b)
        {
            if (shadowCatcherMaterial == null) return;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "ShadowCatcher";
            DestroyImmediate(quad.GetComponent<Collider>());

            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = shadowCatcherMaterial;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = true;

            quad.transform.SetParent(holder.transform, false);
            quad.transform.localPosition = new Vector3(b.center.x, 0.002f, b.center.z);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float s = Mathf.Max(b.size.x, b.size.z) * 3f;
            quad.transform.localScale = new Vector3(s, s, 1f);
        }
    }
}