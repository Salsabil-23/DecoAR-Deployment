using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DecoAR.M5
{
    /// Panier 3D sur le comptoir : les articles ajoutés y tombent sous forme de mini-boîtes / modèles réduits.
    public class Basket3D : MonoBehaviour
    {
        public static Basket3D Instance { get; private set; }

        public Material itemMaterial;
        public float itemSize = 0.15f;
        public float dropHeight = 0.45f;
        public float dropDuration = 0.45f;
        public bool showCounter = true;
        public float counterHeight = 0.15f;

        public event System.Action<int> OnCountChanged;
        public int Count => items.Count;

        readonly List<GameObject> items = new List<GameObject>();
        TextMeshPro label;

        void Awake()
        {
            Instance = this;
            if (showCounter) CreateLabel();
            UpdateLabel();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        Bounds GetBounds()
        {
            var rs = GetComponentsInChildren<Renderer>();
            Bounds b = new Bounds(transform.position, Vector3.one * 0.3f); bool first = true;
            foreach (var r in rs)
            {
                if (IsItem(r.transform)) continue;
                if (r.GetComponent<TMP_Text>()) continue;
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
            return b;
        }

        bool IsItem(Transform t) { foreach (var i in items) if (i && t.IsChildOf(i.transform)) return true; return false; }

        public GameObject AddItem(GameObject prefabOrNull, Color color)
        {
            var b = GetBounds();
            GameObject item;
            if (prefabOrNull)
            {
                item = Instantiate(prefabOrNull);
                foreach (var c in item.GetComponentsInChildren<Collider>()) Destroy(c);
                foreach (var rb in item.GetComponentsInChildren<Rigidbody>()) Destroy(rb);
                var ib = new Bounds(item.transform.position, Vector3.zero); bool f = true;
                foreach (var r in item.GetComponentsInChildren<Renderer>()) { if (f) { ib = r.bounds; f = false; } else ib.Encapsulate(r.bounds); }
                float max = Mathf.Max(ib.size.x, ib.size.y, ib.size.z);
                if (max > 0.0001f) item.transform.localScale *= itemSize / max;
            }
            else
            {
                item = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(item.GetComponent<Collider>());
                item.transform.localScale = new Vector3(itemSize, itemSize * 0.75f, itemSize * 0.85f);
                var mr = item.GetComponent<Renderer>();
                if (itemMaterial) mr.sharedMaterial = itemMaterial;
                mr.material.color = color;
                if (mr.material.HasProperty("_BaseColor")) mr.material.SetColor("_BaseColor", color);
            }
            item.name = "M5_Article_" + items.Count;

            int layer = items.Count / 4;
            Vector2 rnd = Random.insideUnitCircle * 0.3f;
            var target = new Vector3(b.center.x + rnd.x * b.extents.x, 0, b.center.z + rnd.y * b.extents.z);
            target.y = Mathf.Min(b.min.y + b.size.y * 0.3f + layer * itemSize * 0.6f + itemSize * 0.4f, b.max.y + itemSize * 0.3f);
            item.transform.SetParent(transform, true);
            item.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            items.Add(item);
            StartCoroutine(Drop(item.transform, target));
            UpdateLabel();
            OnCountChanged?.Invoke(items.Count);
            return item;
        }

        IEnumerator Drop(Transform t, Vector3 target)
        {
            var start = target + Vector3.up * dropHeight;
            var baseScale = t.localScale;
            float e = 0;
            while (e < dropDuration && t)
            {
                e += Time.deltaTime;
                float k = Mathf.Clamp01(e / dropDuration);
                t.position = Vector3.LerpUnclamped(start, target, k * k);
                yield return null;
            }
            if (!t) yield break;
            t.position = target;
            e = 0;
            while (e < 0.2f && t)
            {
                e += Time.deltaTime;
                float s = Mathf.Sin(Mathf.Clamp01(e / 0.2f) * Mathf.PI) * 0.18f;
                t.localScale = Vector3.Scale(baseScale, new Vector3(1 + s, 1 - s, 1 + s));
                yield return null;
            }
            if (t) t.localScale = baseScale;
        }

        /// Retire un article (rendu par AddItem) avec une petite animation de disparition.
        public void RemoveItem(GameObject item)
        {
            if (!items.Remove(item)) return;
            if (item) StartCoroutine(Shrink(item));
            UpdateLabel();
            OnCountChanged?.Invoke(items.Count);
        }

        IEnumerator Shrink(GameObject g)
        {
            var t = g.transform; var s = t.localScale;
            for (float e = 0; e < 0.18f && t; e += Time.deltaTime) { t.localScale = s * (1f - e / 0.18f); yield return null; }
            if (g) Destroy(g);
        }

        public void Clear()
        {
            foreach (var i in items) if (i) Destroy(i);
            items.Clear();
            UpdateLabel();
            OnCountChanged?.Invoke(0);
        }

        void CreateLabel()
        {
            var go = new GameObject("M5_BasketLabel");
            go.transform.SetParent(transform, false);
            var b = GetBounds();
            go.transform.position = new Vector3(b.center.x, b.max.y + counterHeight + 0.05f, b.center.z);
            float s = Mathf.Max(transform.lossyScale.x, 0.0001f);
            go.transform.localScale = Vector3.one / s;
            label = go.AddComponent<TextMeshPro>();
            label.rectTransform.sizeDelta = new Vector2(1.1f, 0.1f); // « Panier DecoAR · n articles » sur une ligne
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true; label.fontSizeMin = 0.1f; label.fontSizeMax = 2f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.95f, 0.82f, 0.55f); // laiton
        }

        void UpdateLabel()
        {
            if (!label) return;
            int n = items.Count;
            label.text = n == 0 ? "<b>Panier DecoAR</b>" : $"<b>Panier DecoAR</b>  ·  {n} article{(n > 1 ? "s" : "")}";
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (label && cam) label.transform.rotation = Quaternion.LookRotation(label.transform.position - cam.transform.position);
        }

        [ContextMenu("Test : ajouter un article")]
        void TestAdd() => AddItem(null, Random.ColorHSV(0f, 1f, 0.3f, 0.6f, 0.7f, 1f));

        [ContextMenu("Test : vider")]
        void TestClear() => Clear();
    }
}
