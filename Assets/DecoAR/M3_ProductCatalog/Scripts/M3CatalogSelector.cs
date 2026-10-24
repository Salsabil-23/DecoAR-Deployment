using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DecoAR.M3
{
    public class M3CatalogSelector : MonoBehaviour
    {
        [SerializeField] private M3ProductCatalog catalog;
        [SerializeField] private Transform productRoot;
        [SerializeField] private GameObject productButtonPrefab;
        [SerializeField] private TMP_Text selectedLabel;

        private readonly List<GameObject> spawnedButtons = new List<GameObject>();

        private void Start()
        {
            BuildCatalog();
        }

        public void SelectProduct(ProductOption product)
        {
            if (product == null) return;
            if (selectedLabel != null)
                selectedLabel.text = product.displayName + " • " + product.category;

            var gm = DecoAR.Core.GameManager.Instance;
            if (gm != null)
                gm.SelectProduct(product.id, product.prefab, product.sizeCm);
        }

        private void BuildCatalog()
        {
            if (catalog == null || productRoot == null || productButtonPrefab == null)
                return;

            foreach (var button in spawnedButtons)
                if (button != null)
                    Destroy(button);
            spawnedButtons.Clear();

            foreach (var product in catalog.products)
            {
                if (product == null) continue;

                var button = Instantiate(productButtonPrefab, productRoot);
                spawnedButtons.Add(button);

                var text = button.GetComponentInChildren<TMP_Text>();
                if (text != null)
                    text.text = product.displayName + "\n" + product.price + " TND";

                var btn = button.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => SelectProduct(product));
            }
        }
    }
}
