using System;
using System.Collections.Generic;
using UnityEngine;

namespace DecoAR.M3
{
    [Serializable]
    public class ProductOption
    {
        public string id = "product_01";
        public string displayName = "Produit";
        public string category = "Décoration";
        public float price = 49f;
        public Vector3 sizeCm = new Vector3(40f, 40f, 40f);
        public GameObject prefab;
        public string description = "Produit de décoration";
        public Sprite thumbnail;
    }

    [CreateAssetMenu(fileName = "M3_ProductCatalog", menuName = "DecoAR/M3/Product Catalog")]
    public class M3ProductCatalog : ScriptableObject
    {
        public List<ProductOption> products = new List<ProductOption>();

        public ProductOption Get(string productId)
        {
            if (products == null) return null;
            foreach (var product in products)
            {
                if (product != null && product.id == productId)
                    return product;
            }
            return null;
        }
    }
}
