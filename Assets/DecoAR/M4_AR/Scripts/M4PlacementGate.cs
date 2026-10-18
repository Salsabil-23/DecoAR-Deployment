using UnityEngine;

namespace DecoAR.AR
{
    public enum PlacementType { Sol, Mur, Surface }

    public class M4PlacementGate : MonoBehaviour
    {
        [Header("Test (sera fourni par M3 plus tard)")]
        [SerializeField] GameObject productPrefab;
        [SerializeField] PlacementType placementType = PlacementType.Sol;
        [Tooltip("Cocher si le tableau apparait a l'envers (face tournee vers le mur)")]
        [SerializeField] bool flipFront = false;

        GameObject placed;

        // Appelee par l'evenement "Object Spawn Triggered (Vector3, Vector3)"
        public void OnSpawnTriggered(Vector3 position, Vector3 normal)
        {
            bool isWall = Mathf.Abs(normal.y) < 0.3f;
            bool isHorizontalUp = normal.y > 0.7f;

            bool valid = placementType == PlacementType.Mur ? isWall : isHorizontalUp;
            if (!valid)
            {
                Debug.Log($"[M4] Placement refuse : {placementType} sur une surface incompatible");
                // TODO : afficher un message a l'ecran (ex. "Ce produit se pose sur un mur")
                return;
            }

            Transform cam = Camera.main.transform;
            Quaternion rot;

            if (placementType == PlacementType.Mur)
            {
                // Face du produit tournee vers la piece
                Vector3 n = new Vector3(normal.x, 0f, normal.z).normalized;
                if (Vector3.Dot(n, cam.position - position) < 0f) n = -n;
                if (flipFront) n = -n;
                rot = Quaternion.LookRotation(n, Vector3.up);
            }
            else
            {
                // Produit tourne vers l'utilisateur, reste droit
                Vector3 toCam = cam.position - position;
                toCam.y = 0f;
                if (toCam.sqrMagnitude < 0.001f) toCam = Vector3.forward;
                rot = Quaternion.LookRotation(toCam.normalized, Vector3.up);
            }

            if (placed == null)
                placed = Instantiate(productPrefab, position, rot);
            else
                placed.transform.SetPositionAndRotation(position, rot);

            // TODO F4.9 : HapticManager.Instance.Medium();
        }
    }
}