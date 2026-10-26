using System.Collections;
using System.Collections.Generic;
using DecoAR.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
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
        [SerializeField] int textureSize = 1024;
        [SerializeField] float sideAngle = 22f;     // rotation de tete demandee pour les vues de cote
        [SerializeField] float sideTimeout = 10f;   // au-dela, on continue avec les vues deja prises
        [SerializeField] string returnScene = "M1_Personnalisation";

        [Header("Tete complete (M1_FaceServer sur le PC, meme Wi-Fi)")]
        [SerializeField] TMP_InputField serverField;   // adresse du serveur, retenue sur le telephone
        [SerializeField] string defaultServer = "http://192.168.1.10:8000";
        [SerializeField] float serverTimeout = 120f;
        [Tooltip("La camera frontale s'affiche en miroir : le serveur remet les photos a l'endroit")]
        [SerializeField] bool mirroredCamera = true;

        const string ServerKey = "DecoAR.HeadServer";

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

            if (serverField != null)
            {
                serverField.text = PlayerPrefs.GetString(ServerKey, defaultServer);
                serverField.onEndEdit.AddListener(s =>
                {
                    serverField.text = NormalizeServer(s);
                    PlayerPrefs.SetString(ServerKey, serverField.text);
                    PlayerPrefs.Save();
                });
            }
        }

        string ServerUrl => NormalizeServer(PlayerPrefs.GetString(ServerKey, defaultServer));

        // M1_FaceServer ne parle qu'en HTTP : "https://", "192.168.1.10" ou "192.168.1.10:8000" -> "http://ip:8000"
        static string NormalizeServer(string s)
        {
            s = (s ?? "").Trim().TrimEnd('/');
            if (s.Length == 0) return s;
            int scheme = s.IndexOf("://", System.StringComparison.Ordinal);
            if (scheme >= 0) s = s.Substring(scheme + 3);
            if (s.IndexOf(':') < 0) s += ":8000";
            return "http://" + s;
        }

        IEnumerator Begin()
        {
            introGroup.SetActive(false);
            // Premier contact avec le serveur : sur iPhone, c'est ce qui affiche la demande "Reseau local",
            // ainsi l'autorisation est deja donnee quand les photos partent
            if (!string.IsNullOrEmpty(ServerUrl)) UnityWebRequest.Get(ServerUrl + "/").SendWebRequest();
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

            ARFace face = TrackedFace();
            bool ready = IsUsable(face);
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
            if (ready && stableTime >= requiredTime) StartCoroutine(Scan(face));
        }

        ARFace TrackedFace()
        {
            foreach (var f in faceManager.trackables)
                if (f.trackingState == TrackingState.Tracking) return f;
            return null;
        }

        static bool IsUsable(ARFace f) =>
            f != null && f.vertices.IsCreated && f.vertices.Length > 100 && f.indices.IsCreated;

        // Une prise de vue : image camera + position a l'ecran et visibilite de chaque point du visage
        class View
        {
            public Color32[] pixels;
            public int width, height;
            public Vector2[] screen;
            public float[] weight;          // 1 = surface vue de face, 0 = vue de profil ou cachee
            public Vector3 gain = Vector3.one; // harmonise l'exposition avec la vue de face
            public byte[] jpeg;             // photo envoyee au serveur de tete 3D
        }

        // Face, puis chaque cote : les joues et les tempes sont texturees depuis l'angle ou on les voit le mieux
        IEnumerator Scan(ARFace face)
        {
            capturing = true;

            var vertices = face.vertices.ToArray();
            var triangles = face.indices.ToArray();
            Vector3 toCamera = face.transform.InverseTransformPoint(arCamera.transform.position).normalized;
            Vector3[] normals = LocalNormals(vertices, triangles, toCamera);

            var views = new List<View>();
            status.text = "Capture de face…";
            yield return CaptureView(face, normals, views);

            float firstSide = 0f;
            for (int step = 0; step < 2; step++)
            {
                bool reached = false;
                for (float t = 0f; t < sideTimeout; t += Time.deltaTime)
                {
                    status.text = step == 0
                        ? "Tourne lentement la tête d'un côté…"
                        : "Maintenant, tourne-la lentement de l'autre côté…";

                    var f = TrackedFace();
                    if (IsUsable(f))
                    {
                        float yaw = Yaw(f, toCamera);
                        float toward = step == 0 ? Mathf.Abs(yaw) : -yaw * firstSide;
                        progressFill.fillAmount = Mathf.Clamp01(toward / sideAngle);
                        if (toward >= sideAngle)
                        {
                            if (step == 0) firstSide = Mathf.Sign(yaw);
                            face = f;
                            reached = true;
                            break;
                        }
                    }
                    yield return null;
                }
                if (!reached) break; // on garde les vues deja prises

                status.text = "Capture…";
                yield return CaptureView(face, normals, views);
            }

            // Tete complete (crane, oreilles, cou) creee par le serveur a partir des photos
            status.text = "Création de ta tête 3D…";
            progressFill.fillAmount = 0f;
            bool headOk = false;
            yield return SendToServer(views, ok => headOk = ok);
            if (headOk)
            {
                HeadStore.TryLoad(out var head, out _, out _);
                FaceMaskStore.Delete(); // la tete complete remplace les anciens scans
                FaceStore.Delete();
                var gmHead = GameManager.Instance;
                var lookHead = gmHead.Look;
                lookHead.visagePhoto = true;
                lookHead.Set(LookSlot.Peau, SkinTone.NearestSkin(profile, head.Skin));
                lookHead.Set(LookSlot.Cheveux, SkinTone.NearestHair(profile, head.Hair));
                gmHead.SetLook(lookHead);

                HapticManager.Vibrate(HapticIntensity.Moyenne);
                status.text = "Tête 3D créée !";
                progressFill.fillAmount = 1f;
                yield return new WaitForSeconds(0.7f);
                Return();
                yield break;
            }

            // Sans serveur : visage seul, calcule sur le telephone
            status.text = "Création de ton visage 3D…";
            yield return null;

            bool ok = Build(vertices, triangles, toCamera, views, out var data, out var texture,
                out var skinTint, out var hairTint, out bool hasHair);

            if (!ok)
            {
                status.text = "Scan incomplet, réessaie face à la caméra.";
                stableTime = 0f;
                capturing = false;
                yield break;
            }

            FaceMaskStore.Save(data, texture);
            FaceStore.Delete(); // le visage 3D remplace un eventuel scan photo
            HeadStore.Delete();

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

        // Envoie les photos (face puis cotes) a M1_FaceServer et enregistre la tete renvoyee
        IEnumerator SendToServer(List<View> views, System.Action<bool> done)
        {
            string url = ServerUrl;
            if (string.IsNullOrEmpty(url)) { done(false); yield break; }

            string[] names = { "front", "left", "right" };
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var form = new List<IMultipartFormSection>();
                for (int i = 0; i < views.Count && i < names.Length; i++)
                    form.Add(new MultipartFormFileSection(names[i], views[i].jpeg, names[i] + ".jpg", "image/jpeg"));
                form.Add(new MultipartFormDataSection("mirrored", mirroredCamera ? "true" : "false"));

                using (var request = UnityWebRequest.Post(url + "/head", form))
                {
                    request.timeout = Mathf.CeilToInt(serverTimeout);
                    var op = request.SendWebRequest();
                    float t = 0f;
                    while (!op.isDone)
                    {
                        t += Time.unscaledDeltaTime;
                        // Le serveur met environ une minute : la barre avance pour montrer que ca travaille
                        progressFill.fillAmount = Mathf.Clamp01(t / 60f) * 0.95f;
                        status.text = t < 3f ? "Envoi des photos au serveur…" : "Création de ta tête 3D… " + Mathf.FloorToInt(t) + " s";
                        yield return null;
                    }

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning($"[M1] Serveur de tete 3D injoignable ({url}) : {request.error}");
                        // Refus immediat : souvent la demande "Reseau local" d'iOS encore a l'ecran, on reessaie
                        if (attempt == 0 && t < 3f)
                        {
                            status.text = "Autorise l'accès au réseau local si ton téléphone le demande…";
                            yield return new WaitForSeconds(4f);
                            continue;
                        }
                        status.text = "Serveur injoignable : visage calculé sur le téléphone.";
                        yield return new WaitForSeconds(1.2f);
                        done(false);
                        yield break;
                    }
                    bool saved = HeadStore.Save(request.downloadHandler.text);
                    if (!saved) Debug.LogWarning("[M1] Reponse du serveur de tete 3D incomplete");
                    done(saved);
                    yield break;
                }
            }
            done(false);
        }

        IEnumerator CaptureView(ARFace face, Vector3[] normals, List<View> views)
        {
            // L'image capturee ne doit contenir que la camera : on cache l'UI et le maillage de suivi
            var visual = face.GetComponent<Renderer>();
            if (visual != null) visual.enabled = false;
            canvas.enabled = false;
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();

            // Maillage et projection a l'ecran au meme instant que l'image
            var verts = face.vertices;
            int n = Mathf.Min(normals.Length, verts.Length);
            var view = new View
            {
                pixels = shot.GetPixels32(),
                width = shot.width,
                height = shot.height,
                jpeg = shot.EncodeToJPG(92),
                screen = new Vector2[normals.Length],
                weight = new float[normals.Length],
            };
            Vector3 cam = arCamera.transform.position;
            for (int i = 0; i < n; i++)
            {
                Vector3 wp = face.transform.TransformPoint(verts[i]);
                Vector3 sp = arCamera.WorldToScreenPoint(wp);
                view.screen[i] = new Vector2(sp.x / view.width, sp.y / view.height);
                bool onScreen = sp.z > 0f && view.screen[i].x >= 0f && view.screen[i].x <= 1f
                                && view.screen[i].y >= 0f && view.screen[i].y <= 1f;
                float facing = Vector3.Dot(face.transform.TransformDirection(normals[i]), (cam - wp).normalized);
                view.weight[i] = onScreen ? Mathf.Clamp01(facing) : 0f;
            }
            Destroy(shot);

            canvas.enabled = true;
            if (visual != null) visual.enabled = true;
            views.Add(view);
        }

        // Rotation gauche / droite de la tete par rapport a la vue de face, en degres
        float Yaw(ARFace f, Vector3 frontLocal)
        {
            Vector3 up = arCamera.transform.up;
            Vector3 facing = Vector3.ProjectOnPlane(f.transform.TransformDirection(frontLocal), up);
            Vector3 toCam = Vector3.ProjectOnPlane(arCamera.transform.position - f.transform.position, up);
            return Vector3.SignedAngle(toCam, facing, up);
        }

        // Normales du maillage, orientees vers l'exterieur du visage
        static Vector3[] LocalNormals(Vector3[] v, int[] tris, Vector3 front)
        {
            var n = new Vector3[v.Length];
            for (int t = 0; t + 2 < tris.Length; t += 3)
            {
                int a = tris[t], b = tris[t + 1], c = tris[t + 2];
                Vector3 fn = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
                n[a] += fn; n[b] += fn; n[c] += fn;
            }
            float sum = 0f;
            for (int i = 0; i < n.Length; i++)
            {
                n[i] = n[i].normalized;
                sum += Vector3.Dot(n[i], front);
            }
            if (sum < 0f)
                for (int i = 0; i < n.Length; i++) n[i] = -n[i];
            return n;
        }

        // Repere normalise du visage, couleurs, et texture du masque
        bool Build(Vector3[] v, int[] tris, Vector3 front, List<View> views,
            out FaceMaskData data, out Texture2D texture, out Color skinTint, out Color hairTint, out bool hasHair)
        {
            var front0 = views[0];
            var screen = front0.screen;
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
            float width = FaceWidth(c, chin, height, c[nose].z);

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
                alpha[i] = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.86f, 1f, Mathf.Sqrt(ex * ex + ey * ey)));
            }

            var src = front0.pixels;
            int sw = front0.width, sh = front0.height;

            // Peau : joues ; cheveux : au-dessus du front, pixels plus sombres que la peau
            var skinSamples = new List<Color>();
            for (int i = 0; i < n; i++)
            {
                float ax = Mathf.Abs(c[i].x), y = c[i].y + height * 0.5f; // y depuis le menton
                if (ax > 0.03f && ax < 0.06f && y > height * 0.3f && y < height * 0.5f)
                    skinSamples.Add(Sample(src, sw, sh, screen[i]));
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
                Color s = Sample(src, sw, sh, screen[browIndex] + screenUp * (0.06f * j) + side);
                if (SkinTone.Lum(s) < skinLum * 0.75f) hairSamples.Add(s);
            }
            hasHair = hairSamples.Count >= 6;

            skinTint = SkinTone.NearestSkin(profile, skin);
            if (hasHair) hairTint = SkinTone.NearestHair(profile, Average(hairSamples));
            Vector3 gain = SkinTone.Gain(skin, SkinTone.Mul(profile.textureSkin, skinTint));

            MatchExposure(views, src, sw, sh);
            texture = Bake(views, uv, alpha, tris, gain, textureSize);
            data = new FaceMaskData { vertices = c, triangles = tris, uv = uv, height = height, width = width };
            return true;
        }

        // Largeur du visage au niveau des pommettes (meme mesure que sur le personnage)
        static float FaceWidth(Vector3[] c, float chin, float height, float noseZ)
        {
            float min = float.MaxValue, max = float.MinValue;
            foreach (var p in c)
            {
                float y = p.y - chin;
                if (p.z < noseZ - 0.06f || y < height * 0.4f || y > height * 0.6f) continue;
                min = Mathf.Min(min, p.x);
                max = Mathf.Max(max, p.x);
            }
            return max > min ? max - min : 0f;
        }

        // L'exposition automatique de la camera change entre les prises : on recale chaque vue de cote
        // sur la vue de face, en comparant les zones vues correctement dans les deux
        static void MatchExposure(List<View> views, Color32[] src, int sw, int sh)
        {
            var front = views[0];
            for (int k = 1; k < views.Count; k++)
            {
                var view = views[k];
                Vector3 a = Vector3.zero, b = Vector3.zero;
                int count = 0;
                for (int i = 0; i < front.weight.Length; i++)
                {
                    if (front.weight[i] < 0.5f || view.weight[i] < 0.5f) continue;
                    Color cf = Sample(src, sw, sh, front.screen[i]);
                    Color cv = Sample(view.pixels, view.width, view.height, view.screen[i]);
                    a += new Vector3(cf.r, cf.g, cf.b);
                    b += new Vector3(cv.r, cv.g, cv.b);
                    count++;
                }
                if (count < 20) continue;
                view.gain = new Vector3(
                    Mathf.Clamp(a.x / Mathf.Max(b.x, 1e-3f), 0.75f, 1.33f),
                    Mathf.Clamp(a.y / Mathf.Max(b.y, 1e-3f), 0.75f, 1.33f),
                    Mathf.Clamp(a.z / Mathf.Max(b.z, 1e-3f), 0.75f, 1.33f));
            }
        }

        // Rasterise chaque triangle dans la texture du masque. Chaque pixel melange les vues,
        // ponderees par l'angle sous lequel la camera voyait cette zone (la vue la plus frontale domine).
        static Texture2D Bake(List<View> views, Vector2[] uv, float[] alpha, int[] tris, Vector3 gain, int size)
        {
            var dst = new Color32[size * size];
            var front = views[0];
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

                    Color acc = Color.clear;
                    float total = 0f;
                    foreach (var view in views)
                    {
                        float w = view.weight[a] * w0 + view.weight[b] * w1 + view.weight[c] * w2;
                        if (w < 0.2f) continue;
                        w *= w; w *= w; // puissance 4 : la vue la mieux orientee l'emporte nettement
                        Color s = Sample(view.pixels, view.width, view.height,
                            view.screen[a] * w0 + view.screen[b] * w1 + view.screen[c] * w2);
                        acc += new Color(s.r * view.gain.x, s.g * view.gain.y, s.b * view.gain.z) * w;
                        total += w;
                    }
                    Color col = total > 0f
                        ? acc / total
                        : Sample(front.pixels, front.width, front.height,
                            front.screen[a] * w0 + front.screen[b] * w1 + front.screen[c] * w2);

                    float al = alpha[a] * w0 + alpha[b] * w1 + alpha[c] * w2;
                    dst[y * size + x] = new Color32(
                        ToByte(col.r * gain.x), ToByte(col.g * gain.y), ToByte(col.b * gain.z), ToByte(al));
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
