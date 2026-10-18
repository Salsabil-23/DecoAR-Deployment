using UnityEngine;

namespace DecoAR.AR
{
    public enum PlacementType { Sol, Mur, Surface }

    public class M4PlacementGate : MonoBehaviour
    {
        [Header("Test (sera fourni par M3 plus tard)")]
        [SerializeField] GameObject productPrefab;
        [SerializeField] PlacementType placementType = PlacementType.Sol;
        [SerializeField] Vector3 sizeCm = new Vector3(50, 50, 50); // x=largeur, y=HAUTEUR, z=profondeur
        [SerializeField] bool flipFront = false;

        GameObject placed;

        public void OnSpawnTriggered(Vector3 position, Vector3 normal)
        {
            Debug.Log($"[M4] OnSpawnTriggered appele | type={placementType} | prefab={(productPrefab != null ? productPrefab.name : "VIDE")} | sizeCm={sizeCm}");

            if (productPrefab == null) return;

            bool isWall = Mathf.Abs(normal.y) < 0.3f;
            bool isHorizontalUp = normal.y > 0.7f;
            bool valid = placementType == PlacementType.Mur ? isWall : isHorizontalUp;
            if (!valid)
            {
                Debug.Log($"[M4] Placement refuse : {placementType} sur une surface incompatible");
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
                placed = Instantiate(productPrefab, position, Quaternion.identity);
                M4Sizing.FitToRealSize(placed, sizeCm);
            }

            placed.transform.SetPositionAndRotation(position, Quaternion.identity);

            if (placementType != PlacementType.Mur)
            {
                float lift = position.y - M4Sizing.GetBounds(placed).min.y;
                placed.transform.position = position + Vector3.up * lift;
            }

            placed.transform.rotation = rot;
        }
    }
}