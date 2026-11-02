using System;
using UnityEngine;

namespace DecoAR.Core
{
    // Type de placement AR d'un produit (cahier des charges 9.3)
    public enum ProductPlacement { Sol, Mur, Surface }

    // Noms des scenes du parcours, partages par les modules
    public static class SceneNames
    {
        public const string Shop = "caisse";       // la boutique M5 (caisse) : retour depuis le catalogue et l'AR
        public const string Catalog = "M3";
        public const string AR = "M4_AR";
        public const string Checkout = "caisse";
    }

    // Article ajoute au panier par M3 ou M4, lu par M5
    [Serializable]
    public class CartItem
    {
        public string productId;
        public string name;
        public string variantName;
        public Color color = Color.white;
        public float price;
        public int quantity = 1;
        [NonSerialized] public GameObject prefab;
    }
}
