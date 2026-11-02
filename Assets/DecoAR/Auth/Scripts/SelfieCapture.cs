using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DecoAR.Auth
{
    // Photo de profil : apercu de la camera, "Prendre la photo", puis "Utiliser" ou "Reprendre".
    // Confirmed renvoie la photo en JPEG (carre 480 px).
    public class SelfieCapture : MonoBehaviour
    {
        const string DefaultHint = "Place ton visage dans le cercle, bien éclairé";

        [SerializeField] FaceCamera faceCamera;
        [SerializeField] TMP_Text hint;
        [SerializeField] Button captureButton;
        [SerializeField] Button useButton;
        [SerializeField] Button retakeButton;
        [SerializeField] Button cancelButton;

        public event Action<byte[]> Confirmed;
        public event Action Cancelled;

        Texture2D shot;

        void Awake()
        {
            captureButton.onClick.AddListener(Capture);
            retakeButton.onClick.AddListener(Retake);
            useButton.onClick.AddListener(() => { if (shot != null) Confirmed?.Invoke(shot.EncodeToJPG(88)); });
            cancelButton.onClick.AddListener(() => Cancelled?.Invoke());
        }

        void OnEnable()
        {
            hint.text = DefaultHint;
            ShowButtons(captured: false);
        }

        void OnDisable() => ClearShot();

        void Update()
        {
            if (shot == null) captureButton.interactable = faceCamera.Ready;
            if (faceCamera.Error != null) hint.text = faceCamera.Error;
        }

        void Capture()
        {
            shot = faceCamera.Grab(480);
            if (shot == null) return;
            faceCamera.Freeze(shot);
            hint.text = "Elle te plaît ?";
            ShowButtons(captured: true);
        }

        void Retake()
        {
            ClearShot();
            faceCamera.Freeze(null);
            hint.text = DefaultHint;
            ShowButtons(captured: false);
        }

        void ClearShot()
        {
            if (shot != null) { Destroy(shot); shot = null; }
        }

        void ShowButtons(bool captured)
        {
            captureButton.gameObject.SetActive(!captured);
            useButton.gameObject.SetActive(captured);
            retakeButton.gameObject.SetActive(captured);
        }
    }
}
