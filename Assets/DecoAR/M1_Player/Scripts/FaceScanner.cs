using System;
using System.Collections;
using DecoAR.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace DecoAR.Player
{
    // Scan du visage a la camera frontale : on verifie qu'un visage est dans l'ovale (pixels de peau),
    // puis on mesure la couleur des joues et des cheveux pour les appliquer au personnage.
    // Aucune image n'est enregistree : seules deux couleurs sortent de ce script.
    public class FaceScanner : MonoBehaviour
    {
        [SerializeField] CharacterProfile profile;

        [Header("Camera")]
        [SerializeField] RectTransform videoFrame;   // cadre carre (masque)
        [SerializeField] RawImage video;
        [SerializeField] Image oval;
        [SerializeField] GameObject introGroup;      // explication + bouton d'autorisation
        [SerializeField] Button enableCameraButton;

        [Header("Etat")]
        [SerializeField] TMP_Text status;
        [SerializeField] Image progressFill;
        [SerializeField] GameObject resultGroup;
        [SerializeField] Image skinSwatch;
        [SerializeField] Image hairSwatch;
        [SerializeField] TMP_Text hairLabel;
        [SerializeField] RawImage facePreview;

        [Header("Boutons")]
        [SerializeField] Button applyButton;
        [SerializeField] Button retryButton;
        [SerializeField] Button closeButton;

        [Header("Detection")]
        [SerializeField] float requiredTime = 1.5f;    // secondes de visage stable
        [SerializeField] float minSkinRatio = 0.4f;    // part de pixels de peau dans l'ovale
        [SerializeField] float sampleInterval = 0.12f;
        [SerializeField] Color ovalIdle = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] Color ovalDetected = new Color(0.55f, 0.78f, 0.50f, 1f);

        // Ovale guide, en fraction du cadre (centre = 0, cote du cadre = 1). Doit correspondre a l'image "Ovale".
        public const float OvalRx = 0.28f;
        public const float OvalRy = 0.375f;

        // peau, cheveux, cheveux detectes, photo du visage detouree
        public event Action<Color, Color, bool, Texture2D> Applied;

        WebCamTexture cam;
        Color32[] pixels;
        bool mirror;
        bool done;
        float faceTime;
        float nextSample;

        Vector3 skinSum, hairSum;
        int skinCount, hairCount;
        Color skinResult, hairResult;
        bool hasHair;
        Color skinTint = Color.white, hairTint = Color.white;
        Texture2D faceTexture;

        void Awake()
        {
            enableCameraButton.onClick.AddListener(() => StartCoroutine(StartCamera()));
            applyButton.onClick.AddListener(Apply);
            retryButton.onClick.AddListener(ResetScan);
            closeButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            ResetScan();
            introGroup.SetActive(true);
            video.enabled = false;
            status.text = "Autorise la caméra frontale pour lancer le scan.";
        }

        public void Close()
        {
            StopCamera();
            gameObject.SetActive(false);
        }

        void OnDisable() => StopCamera();

        void ResetScan()
        {
            done = false;
            faceTime = 0f;
            ClearSamples();
            resultGroup.SetActive(false);
            progressFill.fillAmount = 0f;
            oval.color = ovalIdle;
            if (cam != null) status.text = "Place ton visage dans l'ovale.";
        }

        void ClearSamples()
        {
            skinSum = hairSum = Vector3.zero;
            skinCount = hairCount = 0;
        }

        IEnumerator StartCamera()
        {
            introGroup.SetActive(false);
            status.text = "Demande d'accès à la caméra…";

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                float t = 0f;
                while (!Permission.HasUserAuthorizedPermission(Permission.Camera) && t < 15f)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            bool granted = Permission.HasUserAuthorizedPermission(Permission.Camera);
#else
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            bool granted = Application.HasUserAuthorization(UserAuthorization.WebCam);
#endif
            if (!granted)
            {
                status.text = "Accès à la caméra refusé. Tu peux choisir tes couleurs à la main.";
                introGroup.SetActive(true);
                yield break;
            }

            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                status.text = "Aucune caméra détectée sur cet appareil.";
                yield break;
            }

            var device = devices[0];
            foreach (var d in devices)
                if (d.isFrontFacing) { device = d; break; }

            // Effet miroir comme un selfie (webcam du PC comprise)
            mirror = device.isFrontFacing || !Application.isMobilePlatform;

            cam = new WebCamTexture(device.name, 640, 480, 30);
            cam.Play();

            float wait = 0f;
            while (cam.width < 32 && wait < 5f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            if (cam.width < 32)
            {
                status.text = "La caméra ne répond pas.";
                StopCamera();
                yield break;
            }

            video.texture = cam;
            video.enabled = true;
            ResetScan();
        }

        void StopCamera()
        {
            if (cam == null) return;
            cam.Stop();
            Destroy(cam);
            cam = null;
            if (video != null)
            {
                video.texture = null;
                video.enabled = false;
            }
        }

        void Update()
        {
            if (cam == null || !cam.isPlaying || cam.width < 32) return;

            LayoutVideo();
            if (done || Time.unscaledTime < nextSample) return;
            nextSample = Time.unscaledTime + sampleInterval;
            Analyse();
        }

        // Image camera tournee / retournee selon le telephone, et qui remplit le cadre
        void LayoutVideo()
        {
            var rt = video.rectTransform;
            rt.sizeDelta = new Vector2(cam.width, cam.height) * TexelToFrame();
            rt.localEulerAngles = new Vector3(0f, 0f, -cam.videoRotationAngle);
            rt.localScale = new Vector3(FlipX(), FlipY(), 1f);
        }

        // Retournements dans le repere de la texture (appliques avant la rotation).
        // Le miroir selfie est horizontal a l'ecran : image tournee de 90 deg, il devient vertical dans la texture.
        bool Rotated => cam.videoRotationAngle % 180 != 0;
        float FlipX() => mirror && !Rotated ? -1f : 1f;
        float FlipY() => (cam.videoVerticallyMirrored ? -1f : 1f) * (mirror && Rotated ? -1f : 1f);

        float TexelToFrame()
        {
            float shownW = Rotated ? cam.height : cam.width;
            float shownH = Rotated ? cam.width : cam.height;
            return videoFrame.rect.width / Mathf.Min(shownW, shownH);
        }

        // Point du cadre (fractions, centre = 0, y vers le haut) -> pixel de la texture camera
        bool ToPixel(float fx, float fy, out Color32 c)
        {
            c = default;
            float frame = videoFrame.rect.width;
            float x = fx * frame, y = fy * frame;

            float a = cam.videoRotationAngle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
            float lx = (x * cos - y * sin) * FlipX();
            float ly = (x * sin + y * cos) * FlipY();

            float k = TexelToFrame();
            int px = (int)(cam.width * 0.5f + lx / k);
            int py = (int)(cam.height * 0.5f + ly / k);
            if (px < 0 || py < 0 || px >= cam.width || py >= cam.height) return false;

            c = pixels[py * cam.width + px];
            return true;
        }

        void Analyse()
        {
            int count = cam.width * cam.height;
            if (pixels == null || pixels.Length != count) pixels = new Color32[count];
            cam.GetPixels32(pixels);

            // 1. Y a-t-il un visage dans l'ovale ?
            const int grid = 18;
            int inside = 0, skin = 0;
            float lum = 0f;
            for (int i = 0; i < grid; i++)
            for (int j = 0; j < grid; j++)
            {
                float u = (i + 0.5f) / grid * 2f - 1f;
                float v = (j + 0.5f) / grid * 2f - 1f;
                if (u * u + v * v > 1f) continue;
                if (!ToPixel(u * OvalRx * 0.8f, v * OvalRy * 0.8f, out var c)) continue;
                inside++;
                lum += Luma(c);
                if (IsSkin(c)) skin++;
            }
            if (inside == 0) return;

            bool dark = lum / inside < 45f;
            bool face = !dark && skin / (float)inside >= minSkinRatio;

            if (face)
            {
                faceTime += sampleInterval;
                Accumulate();
                oval.color = ovalDetected;
                status.text = "Visage détecté, ne bouge pas…";
            }
            else
            {
                faceTime = Mathf.Max(0f, faceTime - sampleInterval * 2f);
                if (faceTime <= 0f) ClearSamples();
                oval.color = ovalIdle;
                status.text = dark
                    ? "Il fait trop sombre : mets-toi face à une lumière."
                    : "Place ton visage dans l'ovale.";
            }

            progressFill.fillAmount = Mathf.Clamp01(faceTime / requiredTime);
            if (faceTime >= requiredTime) Finish();
        }

        void Accumulate()
        {
            // Joues : peau la plus representative (ni sourcils, ni yeux, ni bouche)
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 6; i++)
            for (int j = 0; j < 6; j++)
            {
                float fx = side * 0.13f + (i - 2.5f) * 0.012f;
                float fy = -0.06f + (j - 2.5f) * 0.012f;
                if (ToPixel(fx, fy, out var c) && IsSkin(c))
                {
                    skinSum += new Vector3(c.r, c.g, c.b);
                    skinCount++;
                }
            }

            // Cheveux : bande en haut de l'ovale. On ne garde que les pixels nettement plus sombres
            // que la peau : le mur ou le plafond derriere la tete sont exclus.
            float skinLuma = skinCount > 0 ? SkinTone.Lum(ToColor(skinSum / skinCount)) * 255f : 120f;
            for (int i = 0; i < 12; i++)
            for (int j = 0; j < 6; j++)
            {
                float fx = (i - 5.5f) * 0.022f;
                float fy = OvalRy - 0.07f + j * 0.025f;
                if (!ToPixel(fx, fy, out var c) || IsSkin(c)) continue;
                float y = Luma(c);
                if (y < 8f || y > skinLuma * 0.75f) continue;
                hairSum += new Vector3(c.r, c.g, c.b);
                hairCount++;
            }
        }

        void Finish()
        {
            done = true;
            skinResult = skinCount > 0 ? ToColor(skinSum / skinCount) : profile.textureSkin;
            hasHair = hairCount > 20;
            hairResult = hasHair ? ToColor(hairSum / hairCount) : profile.textureHair;

            // La camera fausse les couleurs (balance des blancs, exposition) : on retient la teinte
            // de la palette la plus proche, puis on recale la photo sur cette teinte
            skinTint = SkinTone.NearestSkin(profile, skinResult);
            hairTint = hasHair ? SkinTone.NearestHair(profile, hairResult) : Color.white;
            Color skinShown = SkinTone.Mul(profile.textureSkin, skinTint);

            skinSwatch.color = skinShown;
            hairSwatch.color = hasHair ? SkinTone.Mul(profile.textureHair, hairTint) : new Color(1f, 1f, 1f, 0.3f);
            hairLabel.text = hasHair ? "Cheveux" : "Cheveux (non détectés)";
            CaptureFace(SkinTone.Gain(skinResult, skinShown));
            facePreview.texture = faceTexture;
            resultGroup.SetActive(true);
            progressFill.fillAmount = 1f;
            status.text = "Scan terminé ! Voici les couleurs détectées.";
            HapticManager.Vibrate(HapticIntensity.Legere);
        }

        // Photo du visage telle qu'elle apparait dans l'ovale, bords fondus pour se raccorder a la peau du personnage
        void CaptureFace(Vector3 gain)
        {
            const int w = 256;
            int h = Mathf.RoundToInt(w * OvalRy / OvalRx);
            const float margin = 1.1f;   // un peu au-dela de l'ovale : front et menton complets
            const float solid = 0.84f;   // opaque jusqu'a ce rayon (1 = bord de l'ovale)
            const float fade = 1.04f;    // transparent a partir de ce rayon

            if (faceTexture != null) Destroy(faceTexture);
            faceTexture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            var oval = new float[w * h];   // bords fondus de l'ovale
            var face = new float[w * h];   // 1 = visage, 0 = fond detecte

            // Le fond (mur, plafond) est clair et n'est pas de la peau. Barbe, sourcils et yeux sont
            // plus sombres que la peau : ils restent. Au centre (yeux, dents) on garde tout.
            float bgLuma = Mathf.Max(SkinTone.Lum(skinResult) * 255f * 0.8f, 70f);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = ((x + 0.5f) / w * 2f - 1f) * margin;  // -1..1 en largeur d'ovale
                float v = ((y + 0.5f) / h * 2f - 1f) * margin;
                float r = Mathf.Sqrt(u * u + v * v);
                int k = y * w + x;
                oval[k] = Mathf.Clamp01((fade - r) / (fade - solid));
                if (oval[k] <= 0f || !ToPixel(u * OvalRx, v * OvalRy, out var c)) continue;

                bool background = r > 0.55f && !IsSkin(c) && Luma(c) > bgLuma;
                face[k] = background ? 0f : 1f;
                px[k] = new Color32(Scale(c.r, gain.x), Scale(c.g, gain.y), Scale(c.b, gain.z), 0);
            }

            // Detourage adouci : pas de bord en escalier entre le visage et la peau du personnage
            face = Blur(face, w, h, 4);
            face = Blur(face, w, h, 4);
            for (int k = 0; k < px.Length; k++)
                px[k].a = (byte)(Mathf.Clamp01(oval[k] * Mathf.SmoothStep(0f, 1f, face[k])) * 255f);

            faceTexture.SetPixels32(px);
            faceTexture.Apply();
        }

        // Flou boite separable (rayon en pixels)
        static float[] Blur(float[] src, int w, int h, int radius)
        {
            var tmp = new float[src.Length];
            var dst = new float[src.Length];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float s = 0f; int n = 0;
                for (int i = Mathf.Max(0, x - radius); i <= Mathf.Min(w - 1, x + radius); i++) { s += src[y * w + i]; n++; }
                tmp[y * w + x] = s / n;
            }
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float s = 0f; int n = 0;
                for (int j = Mathf.Max(0, y - radius); j <= Mathf.Min(h - 1, y + radius); j++) { s += tmp[j * w + x]; n++; }
                dst[y * w + x] = s / n;
            }
            return dst;
        }

        void Apply()
        {
            Applied?.Invoke(skinTint, hairTint, hasHair, faceTexture);
            faceTexture = null; // appartient maintenant au personnage
            Close();
        }


        static byte Scale(byte v, float gain) => (byte)Mathf.Clamp(v * gain, 0f, 255f);

        static Color ToColor(Vector3 rgb255) => new Color(rgb255.x / 255f, rgb255.y / 255f, rgb255.z / 255f);

        static float Luma(Color32 c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        // Classifieur de peau classique en YCbCr, valable pour toutes les carnations
        static bool IsSkin(Color32 c)
        {
            float cb = 128f - 0.168736f * c.r - 0.331264f * c.g + 0.5f * c.b;
            float cr = 128f + 0.5f * c.r - 0.418688f * c.g - 0.081312f * c.b;
            return Luma(c) > 40f && cr >= 135f && cr <= 180f && cb >= 85f && cb <= 135f;
        }
    }
}
