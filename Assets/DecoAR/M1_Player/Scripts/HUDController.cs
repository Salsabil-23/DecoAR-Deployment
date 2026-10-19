using System;
using DecoAR.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DecoAR.Player
{
    // HUD partage (F1.7) : panier avec compteur, mini-carte, parametres, gyroscope (F1.6)
    public class HUDController : MonoBehaviour
    {
        [Header("Barre du haut")]
        [SerializeField] Button cartButton;
        [SerializeField] GameObject cartBadge;
        [SerializeField] TMP_Text cartCountText;
        [SerializeField] Button minimapButton;
        [SerializeField] Button gyroButton;
        [SerializeField] TMP_Text gyroLabel;
        [SerializeField] Button settingsButton;

        [Header("Parametres")]
        [SerializeField] GameObject settingsPanel;
        [SerializeField] Button hapticsButton;
        [SerializeField] TMP_Text hapticsLabel;
        [SerializeField] Button recalibrateButton;
        [SerializeField] Button closeSettingsButton;

        [SerializeField] ThirdPersonCamera playerCamera;

        // M5 ouvre l'ecran panier, M2 la mini-carte
        public static event Action CartRequested;
        public static event Action MinimapRequested;

        void Start()
        {
            cartButton.onClick.AddListener(() => CartRequested?.Invoke());
            minimapButton.onClick.AddListener(() => MinimapRequested?.Invoke());
            gyroButton.onClick.AddListener(ToggleGyro);
            settingsButton.onClick.AddListener(() => settingsPanel.SetActive(true));
            closeSettingsButton.onClick.AddListener(() => settingsPanel.SetActive(false));
            hapticsButton.onClick.AddListener(ToggleHaptics);
            recalibrateButton.onClick.AddListener(() => { if (playerCamera != null) playerCamera.Recalibrate(); });

            settingsPanel.SetActive(false);
            if (!ThirdPersonCamera.GyroAvailable) gyroButton.interactable = false;

            var gm = GameManager.Instance;
            gm.CartCountChanged += RefreshCart;
            gm.GyroChanged += RefreshGyro;
            gm.HapticsChanged += RefreshHaptics;

            RefreshCart(gm.CartCount);
            RefreshGyro(gm.GyroEnabled);
            RefreshHaptics(gm.HapticsEnabled);
        }

        void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.CartCountChanged -= RefreshCart;
            gm.GyroChanged -= RefreshGyro;
            gm.HapticsChanged -= RefreshHaptics;
        }

        void ToggleGyro()
        {
            var gm = GameManager.Instance;
            gm.SetGyroEnabled(!gm.GyroEnabled);
        }

        void ToggleHaptics()
        {
            var gm = GameManager.Instance;
            gm.SetHapticsEnabled(!gm.HapticsEnabled);
        }

        void RefreshCart(int count)
        {
            cartBadge.SetActive(count > 0);
            cartCountText.text = count > 99 ? "99+" : count.ToString();
        }

        void RefreshGyro(bool on)
        {
            if (!ThirdPersonCamera.GyroAvailable) gyroLabel.text = "Gyro indisponible";
            else gyroLabel.text = on ? "Gyro : ON" : "Gyro : OFF";
        }

        void RefreshHaptics(bool on) => hapticsLabel.text = on ? "Vibrations : ON" : "Vibrations : OFF";
    }
}
