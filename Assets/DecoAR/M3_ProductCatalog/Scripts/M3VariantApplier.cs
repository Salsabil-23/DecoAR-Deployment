using System.Collections.Generic;
using UnityEngine;

namespace DecoAR.M3
{
    // Applique une variante sur un modele instancie (M3 en inspection, M4 en AR).
    // Seuls les renderers dont le nom commence par "Variant" changent (ex. le cadre d'un tableau
    // garde sa couleur) ; s'il n'y en a aucun, tous les renderers sont teintes.
    public static class M3VariantApplier
    {
        public const string VariantPrefix = "Variant";

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public static void Apply(GameObject model, ProductVariant variant)
        {
            if (model == null || variant == null) return;

            foreach (var r in GetTargets(model))
            {
                if (variant.material != null)
                {
                    r.sharedMaterial = variant.material;
                    continue;
                }
                // Instance de materiau propre a ce modele : les prefabs du catalogue ne sont pas modifies
                var mat = r.material;
                if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, variant.color);
                else if (mat.HasProperty(ColorId)) mat.SetColor(ColorId, variant.color);
            }
        }

        static List<Renderer> GetTargets(GameObject model)
        {
            var all = model.GetComponentsInChildren<Renderer>(true);
            var tagged = new List<Renderer>();
            foreach (var r in all)
                if (r.gameObject.name.StartsWith(VariantPrefix)) tagged.Add(r);
            return tagged.Count > 0 ? tagged : new List<Renderer>(all);
        }
    }
}
