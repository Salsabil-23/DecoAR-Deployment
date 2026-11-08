using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace DecoAR.Auth
{
    // Camera frontale affichee dans un cercle, en miroir (comme un miroir : gauche = gauche de l'utilisateur).
    // S'ouvre avec son GameObject (OnEnable) et se ferme avec lui. Utilisee par SelfieCapture et FaceChallenge.
    public class FaceCamera : MonoBehaviour
    {
        [SerializeField] RawImage preview;

        WebCamTexture cam;
        bool frontFacing;
        Texture frozen;

        public bool Ready => cam != null && cam.width > 16;
        public string Error { get; private set; }

        void OnEnable() => StartCoroutine(Open());

        void OnDisable()
        {
            StopAllCoroutines();
            if (cam != null)
            {
                cam.Stop();
                Destroy(cam);
                cam = null;
            }
            frozen = null;
        }

        IEnumerator Open()
        {
            Error = null;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                float waited = 0;
                while (!Permission.HasUserAuthorizedPermission(Permission.Camera) && waited < 15f)
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
#else
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
#endif
            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Error = "Aucune caméra trouvée (ou accès refusé)";
                yield break;
            }

            var device = devices.FirstOrDefault(d => d.isFrontFacing);
            if (string.IsNullOrEmpty(device.name)) device = devices[0];
            frontFacing = device.isFrontFacing;
            cam = new WebCamTexture(device.name, 1280, 720, 30);
            cam.Play();
            preview.texture = cam;
        }

        void Update()
        {
            if (!Ready || frozen != null) return;
            // Apercu : carre central, tourne selon le capteur, en miroir pour la camera frontale
            float aspect = (float)cam.width / cam.height;
            preview.uvRect = aspect >= 1
                ? new UnityEngine.Rect((1 - 1 / aspect) / 2, 0, 1 / aspect, 1)
                : new UnityEngine.Rect(0, (1 - aspect) / 2, 1, aspect);
            // L'echelle s'applique AVANT la rotation : avec un capteur tourne de 90/270 degres (telephone),
            // le miroir gauche/droite de l'ecran correspond a l'axe Y de l'image, sinon l'apercu est a l'envers.
            bool sideways = ((cam.videoRotationAngle / 90) % 2 + 2) % 2 == 1;
            bool mirrorX = frontFacing && !sideways;
            bool mirrorY = cam.videoVerticallyMirrored ^ (frontFacing && sideways);
            preview.rectTransform.localEulerAngles = new Vector3(0, 0, -cam.videoRotationAngle);
            preview.rectTransform.localScale = new Vector3(mirrorX ? -1 : 1, mirrorY ? -1 : 1, 1);
        }

        // Affiche une image fixe (photo prise) a la place de la camera ; null = retour a la camera
        public void Freeze(Texture image)
        {
            frozen = image;
            if (image != null)
            {
                cam?.Pause();
                preview.texture = image;
                preview.uvRect = new UnityEngine.Rect(0, 0, 1, 1);
                preview.rectTransform.localEulerAngles = Vector3.zero;
                preview.rectTransform.localScale = Vector3.one;
            }
            else if (cam != null)
            {
                cam.Play();
                preview.texture = cam;
            }
        }

        // Image actuelle : meme cadrage que l'apercu (carre, miroir, rotation), size x size
        public Texture2D Grab(int size)
        {
            if (!Ready) return null;
            int w = cam.width, h = cam.height, s = Mathf.Min(w, h);
            int x0 = (w - s) / 2, y0 = (h - s) / 2;
            var src = cam.GetPixels32();
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    px[y * s + x] = src[(y + y0) * w + x + x0];

            // 1. image du capteur remise a l'endroit (retournement + rotation), 2. miroir de la camera frontale.
            // Dans cet ordre, le miroir reste gauche/droite meme quand le capteur est tourne (telephone).
            if (cam.videoVerticallyMirrored) px = Mirror(px, s, horizontal: false);
            for (int i = 0; i < ((cam.videoRotationAngle / 90) % 4 + 4) % 4; i++) px = RotateClockwise(px, s);
            if (frontFacing) px = Mirror(px, s, horizontal: true);

            var square = new Texture2D(s, s, TextureFormat.RGBA32, false);
            square.SetPixels32(px);
            square.Apply();

            size = Mathf.Min(size, s);
            var rt = RenderTexture.GetTemporary(size, size);
            Graphics.Blit(square, rt);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var result = new Texture2D(size, size, TextureFormat.RGB24, false);
            result.ReadPixels(new UnityEngine.Rect(0, 0, size, size), 0, 0);
            result.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Destroy(square);
            return result;
        }

        public byte[] GrabJpeg(int size)
        {
            var tex = Grab(size);
            if (tex == null) return null;
            var jpg = tex.EncodeToJPG(88);
            Destroy(tex);
            return jpg;
        }

        static Color32[] Mirror(Color32[] px, int s, bool horizontal)
        {
            var o = new Color32[px.Length];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    o[y * s + x] = horizontal ? px[y * s + (s - 1 - x)] : px[(s - 1 - y) * s + x];
            return o;
        }

        // Rotation de 90 degres dans le sens horaire (coordonnees texture, y vers le haut)
        static Color32[] RotateClockwise(Color32[] px, int s)
        {
            var o = new Color32[px.Length];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    o[(s - 1 - x) * s + y] = px[y * s + x];
            return o;
        }
    }
}
