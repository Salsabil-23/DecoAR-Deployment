using UnityEngine;

namespace DecoAR.AR
{
    public static class M4Sizing
    {
        public static Bounds GetBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        // sizeCm : x = largeur, y = HAUTEUR, z = profondeur
        public static void FitToRealSize(GameObject go, Vector3 sizeCm)
        {
            var b = GetBounds(go);
            if (b.size.y < 0.0001f)
            {
                Debug.LogWarning("[M4] Aucun renderer trouve, pas de mise a l'echelle");
                return;
            }
            float factor = (sizeCm.y / 100f) / b.size.y;
            go.transform.localScale *= factor;
            Debug.Log($"[M4] Taille avant: {b.size} | cible: {sizeCm.y / 100f} m | facteur: {factor} | apres: {GetBounds(go).size}");
        }
    }
}