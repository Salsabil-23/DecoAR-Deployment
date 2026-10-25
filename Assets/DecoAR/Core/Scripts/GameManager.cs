using System;
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

        // Panier : M5 met a jour le nombre d'articles, le HUD (M1) l'affiche
        public int CartCount { get; private set; }

        // Position de l'avatar dans la boutique, restauree au retour de l'AR ou de la caisse
        bool hasShopPose;
        Vector3 shopPosition;
        Quaternion shopRotation;

        public event Action<int> CartCountChanged;
        public event Action<bool> GyroChanged;
        public event Action<bool> HapticsChanged;

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

        public void SetCartCount(int count)
        {
            CartCount = Mathf.Max(0, count);
            CartCountChanged?.Invoke(CartCount);
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
