using UnityEngine;

namespace DecoAR.M3
{
    // Produit pose sur l'etagere ; garde sa place pour y revenir apres l'inspection
    public class M3ShelfItem : MonoBehaviour
    {
        public ProductOption Product { get; private set; }
        public GameObject Model { get; private set; }
        public Vector3 LocalCenter { get; private set; }   // centre du modele, repere de l'objet, sans echelle
        public Vector3 LocalSize { get; private set; }

        public Transform HomeParent { get; private set; }
        public Vector3 HomeLocalPosition { get; private set; }
        public Quaternion HomeLocalRotation { get; private set; }
        public Vector3 HomeLocalScale { get; private set; }

        GameObject marker;
        bool highlighted;

        public void Init(ProductOption product, GameObject model, Bounds localBounds, GameObject highlightMarker)
        {
            Product = product;
            Model = model;
            LocalCenter = localBounds.center;
            LocalSize = localBounds.size;
            marker = highlightMarker;
            HomeParent = transform.parent;
            HomeLocalPosition = transform.localPosition;
            HomeLocalRotation = transform.localRotation;
            HomeLocalScale = transform.localScale;
        }

        public void SetHighlighted(bool on)
        {
            highlighted = on;
            if (marker != null) marker.SetActive(on);
        }

        void Update()
        {
            // Leger rebond des produits trouves par la recherche, tant qu'ils sont sur l'etagere
            if (!highlighted || transform.parent != HomeParent) return;
            float bob = (Mathf.Sin(Time.time * 4f) + 1f) * 0.01f;
            transform.localPosition = HomeLocalPosition + Vector3.up * bob;
        }

        public void StopBobbing()
        {
            if (transform.parent == HomeParent) transform.localPosition = HomeLocalPosition;
        }
    }
}
