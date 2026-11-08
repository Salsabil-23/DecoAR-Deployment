using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DecoAR.M6
{
    // Interface du module M6 (administration + generation 3D), construite au chargement des scenes d'Ali
    // sans les modifier : l'ancien Canvas est masque, la logique serveur (M6Controller) est conservee.
    //  - AdminScene : catalogue des produits 3D (voir en 3D, supprimer) + « Générer un modèle 3D »
    //  - M6_Generator : 1. photo  2. génération (suivi animé)  3. ajout au catalogue ; le modèle est
    //    présenté dans un grand cadre (socle, rotation, zoom) adapté au paysage comme au portrait.
    public class M6AdminUI : MonoBehaviour
    {
        /// Modele du catalogue a afficher en arrivant sur l'ecran de generation (« Voir en 3D »).
        static M6Catalog.Item pendingView;

        // Thème « Papier » du design system (docs/11-etats-et-ecrans.md : M6 quitte le fond brun) ;
        // la visionneuse reste sombre pour faire ressortir le modèle.
        static readonly Color Bg = DecoAR.Design.ThemePapier.SurfaceBackground;
        static readonly Color Card = DecoAR.Design.ThemePapier.SurfaceCard;
        static readonly Color Field = DecoAR.Design.ThemePapier.SurfaceSunken;
        static readonly Color Accent = DecoAR.Design.ThemePapier.ActionPrimary;
        static readonly Color Ink = DecoAR.Design.ThemePapier.TextPrimary;
        static readonly Color Muted = DecoAR.Design.ThemePapier.TextTertiary;
        static readonly Color Sage = DecoAR.Design.ThemePapier.ActionCommerce;
        static readonly Color Cream = DecoAR.Design.DecoPalette.Lin100;
        static readonly Color Line = DecoAR.Design.ThemePapier.BorderHairline;
        static readonly Color ViewerBg = DecoAR.Design.DecoPalette.Lin850;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded += (s, _) => Attach(s.name);
            Attach(SceneManager.GetActiveScene().name);
        }

        static void Attach(string scene)
        {
            if (scene != M6Navigation.AdminScene && scene != M6Navigation.GeneratorScene) return;
            foreach (var ui in FindObjectsByType<M6AdminUI>())
                if (ui.gameObject.scene == SceneManager.GetActiveScene()) return;
            new GameObject("M6_AdminUI").AddComponent<M6AdminUI>().Build(scene);
        }

        Sprite rounded, circle, ring;
        RectTransform root, topBar, panel, viewer, spinner, listContent;
        TextMeshProUGUI status, timer, viewerTitle, toast, countLabel;
        CanvasGroup toastGroup, emptyGroup, hintGroup, toolsGroup;
        Button generateButton, addButton;
        TextMeshProUGUI addLabel;
        TMP_InputField nameField;
        RawImage photo;
        AspectRatioFitter photoFit;
        GameObject photoEmpty;
        readonly List<(Button b, string cat)> chips = new List<(Button, string)>();
        string category = M6Catalog.Categories[0];
        M6Controller controller;
        M6ModelViewer modelViewer;
        bool generator, wasBusy, landscape, added;
        float busySince, toastUntil;

        void Build(string scene)
        {
            generator = scene == M6Navigation.GeneratorScene;
            rounded = MakeSprite(64, 20, 0); circle = MakeSprite(64, 32, 0); ring = MakeSprite(64, 32, 6);
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                if (go.GetComponent<Canvas>() != null && go != gameObject) go.SetActive(false); // ancienne interface

            var cgo = new GameObject("M6_Interface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 40;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            root = (RectTransform)cgo.transform;

            // Fond : une camera vide efface tout l'ecran, la camera du modele ne dessine que dans son cadre
            var bgCam = new GameObject("M6_Fond").AddComponent<Camera>();
            bgCam.clearFlags = CameraClearFlags.SolidColor; bgCam.backgroundColor = Bg; bgCam.cullingMask = 0; bgCam.depth = -10;

            BuildTopBar();
            if (generator) BuildGenerator(); else BuildAdmin();
            BuildToast();
            Layout(true);
        }

        // ------------------------------------------------------------ barre du haut

        void BuildTopBar()
        {
            topBar = Rect("Barre", root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), new Vector2(0, 120));
            string back = generator ? "‹ Administration" : "‹ Boutique";
            var b = Pill(topBar, back, Card, Ink, () =>
            {
                if (generator) SceneManager.LoadScene(M6Navigation.AdminScene);
                else SceneManager.LoadScene(DecoAR.Core.SceneNames.Shop); // retour devant le PC de la caisse
            }, 300, 76, 28);
            Place(b, new Vector2(0, 0.5f), new Vector2(36, 0), new Vector2(0, 0.5f));
            var logo = Box("Logo", topBar, DecoAR.Design.DecoBrand.SymbolSprite, Color.white, new Vector2(0, 0.5f), new Vector2(395, 0), new Vector2(84, 84));
            logo.GetComponent<Image>().type = Image.Type.Simple; logo.GetComponent<Image>().preserveAspect = true;
            var title = Label(topBar, generator ? "<b>Génération 3D</b>\n<size=55%><color=#6B5B4D>Une photo du produit › un modèle 3D › le catalogue</color></size>"
                                               : "<b>Administration</b>\n<size=55%><color=#6B5B4D>Catalogue des produits 3D de la boutique</color></size>", 40, Ink, TextAlignmentOptions.MidlineLeft);
            title.rectTransform.anchorMin = new Vector2(0, 0); title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.offsetMin = new Vector2(450, 0); title.rectTransform.offsetMax = new Vector2(-36, 0);
        }

        // ------------------------------------------------------------ génération

        void BuildGenerator()
        {
            controller = FindAnyObjectByType<M6Controller>();
            modelViewer = FindAnyObjectByType<M6ModelViewer>();

            // Cadre du modele (la camera y rend le modele)
            viewer = Rect("Cadre3D", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var corners = Box("Coins", viewer, MakeSprite(64, 32, 0, true), Bg, Vector2.zero, Vector2.zero, Vector2.zero, true);
            corners.GetComponent<Image>().raycastTarget = false;
            var frame = Box("Bordure", viewer, ring, new Color(0.82f, 0.65f, 0.32f, 0.6f), Vector2.zero, Vector2.zero, Vector2.zero, true);
            frame.GetComponent<Image>().type = Image.Type.Sliced; frame.GetComponent<Image>().raycastTarget = false;
            viewerTitle = Label(viewer, "", 30, Cream, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            if (modelViewer != null && modelViewer.viewerCamera != null) modelViewer.viewerCamera.backgroundColor = ViewerBg;
            Place(viewerTitle.rectTransform, new Vector2(0, 1), new Vector2(36, -30), new Vector2(0, 1), new Vector2(900, 50));
            var empty = Rect("Vide", viewer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 300));
            emptyGroup = empty.gameObject.AddComponent<CanvasGroup>(); emptyGroup.blocksRaycasts = false;
            // Illustration du design system (assets/etats-vides/modele-3d-attente)
            var cube = Box("Illustration", empty, Resources.Load<Sprite>("DecoAR/EtatsVides/modele-3d-attente"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(240, 180));
            cube.GetComponent<Image>().type = Image.Type.Simple; cube.GetComponent<Image>().preserveAspect = true;
            var et = Label(empty, "<b>Ton modèle 3D apparaîtra ici</b>\n<size=75%><color=#DDCDB5>Prends une photo du produit pour commencer</color></size>", 32, Cream, TextAlignmentOptions.Center);
            Place(et.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(0.5f, 0.5f), new Vector2(700, 100));

            var tools = Rect("Outils", viewer, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -30), new Vector2(0, 70));
            tools.pivot = new Vector2(1, 1);
            var h = tools.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 12; h.childAlignment = TextAnchor.UpperRight; h.childControlWidth = h.childControlHeight = false; h.childForceExpandWidth = h.childForceExpandHeight = false;
            tools.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            TextMeshProUGUI autoLabel = null;
            var auto = Pill(tools, "Rotation : oui", Card, Ink, () => { modelViewer.AutoRotate = !modelViewer.AutoRotate; autoLabel.text = modelViewer.AutoRotate ? "Rotation : oui" : "Rotation : non"; }, 240, 64, 24);
            autoLabel = auto.GetComponentInChildren<TextMeshProUGUI>();
            Pill(tools, "Recentrer", Card, Ink, () => modelViewer.ResetView(), 190, 64, 24);
            toolsGroup = tools.gameObject.AddComponent<CanvasGroup>(); toolsGroup.alpha = 0;

            var hint = Box("Aide", viewer, rounded, new Color(0.17f, 0.14f, 0.11f, 0.75f), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(820, 56));
            hint.GetComponent<Image>().raycastTarget = false;
            Label(hint, "Glisse pour tourner  ·  pince pour zoomer  ·  touche deux fois pour recentrer", 22, Cream, TextAlignmentOptions.Center);
            hintGroup = hint.gameObject.AddComponent<CanvasGroup>(); hintGroup.blocksRaycasts = false; hintGroup.alpha = 0;

            // Panneau des etapes
            panel = Box("Etapes", root, rounded, Card, Vector2.zero, Vector2.zero, Vector2.zero, true);
            Scroll(panel, out var body);

            Step(body, "1", "Photo du produit");
            var photoBox = Box("Photo", body, rounded, Field, Vector2.zero, Vector2.zero, Vector2.zero, true);
            photoBox.gameObject.AddComponent<LayoutElement>().preferredHeight = 260;
            photoBox.gameObject.AddComponent<RectMask2D>();
            photoEmpty = Label(photoBox, "<color=#8C8073>Aucune photo pour l'instant</color>", 24, Muted, TextAlignmentOptions.Center).gameObject;
            photo = new GameObject("Apercu", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter)).GetComponent<RawImage>();
            photo.rectTransform.SetParent(photoBox, false);
            photoFit = photo.GetComponent<AspectRatioFitter>(); photoFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            photo.gameObject.SetActive(false);
            var row = HRow(body, 84, 14);
            Pill(row, "Prendre une photo", Ink, Cream, () => controller.TakePhoto(), 0, 84, 26).GetComponent<LayoutElement>().flexibleWidth = 1;
            Pill(row, "Galerie", Field, Ink, () => controller.OpenGallery(), 0, 84, 26).GetComponent<LayoutElement>().flexibleWidth = 1;

            Step(body, "2", "Génération 3D");
            generateButton = Pill(body, "<b>Générer le modèle 3D</b>", Accent, Color.white, () => controller.Generate3D(), 0, 96, 30);
            var st = HRow(body, 70, 14);
            spinner = Box("Chargement", st, ring, new Color(0.78f, 0.45f, 0.29f, 0.25f), Vector2.zero, Vector2.zero, new Vector2(46, 46));
            spinner.GetComponent<Image>().type = Image.Type.Simple;
            var sl = spinner.gameObject.AddComponent<LayoutElement>(); sl.preferredWidth = sl.preferredHeight = 46;
            Box("Point", spinner, circle, Accent, new Vector2(0.5f, 1f), new Vector2(0, -6), new Vector2(14, 14)).GetComponent<Image>().type = Image.Type.Simple;
            status = Label(st, "", 23, Ink, TextAlignmentOptions.MidlineLeft);
            status.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            status.textWrappingMode = TextWrappingModes.Normal;
            timer = Label(st, "", 23, Muted, TextAlignmentOptions.MidlineRight);
            timer.gameObject.AddComponent<LayoutElement>().preferredWidth = 90;
#if UNITY_EDITOR
            Pill(body, "Charger le modèle de test du serveur (éditeur)", Field, Muted, () => controller.TestGLB(), 0, 60, 22);
#endif

            Step(body, "3", "Ajouter au catalogue");
            nameField = TextField(body, "Nom du produit (ex. Fauteuil Lin)");
            var grid = new GameObject("Categories", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement)).GetComponent<RectTransform>();
            grid.SetParent(body, false);
            var gl = grid.GetComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(170, 58); gl.spacing = new Vector2(10, 10); gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 3;
            grid.GetComponent<LayoutElement>().preferredHeight = 126;
            foreach (var c in M6Catalog.Categories)
            {
                string cat = c;
                chips.Add((Pill(grid, c, Field, Ink, () => { category = cat; RefreshChips(); }, 0, 58, 22), cat));
            }
            RefreshChips();
            addButton = Pill(body, "Ajouter au catalogue", Sage, Color.white, AddToCatalog, 0, 90, 28);
            addLabel = addButton.GetComponentInChildren<TextMeshProUGUI>();

            // La logique d'Ali ecrit dans nos elements
            if (controller != null)
            {
                controller.photoPreview = photo;
                controller.statusText = status;
                controller.generateButton = generateButton;
            }
            if (modelViewer != null) modelViewer.ModelLoaded += OnModelLoaded;

            // Arrivee depuis « Voir en 3D » du catalogue
            if (pendingView != null)
            {
                var item = pendingView; pendingView = null;
                nameField.text = item.name; category = item.category; RefreshChips();
                added = true;
                status.text = "Chargement du modèle…";
                StartCoroutine(M6Catalog.LoadModel(item, (path, error) =>
                {
                    if (error != null) { status.text = "Modèle indisponible : " + error; return; }
                    _ = modelViewer.LoadModel(path);
                }));
            }
        }

        void OnModelLoaded(string path)
        {
            viewerTitle.text = string.IsNullOrEmpty(nameField.text) ? "Modèle généré" : nameField.text;
            if (!added) { addButton.interactable = true; addLabel.text = "Ajouter au catalogue"; }
            else { addButton.interactable = false; addLabel.text = "Déjà dans le catalogue"; status.text = "Produit du catalogue"; }
        }

        void AddToCatalog()
        {
            if (modelViewer == null || !modelViewer.HasModel) { Toast("Génère d'abord un modèle 3D"); return; }
            if (string.IsNullOrWhiteSpace(nameField.text)) { Toast("Donne un nom au produit"); nameField.Select(); return; }
            addButton.interactable = false; addLabel.text = "Envoi au catalogue…";
            StartCoroutine(M6Catalog.Add(modelViewer.CurrentPath, nameField.text, category, (item, error) =>
            {
                if (error != null)
                {
                    addButton.interactable = true; addLabel.text = "Ajouter au catalogue";
                    Toast("Ajout impossible : " + error);
                    return;
                }
                added = true;
                addLabel.text = "Ajouté au catalogue";
                viewerTitle.text = item.name;
                Toast($"« {item.name} » ajouté au catalogue ({item.category})");
            }));
        }

        void RefreshChips()
        {
            foreach (var (b, cat) in chips)
            {
                bool on = cat == category;
                b.GetComponent<Image>().color = on ? Accent : Field;
                b.GetComponentInChildren<TextMeshProUGUI>().color = on ? Color.white : Ink;
            }
        }

        // ------------------------------------------------------------ administration

        void BuildAdmin()
        {
            var cam = Camera.main;
            if (cam != null) { cam.cullingMask = 0; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Bg; }

            panel = Box("Catalogue", root, rounded, Card, Vector2.zero, Vector2.zero, Vector2.zero, true);
            Column(panel, 16, 34);
            var head = HRow(panel, 96, 16);
            countLabel = Label(head, "", 34, Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            countLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Pill(head, "<b>+  Générer un modèle 3D</b>", Accent, Color.white, () => SceneManager.LoadScene(M6Navigation.GeneratorScene), 460, 86, 28);

            var cols = HRow(panel, 50, 16);
            cols.GetComponent<Image>().color = Field;
            foreach (var (t, w) in new[] { ("Produit", -1f), ("Catégorie", 220f), ("Ajouté le", 220f), ("", 380f) })
            {
                var l = Label(cols, $"<b>{t}</b>", 22, Muted, TextAlignmentOptions.MidlineLeft);
                var le = l.gameObject.AddComponent<LayoutElement>();
                if (w < 0) le.flexibleWidth = 1; else le.preferredWidth = w;
            }
            Scroll(panel, out listContent);
            RefreshList();
        }

        void RefreshList()
        {
            countLabel.text = "Chargement du catalogue…";
            StartCoroutine(M6Catalog.All(ShowList));
        }

        void ShowList(List<M6Catalog.Item> items, string error)
        {
            for (int i = listContent.childCount - 1; i >= 0; i--) Destroy(listContent.GetChild(i).gameObject);
            if (error != null)
            {
                countLabel.text = "Catalogue 3D";
                var err = Label(listContent, $"<b>Catalogue indisponible</b>\n<size=80%><color=#6B5B4D>{error}</color></size>", 30, Ink, TextAlignmentOptions.Center);
                err.gameObject.AddComponent<LayoutElement>().preferredHeight = 110;
                return;
            }
            countLabel.text = items.Count == 0 ? "Catalogue 3D" : $"{items.Count} produit{(items.Count > 1 ? "s" : "")} 3D";
            if (items.Count == 0)
            {
                // État vide du design system : illustration + titre + phrase
                var art = new GameObject("Illustration", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
                art.transform.SetParent(listContent, false);
                art.sprite = Resources.Load<Sprite>("DecoAR/EtatsVides/catalogue-3d-vide"); art.preserveAspect = true; art.raycastTarget = false;
                art.GetComponent<LayoutElement>().preferredHeight = 240;
                var e = Label(listContent, "<b>Aucun produit 3D pour l'instant</b>\n<size=80%><color=#6B5B4D>Génère ton premier modèle à partir d'une photo.</color></size>", 30, Ink, TextAlignmentOptions.Center);
                e.gameObject.AddComponent<LayoutElement>().preferredHeight = 110;
                return;
            }
            foreach (var item in items)
            {
                var it = item;
                var row = HRow(listContent, 96, 16);
                row.GetComponent<Image>().color = new Color(1, 1, 1, 1);
                var n = Label(row, $"<b>{it.name}</b>\n<size=70%><color=#8C8073>{Mathf.Max(1, it.modelSize / 1024)} Ko</color></size>", 26, Ink, TextAlignmentOptions.MidlineLeft);
                n.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                var c = Label(row, it.category, 24, Accent, TextAlignmentOptions.MidlineLeft, FontStyles.Bold); c.gameObject.AddComponent<LayoutElement>().preferredWidth = 220;
                var d = Label(row, it.Created.ToString("dd/MM/yyyy HH:mm"), 22, Muted, TextAlignmentOptions.MidlineLeft); d.gameObject.AddComponent<LayoutElement>().preferredWidth = 220;
                Pill(row, "Voir en 3D", Accent, Color.white, () => { pendingView = it; SceneManager.LoadScene(M6Navigation.GeneratorScene); }, 200, 64, 23);
                TextMeshProUGUI delLabel = null; float armed = -10f;
                var del = Pill(row, "Supprimer", Field, Ink, () =>
                {
                    if (Time.unscaledTime - armed > 3f) { armed = Time.unscaledTime; delLabel.text = "Confirmer ?"; return; }
                    StartCoroutine(M6Catalog.Remove(it, error =>
                    {
                        if (error != null) { Toast("Suppression impossible : " + error); return; }
                        Toast($"« {it.name} » supprimé"); RefreshList();
                    }));
                }, 160, 64, 22);
                delLabel = del.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        // ------------------------------------------------------------ boucle

        void Update()
        {
            Layout(false);
            float dt = Time.unscaledDeltaTime;
            if (toastGroup != null) toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, Time.unscaledTime < toastUntil ? 1f : 0f, dt * 5f);
            if (!generator) return;

            // Le modele s'affiche exactement dans son cadre
            if (modelViewer != null && viewer != null)
            {
                var c = new Vector3[4]; viewer.GetWorldCorners(c);
                const float inset = 0f;
                modelViewer.Viewport = new Rect((c[0].x + inset) / Screen.width, (c[0].y + inset) / Screen.height,
                    (c[2].x - c[0].x - 2 * inset) / Screen.width, (c[2].y - c[0].y - 2 * inset) / Screen.height);
                bool has = modelViewer.HasModel;
                emptyGroup.alpha = has ? 0f : 1f;
                hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, has ? 1f : 0f, dt * 4f);
                toolsGroup.alpha = hintGroup.alpha; toolsGroup.interactable = toolsGroup.blocksRaycasts = has;
            }

            // Suivi de la generation (le bouton est desactive par M6Controller pendant le travail)
            bool busy = generateButton != null && !generateButton.interactable;
            if (busy && !wasBusy) { busySince = Time.unscaledTime; added = false; addButton.interactable = false; }
            wasBusy = busy;
            spinner.gameObject.SetActive(busy);
            if (busy) spinner.Rotate(0, 0, -240f * dt);
            int s = Mathf.FloorToInt(Time.unscaledTime - busySince);
            timer.text = busy ? $"{s / 60}:{s % 60:00}" : "";
            if (photo != null)
            {
                photoEmpty.SetActive(!photo.gameObject.activeSelf);
                if (photo.texture != null) photoFit.aspectRatio = (float)photo.texture.width / Mathf.Max(1, photo.texture.height);
            }
            if (addButton != null && !added && modelViewer != null && !busy) addButton.interactable = modelViewer.HasModel;
        }

        /// Paysage : etapes a gauche, modele a droite. Portrait : modele en haut, etapes en dessous.
        void Layout(bool force)
        {
            bool l = Screen.width >= Screen.height;
            if (!force && l == landscape) return;
            landscape = l;
            const float m = 28f, top = 132f;
            if (!generator)
            {
                Stretch(panel, Vector2.zero, Vector2.one, new Vector2(m, m), new Vector2(-m, -top));
                return;
            }
            if (l)
            {
                Stretch(panel, Vector2.zero, new Vector2(0, 1), new Vector2(m, m), new Vector2(m + 640, -top));
                Stretch(viewer, Vector2.zero, Vector2.one, new Vector2(m * 2 + 640, m), new Vector2(-m, -top));
            }
            else
            {
                Stretch(viewer, new Vector2(0, 0.5f), Vector2.one, new Vector2(m, m * 0.5f), new Vector2(-m, -top));
                Stretch(panel, Vector2.zero, new Vector2(1, 0.5f), new Vector2(m, m), new Vector2(-m, -m * 0.5f));
            }
        }

        static void Stretch(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = oMin; rt.offsetMax = oMax;
        }

        // ------------------------------------------------------------ messages

        void BuildToast()
        {
            var t = Box("Message", root, rounded, Ink, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(900, 76));
            t.GetComponent<Image>().raycastTarget = false;
            toast = Label(t, "", 26, Cream, TextAlignmentOptions.Center);
            toastGroup = t.gameObject.AddComponent<CanvasGroup>(); toastGroup.alpha = 0; toastGroup.blocksRaycasts = false;
        }

        void Toast(string msg) { toast.text = msg; toastUntil = Time.unscaledTime + 3f; }

        // ------------------------------------------------------------ briques

        void Step(Transform parent, string number, string title)
        {
            var r = HRow(parent, 60, 14);
            r.GetComponent<Image>().color = Color.clear;
            var n = Box("Num", r, circle, Accent, Vector2.zero, Vector2.zero, new Vector2(46, 46));
            var le = n.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = 46;
            Label(n, number, 24, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            Label(r, $"<b>{title}</b>", 28, Ink, TextAlignmentOptions.MidlineLeft).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        RectTransform HRow(Transform parent, float height, float spacing)
        {
            var r = Box("Ligne", parent, rounded, new Color(0, 0, 0, 0), Vector2.zero, Vector2.zero, Vector2.zero);
            r.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            var h = r.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing; h.padding = new RectOffset(14, 14, 0, 0); h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            return r;
        }

        static VerticalLayoutGroup Column(RectTransform rt, float spacing, int pad)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(pad, pad, pad, pad);
            v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }

        void Scroll(RectTransform parent, out RectTransform content)
        {
            if (parent.GetComponent<VerticalLayoutGroup>() == null) Column(parent, 16, 30);
            var view = new GameObject("Defilement", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect), typeof(LayoutElement)).GetComponent<RectTransform>();
            view.SetParent(parent, false);
            view.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            view.GetComponent<LayoutElement>().flexibleHeight = 1;
            content = new GameObject("Contenu", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(view, false);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            Column(content, 16, 0);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = view.GetComponent<ScrollRect>(); sr.viewport = view; sr.content = content; sr.horizontal = false; sr.scrollSensitivity = 30;
        }

        TMP_InputField TextField(Transform parent, string placeholder)
        {
            var box = Box("Nom", parent, rounded, Field, Vector2.zero, Vector2.zero, Vector2.zero);
            box.gameObject.AddComponent<LayoutElement>().preferredHeight = 76;
            box.gameObject.SetActive(false);
            var area = Rect("Zone", box, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            area.offsetMin = new Vector2(22, 6); area.offsetMax = new Vector2(-22, -6);
            area.gameObject.AddComponent<RectMask2D>();
            var ph = Label(area, placeholder, 25, Muted, TextAlignmentOptions.MidlineLeft); ph.fontStyle = FontStyles.Italic;
            var tx = Label(area, "", 25, Ink, TextAlignmentOptions.MidlineLeft);
            var f = box.gameObject.AddComponent<TMP_InputField>();
            f.textViewport = area; f.textComponent = tx; f.placeholder = ph; f.targetGraphic = box.GetComponent<Image>();
            f.characterLimit = 40; f.customCaretColor = true; f.caretColor = Accent;
            box.gameObject.SetActive(true);
            return f;
        }

        Button Pill(Transform parent, string label, Color bg, Color fg, Action onClick, float width, float height, float size)
        {
            var r = Box("Bouton", parent, rounded, bg, Vector2.zero, Vector2.zero, new Vector2(width, height));
            var le = r.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = height; if (width > 0) le.preferredWidth = width;
            var b = r.gameObject.AddComponent<Button>();
            var cb = b.colors; cb.pressedColor = new Color(0.8f, 0.8f, 0.8f); cb.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.55f); b.colors = cb;
            b.onClick.AddListener(() => onClick());
            Label(r, label, size, fg, TextAlignmentOptions.Center);
            return b;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 pivot, Vector2? size = null)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot; rt.anchoredPosition = pos;
            if (size.HasValue) rt.sizeDelta = size.Value;
        }
        static void Place(Component c, Vector2 anchor, Vector2 pos, Vector2 pivot) => Place((RectTransform)c.transform, anchor, pos, pivot);

        static RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        static RectTransform Box(string name, Transform parent, Sprite sprite, Color c, Vector2 anchor, Vector2 pos, Vector2 size, bool stretch = false)
        {
            var rt = Rect(name, parent, stretch ? Vector2.zero : anchor, stretch ? Vector2.one : anchor, pos, size);
            if (stretch) rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = rt.gameObject.AddComponent<Image>(); img.sprite = sprite; img.color = c;
            img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            return rt;
        }

        static TextMeshProUGUI Label(Transform parent, string text, float size, Color c, TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
        {
            var t = new GameObject("Texte", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            t.rectTransform.SetParent(parent, false);
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = new Vector2(12, 0); t.rectTransform.offsetMax = new Vector2(-12, 0);
            t.text = text; t.fontSize = size; t.color = c; t.alignment = align; t.fontStyle = style; t.raycastTarget = false;
            return t;
        }

        /// Coin arrondi plein (thickness 0), contour, ou exterieur seul (invert), en 9-slice.
        static Sprite MakeSprite(int size, int radius, int thickness, bool invert = false)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius), cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float inside = radius - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(inside + 0.5f);
                    if (thickness > 0) a *= Mathf.Clamp01(thickness - inside + 0.5f);
                    if (invert) a = 1f - a;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return UnityEngine.Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, Vector4.one * radius);
        }
    }
}
