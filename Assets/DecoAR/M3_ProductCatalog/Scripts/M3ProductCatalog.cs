using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DecoAR.Core;
using UnityEngine;

namespace DecoAR.M3
{
    // Variante de couleur ou de matiere (F3.4) ; appliquee sur les renderers "Variant" du modele
    [Serializable]
    public class ProductVariant
    {
        public string name = "Standard";
        public Color color = Color.white;
        public Material material;      // optionnel : remplace la couleur si renseigne
        public Sprite thumbnail;
    }

    // Format du catalogue partage par M3, M4 et M5 (cahier des charges 9.3)
    [Serializable]
    public class ProductOption
    {
        public string id = "product_01";
        public string displayName = "Produit";
        public string category = M3Categories.ObjetsDeco;
        public float price = 49f;
        public Vector3 sizeCm = new Vector3(40f, 40f, 40f);   // x = largeur, y = hauteur, z = profondeur
        public GameObject prefab;
        public string description = "Produit de décoration";
        public Sprite thumbnail;
        public ProductPlacement placement = ProductPlacement.Surface;
        public List<ProductVariant> variants = new List<ProductVariant>();
        public string keywords = "";   // mots-cles de la recherche vocale / texte

        public string DimensionsLabel => $"{sizeCm.x:0} × {sizeCm.z:0} × {sizeCm.y:0} cm";
        public string PriceLabel => $"{price:0} TND";

        public ProductVariant GetVariant(int index)
        {
            if (variants == null || variants.Count == 0) return null;
            return variants[Mathf.Clamp(index, 0, variants.Count - 1)];
        }
    }

    public static class M3Categories
    {
        public const string Tableaux = "Tableaux";
        public const string Luminaires = "Luminaires";
        public const string ObjetsDeco = "Objets déco";
        public const string Salon = "Salon";
        public const string Chambre = "Chambre";
        public const string Cuisine = "Cuisine";
        public const string SalleDeBain = "Salle de bain";
        public static readonly string[] All = { Tableaux, Luminaires, ObjetsDeco, Salon, Chambre, Cuisine, SalleDeBain };
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

        public List<ProductOption> GetByCategory(string category)
        {
            var result = new List<ProductOption>();
            if (products == null) return result;
            foreach (var product in products)
                if (product != null && (string.IsNullOrEmpty(category) || product.category == category))
                    result.Add(product);
            return result;
        }

        // Recherche vocale ou texte (F3.7, F3.9) : chaque mot prononce est compare au nom, a la categorie,
        // aux mots-cles et aux variantes, sans tenir compte des accents ni de la casse.
        public List<ProductOption> Search(string query)
        {
            var result = new List<ProductOption>();
            if (products == null || string.IsNullOrWhiteSpace(query)) return result;

            var words = Normalize(query).Split(new[] { ' ', ',', '.', '\'', '-' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var product in products)
            {
                if (product == null) continue;
                var haystack = new StringBuilder(Normalize(product.displayName + " " + product.category + " " + product.keywords));
                if (product.variants != null)
                    foreach (var v in product.variants) haystack.Append(' ').Append(Normalize(v.name));
                string text = haystack.ToString();

                foreach (var w in words)
                {
                    if (w.Length < 3) continue;   // ignore "le", "un", "de"...
                    if (text.Contains(w) || (w.EndsWith("s") && text.Contains(w.Substring(0, w.Length - 1))))
                    {
                        result.Add(product);
                        break;
                    }
                }
            }
            return result;
        }

        public static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var decomposed = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString();
        }
    }
}
