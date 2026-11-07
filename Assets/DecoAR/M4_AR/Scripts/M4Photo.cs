using System.Collections;
using UnityEngine;

namespace DecoAR.AR
{
    public class M4Photo : MonoBehaviour
    {
        [SerializeField] Canvas uiCanvas;        // M4_Canvas (cache pendant la capture)
        [SerializeField] M4PlacementGate gate;   // pour les messages
        [SerializeField] string album = "DecoAR";

        bool busy;

        /// Interface à masquer pendant la capture (M4Hud remplace M4_Canvas).
        public Canvas UiCanvas { get => uiCanvas; set => uiCanvas = value; }

        // Appelee par le bouton "Photo"
        public void TakePhoto()
        {
            if (!busy) StartCoroutine(CaptureRoutine());
        }

        IEnumerator CaptureRoutine()
        {
            busy = true;
            if (uiCanvas != null) uiCanvas.enabled = false;   // l'interface ne doit pas apparaitre sur la photo

            yield return new WaitForEndOfFrame();

            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (uiCanvas != null) uiCanvas.enabled = true;

            NativeGallery.SaveImageToGallery(tex, album, "DecoAR_{0}.jpg", (success, path) =>
            {
                if (gate != null)
                    gate.ShowTemporary(success
                        ? "Photo enregistrée dans la galerie"
                        : "Photo non enregistrée (autorisation refusée ?)");
                Destroy(tex);
                busy = false;
            });
        }
    }
}