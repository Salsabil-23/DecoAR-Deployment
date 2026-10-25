using System.Collections.Generic;
using UnityEngine;

namespace DecoAR.M3
{
    // Etagere interactive d'une zone (F3.1) : remplie a partir des produits de la categorie.
    // Les modeles sont reduits pour tenir dans un emplacement ; la taille reelle n'est utilisee qu'en AR.
    public class M3Shelf : MonoBehaviour
    {
        [SerializeField] Transform[] slots;               // emplacements sur les planches (pivot = dessus de planche)
        [SerializeField] float slotSize = 0.32f;          // cote max d'un produit sur l'etagere (m)
        [SerializeField] Color highlightColor = new Color(1f, 0.78f, 0.3f);

        readonly List<M3ShelfItem> items = new List<M3ShelfItem>();
        public IReadOnlyList<M3ShelfItem> Items => items;

        public void Fill(List<ProductOption> products)
        {
            Clear();
            if (slots == null || slots.Length == 0)
            {
                Debug.LogError("[M3] Aucun emplacement sur l'etagere");
                return;
            }
            if (products.Count > slots.Length)
                Debug.LogWarning($"[M3] {products.Count} produits pour {slots.Length} emplacements : les derniers ne sont pas affiches");

            for (int i = 0; i < products.Count && i < slots.Length; i++)
            {
                var product = products[i];
                if (product == null || product.prefab == null) continue;
                items.Add(Spawn(product, slots[i]));
            }
            Debug.Log($"[M3] Etagere remplie : {items.Count} produits");
        }

        public void Clear()
        {
            foreach (var item in items)
                if (item != null) Destroy(item.gameObject);
            items.Clear();
        }

        public M3ShelfItem Find(ProductOption product)
        {
            foreach (var item in items)
                if (item.Product == product) return item;
            return null;
        }

        // Recherche vocale / texte : met en evidence les produits trouves (F3.7)
        public int Highlight(ICollection<ProductOption> products)
        {
            int count = 0;
            foreach (var item in items)
            {
                bool on = products != null && products.Contains(item.Product);
                item.SetHighlighted(on);
                if (on) count++;
            }
            return count;
        }

        M3ShelfItem Spawn(ProductOption product, Transform slot)
        {
            var root = new GameObject("Shelf_" + product.id);
            root.transform.SetParent(slot, false);

            var model = Instantiate(product.prefab, root.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) Destroy(c);

            // Mise a l'echelle pour tenir dans l'emplacement, pose sur la planche
            var b = LocalBounds(root.transform, model);
            float biggest = Mathf.Max(b.size.x, b.size.y, b.size.z);
            float factor = biggest > 0.0001f ? Mathf.Min(1f, slotSize / biggest) : 1f;
            model.transform.localScale *= factor;
            b = LocalBounds(root.transform, model);
            model.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
            b.center = new Vector3(0f, b.size.y * 0.5f, 0f);

            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, b.size.y * 0.5f, 0f);
            box.size = Vector3.Max(b.size, Vector3.one * 0.08f);

            var item = root.AddComponent<M3ShelfItem>();
            item.Init(product, model, b, CreateMarker(root.transform, b.size));
            return item;
        }

        GameObject CreateMarker(Transform parent, Vector3 size)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Highlight";
            Destroy(marker.GetComponent<Collider>());
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(0f, 0.004f, 0f);
            marker.transform.localScale = new Vector3(Mathf.Max(size.x, 0.1f) + 0.04f, 0.008f, Mathf.Max(size.z, 0.1f) + 0.04f);
            var r = marker.GetComponent<Renderer>();
            var mat = new Material(r.sharedMaterial);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", highlightColor);
            mat.color = highlightColor;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", highlightColor * 1.5f);
            }
            r.sharedMaterial = mat;
            marker.SetActive(false);
            return marker;
        }

        static Bounds LocalBounds(Transform space, GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
            bool first = true;
            var b = new Bounds();
            foreach (var r in renderers)
            {
                var wb = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = wb.center + Vector3.Scale(wb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var local = space.InverseTransformPoint(corner);
                    if (first) { b = new Bounds(local, Vector3.zero); first = false; }
                    else b.Encapsulate(local);
                }
            }
            return b;
        }
    }
}
