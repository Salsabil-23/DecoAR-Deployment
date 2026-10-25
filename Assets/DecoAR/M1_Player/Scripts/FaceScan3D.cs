using System.Collections;
using System.Collections.Generic;
using DecoAR.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DecoAR.Player
{
    // Scan 3D du visage : AR Foundation (ARCore / ARKit, camera frontale) fournit le maillage reel du visage
    // (~470 points). On le fige, on y colle l'image de la camera prise au meme instant, et on l'enregistre :
    // CharacterAppearance l'accroche ensuite sur la tete du personnage.
    public class FaceScan3D : MonoBehaviour
    {
        [SerializeField] CharacterProfile profile;

        [Header("AR")]
        [SerializeField] GameObject arRoot;          // ARSession + XR Origin, actives apres l'explication
        [SerializeField] ARFaceManager faceManager;
        [SerializeField] Camera arCamera;

        [Header("UI")]
        [SerializeField] Canvas canvas;
        [SerializeField] GameObject introGroup;
        [SerializeField] Button startButton;
        [SerializeField] TMP_Text status;
        [SerializeField] Image progressFill;
        [SerializeField] Button cancelButton;
        [SerializeField] GameObject unsupportedGroup;
        [SerializeField] Button photoScanButton;
        [SerializeField] Button backButton;

        [Header("Capture")]
        [SerializeField] float requiredTime = 1.6f;   // secondes de suivi stable avant la capture
        [SerializeField] int textureSize = 512;
        [SerializeField] string returnScene = "M1_Personnalisation";

        float stableTime;
        bool running;
        bool capturing;

        void Awake()
        {
            startButton.onClick.AddListener(() => StartCoroutine(Begin()));
            cancelButton.onClick.AddListener(Return);
            photoScanButton.onClick.AddListener(UsePhotoScan);
            backButton.onClick.AddListener(Return);

            arRoot.SetActive(false);
            introGroup.SetActive(true);
            unsupportedGroup.SetActive(false);
            progressFill.fillAmount = 0f;
            status.text = "Le scan 3D utilise la caméra frontale et le suivi du visage de ton téléphone.";
        }

        IEnumerator Begin()
        {
            introGroup.SetActive(false);
            status.text = "Vérification du suivi 3D du visage…";

            if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
                yield return ARSession.CheckAvailability();
            if (ARSession.state == ARSessionState.Unsupported)
            {
                ShowUnsupported();
                yield break;
            }

            // La session demande l'autorisation camera ; ARCore peut proposer d'installer ses services
            arRoot.SetActive(true);
            float wait = 0f;
            while (ARSession.state < ARSessionState.SessionInitializing && wait < 30f)
            {
                if (ARSession.state == ARSessionState.Unsupported) { ShowUnsupported(); yield break; }
                status.text = ARSession.state == ARSessionState.Installing
                    ? "Installation des services de réalité augmentée…"
                    : "Démarrage de la caméra…";
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;

            if (faceManager.subsystem == null)
            {
                ShowUnsupported();
                yield break;
            }
            running = true;
        }

        void ShowUnsupported()
        {
            arRoot.SetActive(false);
            unsupportedGroup.SetActive(true);
            status.text = "Ton téléphone ne gère pas le suivi 3D du visage. Utilise le scan photo.";
        }

        void Update()
        {
            if (!running || capturing) return;

            ARFace face = null;
            foreach (var f in faceManager.trackables)
                if (f.trackingState == TrackingState.Tracking) { face = f; break; }

            bool ready = face != null && face.vertices.IsCreated && face.vertices.Length > 100 && face.indices.IsCreated;
            if (ready)
            {
                stableTime += Time.deltaTime;
                status.text = "Visage détecté en 3D, ne bouge pas…";
            }
            else
            {
                stableTime = Mathf.Max(0f, stableTime - Time.deltaTime * 2f);
                status.text = "Place ton visage face à la caméra, bien éclairé.";
            }

            progressFill.fillAmount = Mathf.Clamp01(stableTime / requiredTime);
            if (ready && stableTime >= requiredTime) StartCoroutine(Capture(face));
        }

        IEnumerator Capture(ARFace face)
        {
            capturing = true;
            status.text = "Capture…";

            // L'image capturee ne doit contenir que la camera : on cache l'UI et le maillage de suivi
            var visual = face.GetComponent<Renderer>();
            if (visual != null) visual.enabled = false;
            canvas.enabled = false;
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();

            // Maillage et projection a l'ecran au meme instant que l'image
            var vertices = face.vertices.ToArray();
            var triangles = face.indices.ToArray();
            var screen = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = arCamera.WorldToScreenPoint(face.transform.TransformPoint(vertices[i]));
                screen[i] = new Vector2(p.x / shot.width, p.y / shot.height);
            }
            Vector3 toCamera = face.transform.InverseTransformPoint(arCamera.transform.position).normalized;

            canvas.enabled = true;
            if (visual != null) visual.enabled = true;

            bool ok = Build(vertices, triangles, screen, toCamera, shot, out var data, out var texture,
                out var skinTint, out var hairTint, out bool hasHair);
            Destroy(shot);

            if (!ok)
            {
                status.text = "Scan incomplet, réessaie face à la caméra.";
                stableTime = 0f;
                capturing = false;
                yield break;
            }

            FaceMaskStore.Save(data, texture);
            FaceStore.Delete(); // le visage 3D remplace un eventuel scan photo

            var gm = GameManager.Instance;
            var look = gm.Look;
            look.visagePhoto = true;
            look.Set(LookSlot.Peau, skinTint);
            if (hasHair) look.Set(LookSlot.Cheveux, hairTint);
            gm.SetLook(look);

            HapticManager.Vibrate(HapticIntensity.Moyenne);
            status.text = "Visage 3D enregistré !";
            progressFill.fillAmount = 1f;
            yield return new WaitForSeconds(0.7f);
            Return();
        }

        // Repere normalise du visage, couleurs, et texture du masque
        bool Build(Vector3[] v, int[] tris, Vector2[] screen, Vector3 front, Texture2D shot,
            out FaceMaskData data, out Texture2D texture, out Color skinTint, out Color hairTint, out bool hasHair)
        {
            data = null; texture = null; skinTint = hairTint = Color.white; hasHair = false;

            Vector3 f = front;
            Vector3 u = Vector3.ProjectOnPlane(Vector3.up, f).normalized;
            Vector3 r = Vector3.Cross(u, f); // droite de la personne

            int n = v.Length;
            var c = new Vector3[n];
            for (int i = 0; i < n; i++) c[i] = new Vector3(Vector3.Dot(v[i], r), Vector3.Dot(v[i], u), Vector3.Dot(v[i], f));

            // Memes reperes que sur le personnage (CharacterAppearance.TryFindFace) : nez, menton, front
            int nose = -1;
            for (int i = 0; i < n; i++)
                if (Mathf.Abs(c[i].x) < 0.035f && (nose < 0 || c[i].z > c[nose].z)) nose = i;
            if (nose < 0) return false;

            float chin = float.MaxValue, brow = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                if (Mathf.Abs(c[i].x - c[nose].x) > 0.05f || c[i].z < c[nose].z - 0.045f) continue;
                chin = Mathf.Min(chin, c[i].y);
                brow = Mathf.Max(brow, c[i].y);
            }
            float height = brow - chin;
            if (height < 0.08f) return false;

            Vector3 origin = new Vector3(c[nose].x, (chin + brow) * 0.5f, c[nose].z);
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                c[i] -= origin;
                minX = Mathf.Min(minX, c[i].x); maxX = Mathf.Max(maxX, c[i].x);
                minY = Mathf.Min(minY, c[i].y); maxY = Mathf.Max(maxY, c[i].y);
            }

            // UV de face et bords fondus (le contour du masque se mele a la peau du personnage)
            var uv = new Vector2[n];
            var alpha = new float[n];
            float cx = (minX + maxX) * 0.5f, cy = (minY + maxY) * 0.5f;
            float hw = (maxX - minX) * 0.5f, hh = (maxY - minY) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                uv[i] = new Vector2((c[i].x - minX) / (maxX - minX), (c[i].y - minY) / (maxY - minY));
                float ex = (c[i].x - cx) / hw, ey = (c[i].y - cy) / hh;
                alpha[i] = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 0.98f, Mathf.Sqrt(ex * ex + ey * ey)));
            }

            var src = shot.GetPixels32();

            // Peau : joues ; cheveux : au-dessus du front, pixels plus sombres que la peau
            var skinSamples = new List<Color>();
            for (int i = 0; i < n; i++)
            {
                float ax = Mathf.Abs(c[i].x), y = c[i].y + height * 0.5f; // y depuis le menton
                if (ax > 0.03f && ax < 0.06f && y > height * 0.3f && y < height * 0.5f)
                    skinSamples.Add(Sample(src, shot.width, shot.height, screen[i]));
            }
            if (skinSamples.Count == 0) return false;
            Color skin = Average(skinSamples);

            int browIndex = 0, chinIndex = 0;
            for (int i = 0; i < n; i++)
            {
                if (c[i].y > c[browIndex].y && Mathf.Abs(c[i].x) < 0.05f) browIndex = i;
                if (c[i].y < c[chinIndex].y && Mathf.Abs(c[i].x) < 0.05f) chinIndex = i;
            }
            Vector2 screenUp = screen[browIndex] - screen[chinIndex];
            var hairSamples = new List<Color>();
            float skinLum = SkinTone.Lum(skin);
            for (int k = -4; k <= 4; k++)
            for (int j = 1; j <= 3; j++)
            {
                Vector2 side = new Vector2(screenUp.y, -screenUp.x) * (k * 0.06f);
                Color s = Sample(src, shot.width, shot.height, screen[browIndex] + screenUp * (0.06f * j) + side);
                if (SkinTone.Lum(s) < skinLum * 0.75f) hairSamples.Add(s);
            }
            hasHair = hairSamples.Count >= 6;

            skinTint = SkinTone.NearestSkin(profile, skin);
            if (hasHair) hairTint = SkinTone.NearestHair(profile, Average(hairSamples));
            Vector3 gain = SkinTone.Gain(skin, SkinTone.Mul(profile.textureSkin, skinTint));

            texture = Bake(src, shot.width, shot.height, screen, uv, alpha, tris, gain, textureSize);
            data = new FaceMaskData { vertices = c, triangles = tris, uv = uv, height = height };
            return true;
        }

        // Rasterise chaque triangle dans la texture du masque en lisant l'image camera a sa position ecran
        static Texture2D Bake(Color32[] src, int sw, int sh, Vector2[] screen, Vector2[] uv, float[] alpha,
            int[] tris, Vector3 gain, int size)
        {
            var dst = new Color32[size * size];
            for (int t = 0; t + 2 < tris.Length; t += 3)
            {
                int a = tris[t], b = tris[t + 1], c = tris[t + 2];
                Vector2 pa = uv[a] * size, pb = uv[b] * size, pc = uv[c] * size;
                float area = Edge(pa, pb, pc);
                if (Mathf.Abs(area) < 1e-6f) continue;

                int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(pa.x, Mathf.Min(pb.x, pc.x))), 0, size - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(pa.x, Mathf.Max(pb.x, pc.x))), 0, size - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(pa.y, Mathf.Min(pb.y, pc.y))), 0, size - 1);
                int y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(pa.y, Mathf.Max(pb.y, pc.y))), 0, size - 1);

                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float w0 = Edge(pb, pc, p) / area, w1 = Edge(pc, pa, p) / area, w2 = 1f - w0 - w1;
                    if (w0 < -0.02f || w1 < -0.02f || w2 < -0.02f) continue;

                    Color s = Sample(src, sw, sh, screen[a] * w0 + screen[b] * w1 + screen[c] * w2);
                    float al = alpha[a] * w0 + alpha[b] * w1 + alpha[c] * w2;
                    dst[y * size + x] = new Color32(
                        ToByte(s.r * gain.x), ToByte(s.g * gain.y), ToByte(s.b * gain.z), ToByte(al));
                }
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(dst);
            tex.Apply();
            return tex;
        }

        static float Edge(Vector2 a, Vector2 b, Vector2 p) => (p.x - a.x) * (b.y - a.y) - (p.y - a.y) * (b.x - a.x);

        static Color Sample(Color32[] src, int w, int h, Vector2 uv)
        {
            int x = Mathf.Clamp((int)(uv.x * w), 0, w - 1);
            int y = Mathf.Clamp((int)(uv.y * h), 0, h - 1);
            return src[y * w + x];
        }

        static Color Average(List<Color> colors)
        {
            Color sum = Color.clear;
            foreach (var col in colors) sum += col;
            return sum / colors.Count;
        }

        static byte ToByte(float v) => (byte)(Mathf.Clamp01(v) * 255f);

        void UsePhotoScan()
        {
            CharacterCustomizer.OpenPhotoScanOnStart = true;
            Return();
        }

        void Return() => SceneManager.LoadScene(returnScene);
    }
}
