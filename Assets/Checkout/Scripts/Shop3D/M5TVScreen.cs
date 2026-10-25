using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DecoAR.M5
{
    /// Écran de la télévision : en veille de loin, s'allume (effet cathodique + lueur) quand le joueur s'approche et affiche « Voir le catalogue ».
    /// Toucher l'écran allumé déclenche onCatalogRequested (branchement du catalogue à venir).
    /// L'objet est placé au centre de l'écran, son axe Z pointe DANS la télé.
    public class M5TVScreen : MonoBehaviour
    {
        [Header("Écran (m)")]
        public Vector2 screenSize = new Vector2(1.4f, 0.8f);
        public Material glowMaterial;
        [Header("Déclenchement")]
        public float activationRadius = 3.2f;
        [Header("Contenu")]
        public string title = "Voir le catalogue";
        public string subtitle = "Découvrez toute notre collection déco";
        public string callToAction = "Touchez l'écran";
        public Color colorA = new Color(0.36f, 0.50f, 0.40f);
        public Color colorB = new Color(0.80f, 0.60f, 0.30f);
        [Header("Lumière")]
        public float lightIntensity = 1.6f;
        public UnityEvent onCatalogRequested;

        public bool IsOn => on;

        const float W = 1600f;
        static readonly Color Cream = new Color(0.98f, 0.95f, 0.89f);

        Transform content;
        RectTransform screen, line, shimmer, cta, arrow;
        RawImage gradient;
        CanvasGroup contentGroup, standbyGroup;
        Image flash;
        TextMeshProUGUI titleText, subText, logoText;
        Renderer glow;
        Light spill;
        AudioSource audioSource;
        AudioClip powerOn, powerOff, tap;
        MaterialPropertyBlock mpb;
        readonly List<(RectTransform rt, float speed, float phase, float x)> motes = new List<(RectTransform, float, float, float)>();
        bool on;
        float switchTime = -10f, tapTime = -10f, messageUntil;
        float H => W * screenSize.y / Mathf.Max(screenSize.x, 0.01f);

        void Start()
        {
            // Nœud en mètres, indépendant de l'échelle de la télé
            content = new GameObject("M5_EcranContenu").transform;
            content.SetParent(transform, false);
            var s = transform.lossyScale;
            content.localScale = new Vector3(1f / Mathf.Max(s.x, 1e-4f), 1f / Mathf.Max(s.y, 1e-4f), 1f / Mathf.Max(s.z, 1e-4f));

            BuildGlow();
            BuildCanvas();
            BuildLight();
            BuildAudio();
            var box = gameObject.GetComponent<BoxCollider>();
            if (!box) box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(screenSize.x / Mathf.Max(s.x, 1e-4f), screenSize.y / Mathf.Max(s.y, 1e-4f), 0.04f / Mathf.Max(s.z, 1e-4f));
            box.isTrigger = true;
            ApplyState(99f); // démarre éteint
        }

        void Update()
        {
            var cam = Camera.main;
            if (cam)
            {
                var d = cam.transform.position - transform.position;
                float inFront = Vector3.Dot(d, -transform.forward);
                d.y = 0;
                bool want = d.magnitude < activationRadius && inFront > 0.1f;
                if (want != on)
                {
                    on = want; switchTime = Time.time;
                    if (audioSource) audioSource.PlayOneShot(on ? powerOn : powerOff, 0.5f);
                }
                if (on) DetectTap(cam);
            }
            ApplyState(Time.time - switchTime);
        }

        // ---------- Animation ----------
        void ApplyState(float e)
        {
            float t = Time.time;
            // Allumage : ligne horizontale → plein écran ; extinction : l'inverse, plus rapide
            float open, lineW;
            if (on) { lineW = Smooth(e / 0.16f); open = Smooth((e - 0.14f) / 0.28f); }
            else { open = 1f - Smooth(e / 0.18f); lineW = 1f - Smooth((e - 0.16f) / 0.14f); }

            contentGroup.alpha = open;
            screen.localScale = new Vector3(1f, Mathf.Lerp(0.012f, 1f, open), 1f);
            line.gameObject.SetActive(lineW > 0.001f && open < 0.999f);
            line.localScale = new Vector3(lineW, 1f, 1f);
            flash.color = new Color(1, 1, 1, on ? Mathf.Clamp01(1f - (e - 0.3f) / 0.35f) * open * 0.85f : 0f);
            standbyGroup.alpha = Mathf.Lerp(standbyGroup.alpha, on ? 0f : 1f, Time.deltaTime * 6f);
            logoText.alpha = 0.18f + 0.1f * Mathf.Sin(t * 1.2f);

            if (open > 0.001f)
            {
                gradient.uvRect = new Rect(t * 0.04f, Mathf.Sin(t * 0.3f) * 0.08f, 1f, 1f);
                float pop = on ? 1f + 0.18f * (1f - Smooth((e - 0.35f) / 0.45f)) : 1f;
                titleText.rectTransform.localScale = Vector3.one * pop;
                cta.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(t * 4f) + (t - tapTime < 0.25f ? 0.08f : 0f));
                arrow.anchoredPosition = new Vector2(Mathf.Sin(t * 5f) * 8f, 0);
                float sp = Mathf.Repeat(t / 3.2f, 1f);
                shimmer.anchoredPosition = new Vector2(Mathf.Lerp(-W, W, sp), 0);
                foreach (var m in motes)
                {
                    float y = Mathf.Repeat(t * m.speed + m.phase, 1f);
                    m.rt.anchoredPosition = new Vector2(m.x + Mathf.Sin(t + m.phase * 6f) * 20f, Mathf.Lerp(-H * 0.55f, H * 0.55f, y));
                    m.rt.GetComponent<Image>().color = new Color(1, 1, 1, 0.35f * Mathf.Sin(y * Mathf.PI));
                }
                if (messageUntil > 0 && Time.time > messageUntil) { subText.text = subtitle; messageUntil = 0; }
            }

            float pulse = 1f + 0.12f * Mathf.Sin(t * 2.2f);
            float level = open * pulse + (t - tapTime < 0.3f ? 0.6f : 0f);
            if (glow)
            {
                var c = Color.Lerp(colorA, colorB, 0.5f + 0.5f * Mathf.Sin(t * 0.5f));
                glow.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", Color.Lerp(new Color(0.01f, 0.012f, 0.015f), c * 2.4f, level));
                glow.SetPropertyBlock(mpb);
            }
            if (spill) { spill.intensity = lightIntensity * level; spill.enabled = level > 0.01f; spill.color = Color.Lerp(colorA, Color.white, 0.45f); }
        }

        static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        // ---------- Toucher l'écran ----------
        void DetectTap(Camera cam)
        {
            Vector2? p = null;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) p = mouse.position.ReadValue();
            var ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.wasPressedThisFrame) p = ts.primaryTouch.position.ReadValue();
            if (p == null) return;
            var col = GetComponent<Collider>();
            if (!col.Raycast(cam.ScreenPointToRay(p.Value), out _, 50f)) return;
            tapTime = Time.time;
            if (audioSource) audioSource.PlayOneShot(tap, 0.6f);
            M5Haptics.Light();
            if (onCatalogRequested.GetPersistentEventCount() > 0 || HasRuntimeListeners) onCatalogRequested.Invoke();
            else ShowMessage("Le catalogue arrive bientôt !");
        }

        bool HasRuntimeListeners { get; set; }
        public void AddCatalogListener(UnityAction a) { onCatalogRequested.AddListener(a); HasRuntimeListeners = true; }

        public void ShowMessage(string msg, float duration = 2.5f)
        {
            subText.text = msg;
            messageUntil = Time.time + duration;
        }

        // ---------- Construction ----------
        void BuildGlow()
        {
            mpb = new MaterialPropertyBlock();
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Lueur";
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(content, false);
            q.transform.localPosition = new Vector3(0, 0, 0.002f);
            q.transform.localScale = new Vector3(screenSize.x, screenSize.y, 1f);
            glow = q.GetComponent<Renderer>();
            glow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (glowMaterial) glow.sharedMaterial = glowMaterial;
        }

        void BuildCanvas()
        {
            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
            cgo.transform.SetParent(content, false);
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var crt = (RectTransform)cgo.transform;
            crt.sizeDelta = new Vector2(W, H);
            crt.localScale = Vector3.one * (screenSize.x / W);
            crt.localPosition = Vector3.zero;
            cgo.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;

            var round = MakeRounded(64, 24);

            // Veille : reflet + logo discret
            standbyGroup = Group("Veille", crt).gameObject.AddComponent<CanvasGroup>();
            var refl = Img("Reflet", (RectTransform)standbyGroup.transform, Vector2.zero, new Vector2(W * 1.6f, H * 0.35f), new Color(1, 1, 1, 0.035f));
            refl.rectTransform.localRotation = Quaternion.Euler(0, 0, 28f);
            logoText = Txt("Logo", (RectTransform)standbyGroup.transform, "DecoAR", 120, new Color(1, 1, 1, 0.2f), new Vector2(W, 200), FontStyles.Bold);
            logoText.characterSpacing = 20;

            // Écran allumé
            screen = Group("Allume", crt);
            contentGroup = screen.gameObject.AddComponent<CanvasGroup>();
            gradient = new GameObject("Degrade", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            Fill(gradient.rectTransform, screen);
            gradient.texture = MakeGradient();
            gradient.color = new Color(1, 1, 1, 0.78f); // laisse passer la lueur HDR (bloom)
            gradient.raycastTarget = false;

            var masked = Group("Masque", screen);
            masked.gameObject.AddComponent<RectMask2D>();
            for (int i = 0; i < 14; i++)
            {
                var m = Img("Particule", masked, Vector2.zero, Vector2.one * Random.Range(8f, 20f), Color.white);
                m.sprite = round;
                motes.Add((m.rectTransform, Random.Range(0.05f, 0.12f), Random.value, Random.Range(-W * 0.48f, W * 0.48f)));
            }
            shimmer = Img("Brillance", masked, Vector2.zero, new Vector2(160, H * 2.2f), new Color(1, 1, 1, 0.12f)).rectTransform;
            shimmer.localRotation = Quaternion.Euler(0, 0, -22f);

            var frame = Img("Cadre", screen, Vector2.zero, new Vector2(W - 40, H - 40), new Color(1f, 0.93f, 0.75f, 0.55f));
            frame.sprite = MakeFrame(64, 24, 3); frame.type = Image.Type.Sliced;

            Txt("Marque", screen, "DECOAR  •  BOUTIQUE", 34, new Color(1, 1, 1, 0.75f), new Vector2(W, 60), FontStyles.Bold).rectTransform.anchoredPosition = new Vector2(0, H * 0.32f);
            titleText = Txt("Titre", screen, title, 150, Color.white, new Vector2(W * 0.9f, 200), FontStyles.Bold);
            titleText.rectTransform.anchoredPosition = new Vector2(0, H * 0.08f);
            titleText.enableAutoSizing = true; titleText.fontSizeMin = 40; titleText.fontSizeMax = 150;
            subText = Txt("SousTitre", screen, subtitle, 46, Cream, new Vector2(W * 0.85f, 80), FontStyles.Normal);
            subText.rectTransform.anchoredPosition = new Vector2(0, -H * 0.1f);

            cta = Img("Bouton", screen, new Vector2(0, -H * 0.3f), new Vector2(560, 104), new Color(1f, 0.97f, 0.9f, 0.95f)).rectTransform;
            var ctaImg = cta.GetComponent<Image>(); ctaImg.sprite = round; ctaImg.type = Image.Type.Sliced;
            var ctaText = Txt("Texte", cta, callToAction, 42, new Color(0.22f, 0.27f, 0.2f), new Vector2(440, 100), FontStyles.Bold);
            ctaText.rectTransform.anchoredPosition = new Vector2(-30, 0);
            var arrowHolder = Group("Fleche", cta); arrowHolder.anchoredPosition = new Vector2(210, 0); arrowHolder.sizeDelta = new Vector2(60, 100);
            arrow = Txt("Chevron", arrowHolder, ">", 70, new Color(0.22f, 0.27f, 0.2f), new Vector2(60, 100), FontStyles.Bold).rectTransform;

            flash = Img("Flash", screen, Vector2.zero, new Vector2(W, H), new Color(1, 1, 1, 0));
            line = Img("LigneAllumage", crt, Vector2.zero, new Vector2(W, 10), Color.white).rectTransform;
        }

        void BuildLight()
        {
            var go = new GameObject("Lumiere_Ecran");
            go.transform.SetParent(content, false);
            go.transform.localPosition = new Vector3(0, 0, -0.55f);
            spill = go.AddComponent<Light>();
            spill.type = LightType.Point; spill.range = 3.2f; spill.intensity = 0f; spill.shadows = LightShadows.None;
            spill.enabled = false;
        }

        void BuildAudio()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; audioSource.spatialBlend = 0.7f; audioSource.maxDistance = 12f;
            powerOn = Tone("Allumage", 0.35f, 180f, 900f, 0.5f);
            powerOff = Tone("Extinction", 0.22f, 700f, 120f, 0.4f);
            tap = Tone("Toucher", 0.09f, 1200f, 1500f, 0.5f);
        }

        /// Petit son synthétisé (glissando + fondu), aucun fichier audio nécessaire.
        static AudioClip Tone(string name, float dur, float f0, float f1, float vol)
        {
            const int rate = 44100;
            int n = Mathf.CeilToInt(dur * rate);
            var data = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)n;
                phase += 2 * System.Math.PI * Mathf.Lerp(f0, f1, k * k) / rate;
                float env = Mathf.Min(1f, k * 30f) * (1f - k) * (1f - k);
                data[i] = (float)(System.Math.Sin(phase) + 0.3 * System.Math.Sin(phase * 2)) * env * vol * 0.6f;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static RectTransform Group(string name, RectTransform parent)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            Fill(rt, parent);
            return rt;
        }

        static void Fill(RectTransform rt, RectTransform parent)
        {
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static Image Img(string name, RectTransform parent, Vector2 pos, Vector2 size, Color c)
        {
            var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            var rt = img.rectTransform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            img.color = c; img.raycastTarget = false;
            return img;
        }

        static TextMeshProUGUI Txt(string name, RectTransform parent, string text, float size, Color c, Vector2 box, FontStyles style)
        {
            var t = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            var rt = t.rectTransform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = box;
            t.text = text; t.fontSize = size; t.color = c; t.fontStyle = style;
            t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        Texture2D MakeGradient()
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Mirror };
            var px = new Color32[n * n];
            var deep = Color.Lerp(colorA, Color.black, 0.45f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (n - 1f), v = y / (n - 1f);
                    var c = Color.Lerp(Color.Lerp(deep, colorA, v), colorB, Mathf.Clamp01(u * 0.9f - v * 0.3f));
                    float r = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.55f));
                    c = Color.Lerp(c, Color.black, Mathf.Clamp01(r - 0.35f) * 0.6f); // vignette
                    c.a = 1f;
                    px[y * n + x] = c;
                }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        static Sprite MakeRounded(int size, int radius) => MakeFrame(size, radius, 0);

        /// Coin arrondi plein (thickness 0) ou contour (thickness > 0), en 9-slice.
        static Sprite MakeFrame(int size, int radius, int thickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius), cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = radius - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)); // distance au bord (>0 dedans)
                    float a = Mathf.Clamp01(d + 0.5f);
                    if (thickness > 0) a *= Mathf.Clamp01(thickness - d + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
    }
}
