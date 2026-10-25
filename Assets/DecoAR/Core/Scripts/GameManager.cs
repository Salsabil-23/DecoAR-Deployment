using System;
using System.Collections.Generic;
using UnityEngine;

namespace DecoAR.Core
{
    // Etat persistant partage entre Boutique, AR et Caisse (cahier des charges 9.4)
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        const string KeyLook = "DecoAR.Look";
        const string KeyGyro = "DecoAR.GyroEnabled";
        const string KeyHaptics = "DecoAR.HapticsEnabled";

        // Apparence du personnage (M1), lue par la boutique, l'AR et la caisse
        AvatarLook look = new AvatarLook();
        public AvatarLook Look => look.Clone();
        public bool GyroEnabled { get; private set; }
        public bool HapticsEnabled { get; private set; } = true;
        public bool IsNight { get; set; }

        // Produit courant selectionne dans le catalogue M3 ; transmis a M4 lors du placement AR.
        public string SelectedProductId { get; private set; } = "";
        public GameObject SelectedProductPrefab { get; private set; }
        public Vector3 SelectedProductSizeCm { get; private set; } = new Vector3(50f, 50f, 50f);
        public ProductPlacement SelectedProductPlacement { get; private set; } = ProductPlacement.Sol;
        public int SelectedVariantIndex { get; private set; }
        public bool HasSelectedProduct => SelectedProductPrefab != null;

        // Zone (categorie) ouverte dans le catalogue M3 ; vide = premiere zone
        public string CurrentCategory { get; set; } = "";

        // Panier : M3 et M4 ajoutent des articles, M5 les recupere ; le HUD (M1) affiche le nombre
        public int CartCount { get; private set; }
        readonly List<CartItem> cartItems = new List<CartItem>();
        public IReadOnlyList<CartItem> CartItems => cartItems;

        // Position de l'avatar dans la boutique, restauree au retour de l'AR ou de la caisse
        bool hasShopPose;
        Vector3 shopPosition;
        Quaternion shopRotation;

        public event Action<int> CartCountChanged;
        public event Action<bool> GyroChanged;
        public event Action<bool> HapticsChanged;
        public event Action<string, int> ProductSelected;   // id, variante
        public event Action<CartItem> CartItemAdded;

        // Cree automatiquement, quelle que soit la scene lancee
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            new GameObject("GameManager").AddComponent<GameManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        void Load()
        {
            string json = PlayerPrefs.GetString(KeyLook, "");
            if (!string.IsNullOrEmpty(json)) JsonUtility.FromJsonOverwrite(json, look);
            GyroEnabled = PlayerPrefs.GetInt(KeyGyro, 0) == 1;
            HapticsEnabled = PlayerPrefs.GetInt(KeyHaptics, 1) == 1;
        }

        public void SetLook(AvatarLook newLook)
        {
            look = newLook.Clone();
            PlayerPrefs.SetString(KeyLook, JsonUtility.ToJson(look));
            PlayerPrefs.Save();
        }

        public void SetGyroEnabled(bool enabled)
        {
            if (GyroEnabled == enabled) return;
            GyroEnabled = enabled;
            PlayerPrefs.SetInt(KeyGyro, enabled ? 1 : 0);
            PlayerPrefs.Save();
            GyroChanged?.Invoke(enabled);
        }

        public void SetHapticsEnabled(bool enabled)
        {
            if (HapticsEnabled == enabled) return;
            HapticsEnabled = enabled;
            PlayerPrefs.SetInt(KeyHaptics, enabled ? 1 : 0);
            PlayerPrefs.Save();
            HapticsChanged?.Invoke(enabled);
        }

        public void SelectProduct(string productId, GameObject prefab, Vector3 sizeCm)
            => SelectProduct(productId, prefab, sizeCm, SelectedProductPlacement, 0);

        public void SelectProduct(string productId, GameObject prefab, Vector3 sizeCm, ProductPlacement placement, int variantIndex)
        {
            SelectedProductId = productId ?? string.Empty;
            SelectedProductPrefab = prefab;
            SelectedProductSizeCm = sizeCm;
            SelectedProductPlacement = placement;
            SelectedVariantIndex = Mathf.Max(0, variantIndex);
            ProductSelected?.Invoke(SelectedProductId, SelectedVariantIndex);
        }

        public void SetSelectedVariant(int variantIndex)
        {
            SelectedVariantIndex = Mathf.Max(0, variantIndex);
            ProductSelected?.Invoke(SelectedProductId, SelectedVariantIndex);
        }

        public void ClearSelectedProduct()
        {
            SelectedProductId = string.Empty;
            SelectedProductPrefab = null;
            SelectedProductSizeCm = new Vector3(50f, 50f, 50f);
            SelectedProductPlacement = ProductPlacement.Sol;
            SelectedVariantIndex = 0;
        }

        public void SetCartCount(int count)
        {
            CartCount = Mathf.Max(0, count);
            CartCountChanged?.Invoke(CartCount);
        }

        // Meme produit + meme variante = quantite incrementee
        public void AddToCart(CartItem item)
        {
            if (item == null) return;
            var line = cartItems.Find(x => x.productId == item.productId && x.variantName == item.variantName);
            if (line != null) line.quantity += Mathf.Max(1, item.quantity);
            else cartItems.Add(item);
            SetCartCount(CartCount + Mathf.Max(1, item.quantity));
            CartItemAdded?.Invoke(item);
        }

        // M5 recupere les articles en attente pour les afficher dans sa caisse
        public List<CartItem> TakeCartItems()
        {
            var items = new List<CartItem>(cartItems);
            cartItems.Clear();
            return items;
        }

        public void SaveShopPose(Vector3 position, Quaternion rotation)
        {
            hasShopPose = true;
            shopPosition = position;
            shopRotation = rotation;
        }

        public bool TryGetShopPose(out Vector3 position, out Quaternion rotation)
        {
            position = shopPosition;
            rotation = shopRotation;
            return hasShopPose;
        }

        public void ClearShopPose() => hasShopPose = false;
    }
}
