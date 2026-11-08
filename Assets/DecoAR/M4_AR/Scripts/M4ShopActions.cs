using DecoAR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DecoAR.AR
{
    // Boutons "Ajouter au panier" et "Retour" de l'ecran AR (F4.8)
    public class M4ShopActions : MonoBehaviour
    {
        [SerializeField] M4PlacementGate gate;

        // Appelee par le bouton "Ajouter au panier" : envoie le produit et sa variante a M5
        public void AddToCart()
        {
            var gm = GameManager.Instance;
            var product = gate != null ? gate.Product : null;
            if (gm == null || product == null)
            {
                if (gate != null) gate.ShowTemporary("Aucun produit à ajouter");
                return;
            }

            var variant = gate.CurrentVariant;
            gm.AddToCart(new CartItem
            {
                productId = product.id,
                name = product.displayName,
                variantName = variant != null ? variant.name : "",
                color = variant != null ? variant.color : Color.white,
                price = product.price,
                prefab = product.prefab,
            });
            HapticManager.Vibrate(HapticIntensity.Legere);
            gate.ShowTemporary($"{product.displayName} ajouté au panier ({gm.CartCount})");
        }

        // Appelee par le bouton "Retour au rayon" : M3 reprend le produit choisi
        public void BackToCatalog() => SceneManager.LoadScene(SceneNames.Catalog);

        // Appelee par le bouton "Retour à la boutique" : l'avatar reprend sa position (GameManager.ShopPose)
        public void BackToShop() => SceneManager.LoadScene(SceneNames.Shop);
    }
}
