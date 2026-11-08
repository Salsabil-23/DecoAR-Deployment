using System.Collections;
using DecoAR.Auth;
using DecoAR.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace DecoAR.M5
{
    /// Poste d'administration derrière la caisse : l'écran du PC affiche une veille DecoAR. Toucher le PC fait
    /// voler la caméra jusqu'à l'écran, qui montre le compte connecté. Réservé aux comptes admin (rôle donné sur le
    /// serveur) : l'admin confirme son identité avec le défi de reconnaissance faciale (AuthOverlay.ConfirmAdmin),
    /// le serveur lui donne un jeton admin, puis l'espace admin (M6) s'ouvre.
    public class M5AdminTerminal : MonoBehaviour
    {
        [Tooltip("Centre de la dalle : forward sort de l'écran vers l'utilisateur, up = haut de l'écran.")]
        public Transform screen;
        public Vector2 screenSize = new Vector2(0.33f, 0.2f);
        public Texture2D logo;
        [Tooltip("Symbole seul (sans fond), posé dans le rond de l'avatar.")]
        public Texture2D logoMark;
        public float maxTapDistance = 12f;
        public float hintDistance = 6.5f;
        public float flyDuration = 1.6f;

        const float CanvasWidth = 1200f;
        static readonly Color Night = new Color(0.10f, 0.08f, 0.07f);
        static readonly Color Error = new Color(0.90f, 0.42f, 0.36f);

        enum State { Veille, Vol, Connexion, Ouverture, Retour }
        State state = State.Veille;

        Camera cam;
        Transform camParent;
        Vector3 camLocalPos; Quaternion camLocalRot; float camFov;
        M5PlayerController player;
        M5ThirdPerson thirdPerson;
        CanvasGroup hud;

        RectTransform screenRoot, lockPanel, loginPanel, successPanel, fieldBox, hint, progress;
        CanvasGroup lockGroup, loginGroup, successGroup, hintGroup, fadeGroup, wakeGroup;
        TextMeshProUGUI clock, date, errorText, tapText, accountText, unlockLabel;
        bool confirming;
        Image checkCircle, checkIcon;
        float errorUntil, shake;
        Vector2 pressPos; float pressTime; bool pressing, pressOverUI;

        void OnEnable() => EnhancedTouchSupport.Enable();

        void Start()
        {
            cam = Camera.main;
            player = FindAnyObjectByType<M5PlayerController>();
            thirdPerson = player ? player.GetComponent<M5ThirdPerson>() : null;
            BuildScreen();
            BuildHint();
            BuildFade();
        }

        // ------------------------------------------------------------ boucle

        void Update()
        {
            float dt = Time.unscaledDeltaTime, k = 1f - Mathf.Exp(-10f * dt);
            if (clock) { var now = System.DateTime.Now; clock.text = now.ToString("HH:mm"); date.text = Capitalize(now.ToString("dddd d MMMM", new System.Globalization.CultureInfo("fr-FR"))); }
            tapText.alpha = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 2.4f);

            // Bulle « Espace admin » quand on s'approche
            bool near = state == State.Veille && cam && Vector3.Distance(cam.transform.position, screen.position) < hintDistance;
            hintGroup.alpha = Mathf.Lerp(hintGroup.alpha, near ? 1f : 0f, k);
            if (cam)
            {
                hint.position = screen.position + Vector3.up * (screenSize.y * 0.9f + 0.1f + 0.03f * Mathf.Sin(Time.unscaledTime * 2f));
                hint.rotation = Quaternion.LookRotation(hint.position - cam.transform.position);
            }

            if (state == State.Veille) DetectTap();
            if (state == State.Connexion)
            {
                var kb = Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame) Leave();
            }

            // Champ secoué après une erreur
            shake = Mathf.MoveTowards(shake, 0f, dt * 2.2f);
            if (fieldBox) fieldBox.anchoredPosition = new Vector2(Mathf.Sin(shake * 40f) * 26f * shake, fieldBox.anchoredPosition.y);
            errorText.alpha = Mathf.Lerp(errorText.alpha, Time.unscaledTime < errorUntil ? 1f : 0f, k);
        }

        void DetectTap()
        {
            Vector2 pos = default; bool down = false, any = false;
            if (ETouch.activeTouches.Count > 0) { pos = ETouch.activeTouches[0].screenPosition; down = true; any = true; }
            else if (Mouse.current != null) { pos = Mouse.current.position.ReadValue(); down = Mouse.current.leftButton.isPressed; any = true; }
            if (!any) return;
            if (down && !pressing) { pressing = true; pressPos = pos; pressTime = Time.unscaledTime; pressOverUI = OverUI(pos); }
            else if (!down && pressing)
            {
                pressing = false;
                if (pressOverUI || (pos - pressPos).magnitude > 24f || Time.unscaledTime - pressTime > 0.45f) return;
                var ray = cam.ScreenPointToRay(pos);
                if (Physics.Raycast(ray, out var hit, maxTapDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide) && hit.collider.transform.IsChildOf(transform))
                    StartCoroutine(Enter());
            }
        }

        static bool OverUI(Vector2 p)
        {
            var es = EventSystem.current;
            if (!es) return false;
            var hits = new System.Collections.Generic.List<RaycastResult>();
            es.RaycastAll(new PointerEventData(es) { position = p }, hits);
            foreach (var h in hits) if (h.module is GraphicRaycaster gr && gr.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay) return true;
            return false;
        }

        // ------------------------------------------------------------ vol de la caméra

        IEnumerator Enter()
        {
            state = State.Vol;
            M5Haptics.Light();
            if (player) player.enabled = false;
            if (thirdPerson) thirdPerson.enabled = false;
            SetHud(false);

            camParent = cam.transform.parent;
            camLocalPos = cam.transform.localPosition; camLocalRot = cam.transform.localRotation; camFov = cam.fieldOfView;
            cam.transform.SetParent(null, true);

            var (pos, rot) = ViewPose(40f);
            StartCoroutine(Wake());
            yield return Fly(cam.transform.position, cam.transform.rotation, camFov, pos, rot, 40f, flyDuration);

            state = State.Connexion;
            RefreshAccount();
        }

        /// Compte connecté affiché sur l'écran du PC, et texte du bouton selon ses droits.
        void RefreshAccount()
        {
            if (!AuthSession.IsLoggedIn)
            {
                accountText.text = "<color=#C9B8A6>Aucun compte connecté</color>";
                unlockLabel.text = "<b>Réservé aux administrateurs</b>";
            }
            else
            {
                var u = AuthSession.User;
                accountText.text = AuthSession.IsAdmin
                    ? $"{u.firstName} {u.lastName}  ·  <color=#E0B867>admin</color>"
                    : $"{u.firstName} {u.lastName}  ·  <color=#C9B8A6>client</color>";
                unlockLabel.text = AuthSession.IsAdmin ? "<b>Confirmer avec mon visage</b>" : "<b>Réservé aux administrateurs</b>";
            }
        }

        void Leave()
        {
            if (state != State.Connexion) return;
            StartCoroutine(Exit());
        }

        IEnumerator Exit()
        {
            state = State.Retour;
            StartCoroutine(Sleep());
            var back = camParent != null ? camParent.TransformPoint(camLocalPos) : cam.transform.position;
            var backRot = camParent != null ? camParent.rotation * camLocalRot : cam.transform.rotation;
            yield return Fly(cam.transform.position, cam.transform.rotation, cam.fieldOfView, back, backRot, camFov, flyDuration * 0.8f);
            cam.transform.SetParent(camParent, false);
            cam.transform.localPosition = camLocalPos; cam.transform.localRotation = camLocalRot; cam.fieldOfView = camFov;
            if (player) player.enabled = true;
            if (thirdPerson) thirdPerson.enabled = true;
            SetHud(true);
            state = State.Veille;
        }

        /// Position où l'écran remplit le champ de vision (avec une marge), face à la dalle.
        (Vector3, Quaternion) ViewPose(float fov)
        {
            float v = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float h = Mathf.Atan(v * cam.aspect);
            float d = Mathf.Max(screenSize.y * 0.5f / 0.88f / v, screenSize.x * 0.5f / 0.9f / Mathf.Tan(h));
            return (screen.position + screen.forward * d, Quaternion.LookRotation(-screen.forward, screen.up));
        }

        /// Trajectoire en arc, accélération et freinage doux, légère rotation en avance.
        IEnumerator Fly(Vector3 p0, Quaternion r0, float f0, Vector3 p2, Quaternion r2, float f2, float duration)
        {
            var p1 = (p0 + p2) * 0.5f + Vector3.up * Mathf.Min(0.35f, Vector3.Distance(p0, p2) * 0.12f);
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float x = t / duration;
                float e = x * x * x * (x * (x * 6f - 15f) + 10f);                // très doux au départ et à l'arrivée
                float er = 1f - Mathf.Pow(1f - Mathf.Clamp01(x * 1.25f), 3f);     // le regard se tourne un peu avant
                float u = 1f - e;
                cam.transform.position = u * u * p0 + 2f * u * e * p1 + e * e * p2;
                cam.transform.rotation = Quaternion.Slerp(r0, r2, er);
                cam.fieldOfView = Mathf.Lerp(f0, f2, e);
                yield return null;
            }
            cam.transform.SetPositionAndRotation(p2, r2);
            cam.fieldOfView = f2;
        }

        void SetHud(bool on)
        {
            if (hud == null)
            {
                var ui = FindAnyObjectByType<M5CheckoutUI>();
                var canvas = ui ? ui.GetComponentInChildren<Canvas>() : null;
                if (canvas)
                {
                    hud = canvas.GetComponent<CanvasGroup>();
                    if (hud == null) hud = canvas.gameObject.AddComponent<CanvasGroup>();
                }
            }
            if (hud == null) return;
            StartCoroutine(FadeGroup(hud, on ? 1f : 0f, 0.35f));
            hud.blocksRaycasts = hud.interactable = on;
        }

        // ------------------------------------------------------------ écran

        IEnumerator Wake()
        {
            loginPanel.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(flyDuration * 0.35f);
            StartCoroutine(FadeGroup(wakeGroup, 1f, 0.5f));
            StartCoroutine(FadeGroup(lockGroup, 0f, 0.5f));
            yield return new WaitForSecondsRealtime(0.25f);
            // la fenêtre de connexion monte en se posant
            for (float t = 0; t < 0.6f; t += Time.unscaledDeltaTime)
            {
                float e = 1f - Mathf.Pow(1f - t / 0.6f, 3f);
                loginGroup.alpha = e;
                loginPanel.anchoredPosition = new Vector2(0, Mathf.Lerp(-60f, 0f, e));
                loginPanel.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, e);
                yield return null;
            }
            loginGroup.alpha = 1f; loginPanel.anchoredPosition = Vector2.zero; loginPanel.localScale = Vector3.one;
            loginGroup.interactable = loginGroup.blocksRaycasts = true;
        }

        IEnumerator Sleep()
        {
            loginGroup.interactable = loginGroup.blocksRaycasts = false;
            StartCoroutine(FadeGroup(loginGroup, 0f, 0.3f));
            StartCoroutine(FadeGroup(wakeGroup, 0f, 0.6f));
            yield return FadeGroup(lockGroup, 1f, 0.6f);
        }

        void TryUnlock()
        {
            if (state != State.Connexion || confirming) return;
            if (!AuthSession.IsLoggedIn) { Refuse("Connecte-toi avec un compte administrateur"); return; }
            if (!AuthSession.IsAdmin) { Refuse("Accès réservé aux administrateurs"); return; }
            if (AuthOverlay.Instance == null) { Refuse("Confirmation indisponible, relance l'application"); return; }

            // Défi de reconnaissance faciale par-dessus la boutique ; le serveur vérifie le visage du compte admin
            confirming = true;
            AuthOverlay.Instance.ConfirmAdmin(ok =>
            {
                confirming = false;
                if (ok) StartCoroutine(Unlock());
                else Refuse("Identité non confirmée");
            });
        }

        void Refuse(string message)
        {
            shake = 1f;
            errorUntil = Time.unscaledTime + 2.5f;
            errorText.text = message;
            M5Haptics.Strong();
        }

        IEnumerator Unlock()
        {
            state = State.Ouverture;
            M5Haptics.Light();
            loginGroup.interactable = loginGroup.blocksRaycasts = false;
            StartCoroutine(FadeGroup(loginGroup, 0f, 0.25f));
            successPanel.gameObject.SetActive(true);
            successGroup.alpha = 0f;
            StartCoroutine(FadeGroup(successGroup, 1f, 0.3f));

            // Coche : le cercle grossit avec un léger rebond, puis la coche apparaît
            for (float t = 0; t < 0.55f; t += Time.unscaledDeltaTime)
            {
                float x = t / 0.55f;
                float s = 1f + 0.18f * Mathf.Sin(x * Mathf.PI) * (1f - x);
                checkCircle.rectTransform.localScale = Vector3.one * Mathf.Min(1f, x * 1.6f) * s;
                checkIcon.color = new Color(1, 1, 1, Mathf.Clamp01((x - 0.4f) * 2.5f));
                yield return null;
            }
            checkCircle.rectTransform.localScale = Vector3.one;
            checkIcon.color = Color.white;

            // Chargement de l'espace administration
            for (float t = 0; t < 0.9f; t += Time.unscaledDeltaTime)
            {
                float e = Mathf.SmoothStep(0f, 1f, t / 0.9f);
                progress.anchorMax = new Vector2(e, 1f);
                yield return null;
            }
            progress.anchorMax = Vector2.one;

            var gm = DecoAR.Core.GameManager.Instance;
            if (gm != null && player) gm.SaveShopPose(player.transform.position, player.transform.rotation); // retour au même endroit

            // L'écran remplit la vue, puis fondu
            var (pos, rot) = ViewPose(40f);
            var start = cam.transform.position;
            for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                float e = Mathf.SmoothStep(0f, 1f, t / 0.5f);
                cam.transform.position = Vector3.Lerp(start, screen.position + screen.forward * 0.05f, e * 0.75f);
                fadeGroup.alpha = e;
                yield return null;
            }
            fadeGroup.alpha = 1f;
            SceneManager.LoadScene("AdminScene");
        }

        static IEnumerator FadeGroup(CanvasGroup g, float to, float duration)
        {
            float from = g.alpha;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                g.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            g.alpha = to;
        }

        // ------------------------------------------------------------ construction

        void BuildScreen()
        {
            float hgt = CanvasWidth * screenSize.y / screenSize.x;
            var go = new GameObject("Ecran_Admin", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(screen, false);
            go.transform.localPosition = Vector3.forward * 0.0015f;
            go.transform.localRotation = Quaternion.Euler(0, 180, 0); // la face de l'UI regarde vers l'utilisateur
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 3f;
            screenRoot = (RectTransform)go.transform;
            screenRoot.sizeDelta = new Vector2(CanvasWidth, hgt);
            screenRoot.localScale = Vector3.one * (screenSize.x / CanvasWidth);

            // Fond : dégradé chaud + halo terracotta
            var bg = Raw(screenRoot, "Fond", Gradient());
            DecoUI.Stretch(bg.rectTransform);
            var glow = Raw(screenRoot, "Halo", Halo());
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.62f);
            glow.rectTransform.sizeDelta = new Vector2(CanvasWidth * 1.1f, CanvasWidth * 1.1f);
            glow.color = new Color(0.85f, 0.45f, 0.28f, 0.55f);
            wakeGroup = glow.gameObject.AddComponent<CanvasGroup>(); wakeGroup.alpha = 0.35f;

            // Veille : horloge, date, logo
            lockPanel = Panel("Veille");
            lockGroup = lockPanel.gameObject.AddComponent<CanvasGroup>();
            var lg = Raw(lockPanel, "Logo", logo);
            lg.rectTransform.anchorMin = lg.rectTransform.anchorMax = new Vector2(0.5f, 0.84f);
            lg.rectTransform.sizeDelta = Vector2.one * 92f;
            clock = Text(lockPanel, "", 200, DecoUI.Cream, new Vector2(0.5f, 0.56f), new Vector2(900, 220));
            clock.fontStyle = FontStyles.Bold; clock.characterSpacing = 2f;
            date = Text(lockPanel, "", 40, DecoUI.Soft, new Vector2(0.5f, 0.37f), new Vector2(900, 60));
            var badge = DecoUI.Box("Pastille", lockPanel, new Vector2(0.5f, 0.17f), new Vector2(0.5f, 0.17f), Vector2.zero, new Vector2(520, 76), DecoUI.Glass);
            badge.GetComponent<Image>().sprite = DecoUI.PillShape;
            tapText = Text(badge, "Espace administration  ·  touchez pour ouvrir", 30, DecoUI.Cream, new Vector2(0.5f, 0.5f), new Vector2(520, 76));

            // Connexion
            loginPanel = Panel("Connexion");
            loginGroup = loginPanel.gameObject.AddComponent<CanvasGroup>(); loginGroup.alpha = 0; loginGroup.interactable = loginGroup.blocksRaycasts = false;
            var card = DecoUI.Box("Carte", loginPanel, new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(620, 520), Color.clear);
            DecoUI.Glassy(card, new Color(0.12f, 0.10f, 0.08f, 0.82f), DecoUI.Rounded);
            var av = DecoUI.Box("Avatar", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -92), new Vector2(124, 124), Color.clear); // le symbole a sa propre plaque : pas de pastille derrière
            av.GetComponent<Image>().sprite = DecoUI.Circle; av.GetComponent<Image>().type = Image.Type.Simple;
            var avLogo = Raw(av, "Logo", logoMark != null ? logoMark : logo); DecoUI.Stretch(avLogo.rectTransform, 0f);
            Text(card, "<b>Administrateur</b>", 40, DecoUI.Cream, new Vector2(0.5f, 1f), new Vector2(560, 56), new Vector2(0, -190));
            Text(card, "Catalogue 3D de la boutique DecoAR", 24, DecoUI.Soft, new Vector2(0.5f, 1f), new Vector2(560, 36), new Vector2(0, -232));

            // Compte connecté (plus de mot de passe dans l'application : rôle admin vérifié par le serveur)
            fieldBox = DecoUI.Box("Compte", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -306), new Vector2(480, 74), new Color(1, 1, 1, 0.1f));
            fieldBox.GetComponent<Image>().sprite = DecoUI.PillShape;
            DecoUI.Layer(fieldBox, "Liseré", DecoUI.RingSprite, new Color(1, 1, 1, 0.22f), false);
            var lockIcon = DecoUI.Icon(fieldBox, M5Icons.Kind.Cadenas, 32, DecoUI.Soft);
            lockIcon.rectTransform.anchorMin = lockIcon.rectTransform.anchorMax = new Vector2(0, 0.5f);
            lockIcon.rectTransform.anchoredPosition = new Vector2(40, 0);
            accountText = DecoUI.Label(fieldBox, "", 26, DecoUI.Cream, TextAlignmentOptions.MidlineLeft);
            accountText.rectTransform.offsetMin = new Vector2(72, 0); accountText.rectTransform.offsetMax = new Vector2(-24, 0);
            accountText.textWrappingMode = TextWrappingModes.NoWrap;
            accountText.overflowMode = TextOverflowModes.Ellipsis;

            errorText = Text(card, "", 24, Error, new Vector2(0.5f, 1f), new Vector2(560, 34), new Vector2(0, -376));
            errorText.alpha = 0;
            var unlock = DecoUI.Pill(card, "<b>Confirmer avec mon visage</b>", DecoUI.Brass, DecoUI.Ink, TryUnlock, 70, 28);
            unlockLabel = unlock.GetComponentInChildren<TextMeshProUGUI>();
            var urt = (RectTransform)unlock.transform;
            urt.anchorMin = urt.anchorMax = new Vector2(0.5f, 0f); urt.pivot = new Vector2(0.5f, 0f);
            urt.anchoredPosition = new Vector2(0, 34); urt.sizeDelta = new Vector2(480, 70);

            var back = DecoUI.Pill(loginPanel, "‹  Retour à la boutique", new Color(1, 1, 1, 0f), DecoUI.Soft, Leave, 50, 24);
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.06f); brt.sizeDelta = new Vector2(420, 50);
            loginPanel.gameObject.SetActive(false);

            // Succès
            successPanel = Panel("Acces");
            successGroup = successPanel.gameObject.AddComponent<CanvasGroup>();
            var circle = DecoUI.Box("Cercle", successPanel, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(170, 170), DecoUI.Sage);
            checkCircle = circle.GetComponent<Image>(); checkCircle.sprite = DecoUI.Circle; checkCircle.type = Image.Type.Simple;
            checkIcon = DecoUI.Icon(circle, M5Icons.Kind.Chevron, 96, Color.white);
            checkIcon.sprite = Check();
            Text(successPanel, "<b>Accès autorisé</b>", 54, DecoUI.Cream, new Vector2(0.5f, 0.36f), new Vector2(900, 70));
            Text(successPanel, "Ouverture de l'espace administration…", 28, DecoUI.Soft, new Vector2(0.5f, 0.28f), new Vector2(900, 40));
            var bar = DecoUI.Box("Barre", successPanel, new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(420, 10), new Color(1, 1, 1, 0.12f));
            bar.GetComponent<Image>().sprite = DecoUI.PillShape;
            progress = DecoUI.Box("Progression", bar, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero, DecoUI.Brass);
            progress.GetComponent<Image>().sprite = DecoUI.PillShape;
            progress.offsetMin = progress.offsetMax = Vector2.zero;
            successPanel.gameObject.SetActive(false);
        }

        /// Bulle flottante au-dessus du PC.
        void BuildHint()
        {
            var go = new GameObject("Bulle_Admin", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 3f;
            hint = (RectTransform)go.transform;
            hint.sizeDelta = new Vector2(400, 84);
            hint.localScale = Vector3.one * 0.0021f;
            hintGroup = go.AddComponent<CanvasGroup>(); hintGroup.alpha = 0;
            var pill = DecoUI.Box("Pastille", hint, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, DecoUI.GlassStrong);
            pill.offsetMin = pill.offsetMax = Vector2.zero;
            pill.GetComponent<Image>().sprite = DecoUI.PillShape;
            var ic = DecoUI.Icon(pill, M5Icons.Kind.Cadenas, 40, DecoUI.Brass);
            ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0, 0.5f); ic.rectTransform.anchoredPosition = new Vector2(46, 0);
            var t = DecoUI.Label(pill, "<b>Espace admin</b>", 30, DecoUI.Cream, TextAlignmentOptions.MidlineLeft);
            t.rectTransform.offsetMin = new Vector2(82, 0); t.rectTransform.offsetMax = new Vector2(-20, 0);
        }

        void BuildFade()
        {
            var root = DecoUI.Canvas(transform, "Fondu_Admin", 200);
            fadeGroup = DecoUI.Fade(root);
            fadeGroup.alpha = 0;
        }

        RectTransform Panel(string name)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(screenRoot, false);
            DecoUI.Stretch(rt);
            return rt;
        }

        static TextMeshProUGUI Text(Transform parent, string text, float size, Color c, Vector2 anchor, Vector2 box, Vector2? pos = null)
        {
            var t = DecoUI.Label(parent, text, size, c, TextAlignmentOptions.Center);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor; rt.sizeDelta = box; rt.anchoredPosition = pos ?? Vector2.zero;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        static RawImage Raw(Transform parent, string name, Texture tex)
        {
            var img = new GameObject(name, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            img.transform.SetParent(parent, false);
            img.texture = tex; img.raycastTarget = false;
            return img;
        }

        static Texture2D Gradient()
        {
            var tex = new Texture2D(4, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var top = new Color(0.20f, 0.15f, 0.12f); var bottom = Night;
            for (int y = 0; y < 128; y++) for (int x = 0; x < 4; x++) tex.SetPixel(x, y, Color.Lerp(bottom, top, y / 127f));
            tex.Apply();
            return tex;
        }

        static Texture2D Halo()
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float a = Mathf.Clamp01(1f - d); a = a * a * a;
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            tex.Apply();
            return tex;
        }

        /// Coche dessinée par code.
        static Sprite Check()
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Vector2 a = new Vector2(0.22f, 0.52f), b = new Vector2(0.43f, 0.31f), c = new Vector2(0.8f, 0.7f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var p = new Vector2((x + 0.5f) / n, (y + 0.5f) / n);
                    float d = Mathf.Min(Seg(p, a, b), Seg(p, b, c)) - 0.055f;
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - d * n)));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        static float Seg(Vector2 p, Vector2 a, Vector2 b)
        {
            var pa = p - a; var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }
}
