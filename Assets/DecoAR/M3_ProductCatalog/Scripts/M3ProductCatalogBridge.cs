using DecoAR.AR;
using UnityEngine;

namespace DecoAR.M3
{
    public class M3ProductCatalogBridge : MonoBehaviour
    {
        [SerializeField] private M3ProductCatalog catalog;
        [SerializeField] private M4PlacementGate placementGate;

        // Appel manuel depuis un bouton ou un évènement Unity.
        public void ApplySelectedProduct(string productId)
        {
            if (catalog == null) return;

            var product = catalog.Get(productId);
            if (product == null || product.prefab == null) return;

            var gm = DecoAR.Core.GameManager.Instance;
            if (gm != null)
                gm.SelectProduct(product.id, product.prefab, product.sizeCm);

            if (placementGate != null)
                placementGate.SetProductPrefab(product.prefab, product.sizeCm);
        }
    }
}
