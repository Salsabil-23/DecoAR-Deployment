using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DecoAR.Auth.EditorTools
{
    // Menu DecoAR > Auth : genere les sprites arrondis et la scene Auth (inscription + connexion)
    public static class AuthSceneBuilder
    {
        const string Root = "Assets/DecoAR/Auth";
        const string UiFolder = Root + "/UI";
        const string SceneFolder = Root + "/Scenes";
        const string ScenePath = SceneFolder + "/Auth.unity";

        // Palette deco : expresso, creme, terracotta
        static readonly Color BgTop = Hex("3A2E27");
        static readonly Color BgBottom = Hex("1C1613");
        static readonly Color Cream = Hex("F3E9DD");
        static readonly Color Gold = Hex("D9B48A");
        static readonly Color Accent = Hex("C8734A");
        static readonly Color Card = Color.white;
        static readonly Color FieldFill = Hex("F8F4EF");
        static readonly Color FieldBorder = Hex("E2DACF");
        static readonly Color Ink = Hex("2E2A26");
        static readonly Color Muted = Hex("8A8178");
        static readonly Color Hint = Hex("A79E94");

        static Sprite rounded, circle, gradient;

        [MenuItem("DecoAR/Auth/Creer la scene inscription + connexion")]
        public static void BuildScene()
        {
            EnsureFolder(UiFolder);
            EnsureFolder(SceneFolder);
            rounded = EnsureSprite("Rounded", 64, 24, RoundedRect);
            circle = EnsureSprite("Circle", 128, 0, Circle);
            gradient = EnsureSprite("Gradient", 256, 0, Gradient);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BgBottom;

            var canvasGo = new GameObject("UI", typeof(RectTransform));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var ct = canvasGo.transform;

            // ---- fond degrade + cercles decoratifs
            var bg = Stretch("Fond", ct);
            Img(bg, gradient, Color.white, 1f).raycastTarget = false;
            Decor(bg, new Vector2(0, 0), new Vector2(120, 80), 820, Accent, 0.18f);
            Decor(bg, new Vector2(0.38f, 1), new Vector2(-40, -60), 380, Gold, 0.10f);
            Decor(bg, new Vector2(1, 0), new Vector2(-60, 40), 300, Accent, 0.08f);

            // ---- panneau marque
            var brand = Stretch("Marque", ct);
            var brandStack = Rect("Contenu", brand, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 600));
            var bv = brandStack.gameObject.AddComponent<VerticalLayoutGroup>();
            bv.childAlignment = TextAnchor.MiddleCenter;
            bv.spacing = 18;
            bv.childControlWidth = bv.childControlHeight = true;
            bv.childForceExpandWidth = true;
            bv.childForceExpandHeight = false;
            brandStack.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var logoRow = Row("Logo", brandStack, 150, TextAnchor.MiddleCenter);
            logoRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var logo = Rect("Pastille", logoRow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
            Fixed(logo, 150, 150);
            Img(logo, circle, Accent, 1f);
            logo.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -6);
            var logoText = Label(logo, "D", 96, Color.white, TextAlignmentOptions.Center);
            logoText.fontStyle = FontStyles.Bold;

            var brandTitle = Label(Elem("Titre", brandStack, 130), "Deco<color=#C8734A>AR</color>", 110, Cream, TextAlignmentOptions.Center);
            brandTitle.fontStyle = FontStyles.Bold;
            Label(Elem("Slogan", brandStack, 110), "Décore ton intérieur\nen réalité augmentée", 40, Gold, TextAlignmentOptions.Center);

            var details = Rect("Atouts", brandStack, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 190));
            Fixed(details, 560, 190);
            var dv = details.gameObject.AddComponent<VerticalLayoutGroup>();
            dv.padding = new RectOffset(70, 0, 30, 0);
            dv.spacing = 10;
            dv.childControlWidth = dv.childControlHeight = true;
            dv.childForceExpandHeight = false;
            foreach (var line in new[] { "Essaie les meubles chez toi en AR", "Taille réelle, lumière réaliste", "Connexion express avec Face ID" })
                Label(Elem("Atout", details, 44), $"<color=#C8734A>•</color>  {line}", 30, Cream, TextAlignmentOptions.Left);

            // ---- carte formulaire
            var cardArea = Stretch("Zone carte", ct);
            var card = Rect("Carte", cardArea, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            card.anchorMin = new Vector2(0.5f, 0);
            card.anchorMax = new Vector2(0.5f, 1);
            card.sizeDelta = new Vector2(880, -70);
            Img(card, rounded, Card, 0.66f);
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.35f);
            shadow.effectDistance = new Vector2(0, -10);

            var screen = canvasGo.AddComponent<AuthScreen>();
            Set(screen, "scaler", scaler);
            Set(screen, "brandPanel", brand);
            Set(screen, "brandDetails", details.gameObject);
            Set(screen, "cardArea", cardArea);

            BuildRegister(card, screen);
            BuildLogin(card, screen);

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            SaveScene();
        }

        // ---------------------------------------------------------------- panneaux

        static void BuildRegister(RectTransform card, AuthScreen screen)
        {
            var content = ScrollPanel("Inscription", card, out var panel);
            Set(screen, "registerPanel", panel);

            Title(content, "Créer un compte", "Rejoins DecoAR en quelques secondes");

            var names = Row("Nom complet", content, 84, TextAnchor.MiddleCenter, 20);
            Set(screen, "firstName", Field(names, "Prenom", "Prénom", TMP_InputField.ContentType.Name, false));
            Set(screen, "lastName", Field(names, "Nom", "Nom", TMP_InputField.ContentType.Name, false));
            Set(screen, "email", Field(content, "Email", "Adresse e-mail", TMP_InputField.ContentType.EmailAddress, false));
            var phone = Field(content, "Telephone", "Téléphone (optionnel)", TMP_InputField.ContentType.Custom, false);
            phone.Input.keyboardType = TouchScreenKeyboardType.PhonePad;
            Set(screen, "phone", phone);
            var pw = Row("Mots de passe", content, 84, TextAnchor.MiddleCenter, 20);
            Set(screen, "password", Field(pw, "MotDePasse", "Mot de passe", TMP_InputField.ContentType.Password, true));
            Set(screen, "confirmPassword", Field(pw, "Confirmation", "Confirmer", TMP_InputField.ContentType.Password, true));

            // jauge de robustesse
            var strength = Elem("Robustesse", content, 40);
            var bars = Rect("Barres", strength, new Vector2(0, 0.5f), new Vector2(170, 0), new Vector2(340, 10));
            var bh = bars.gameObject.AddComponent<HorizontalLayoutGroup>();
            bh.spacing = 8;
            bh.childControlWidth = bh.childControlHeight = true;
            bh.childForceExpandWidth = bh.childForceExpandHeight = true;
            var barImages = Enumerable.Range(0, 4).Select(i => Img(Rect($"Barre {i + 1}", bars, Vector2.zero, Vector2.zero, Vector2.zero), rounded, FieldBorder, 5f)).ToArray();
            SetArray(screen, "strengthBars", barImages);
            var strengthLabel = Rect("Texte", strength, new Vector2(1, 0.5f), new Vector2(-230, 0), new Vector2(460, 40));
            Set(screen, "strengthLabel", Label(strengthLabel, "", 24, Muted, TextAlignmentOptions.Right));

            Set(screen, "registerFaceIdToggle", FaceIdRow(content, "Activer Face ID", "Connecte-toi ensuite sans mot de passe", out var faceRow));
            Set(screen, "registerFaceIdRow", faceRow);

            Set(screen, "registerStatus", Status(content));
            Set(screen, "registerButton", PrimaryButton(content, "Créer mon compte"));
            Set(screen, "goToLoginButton", LinkButton(content, "Déjà un compte ? <b><color=#C8734A>Se connecter</color></b>"));
        }

        static void BuildLogin(RectTransform card, AuthScreen screen)
        {
            var content = ScrollPanel("Connexion", card, out var panel);
            Set(screen, "loginPanel", panel);

            Title(content, "Bon retour !", "Connecte-toi pour continuer ta déco");
            Set(screen, "loginEmail", Field(content, "Email", "Adresse e-mail", TMP_InputField.ContentType.EmailAddress, false));
            Set(screen, "loginPassword", Field(content, "MotDePasse", "Mot de passe", TMP_InputField.ContentType.Password, true));

            Set(screen, "loginFaceIdToggle", FaceIdRow(content, "Activer Face ID sur cet appareil", "Pour ta prochaine connexion", out var faceRow));
            Set(screen, "loginFaceIdRow", faceRow);

            Set(screen, "loginStatus", Status(content));
            Set(screen, "loginButton", PrimaryButton(content, "Se connecter"));

            // "ou" + bouton Face ID (visible seulement si Face ID est deja active sur ce telephone)
            var group = Elem("Groupe Face ID", content, 170);
            Set(screen, "faceIdLoginGroup", group.gameObject);
            var divider = Rect("Separateur", group, new Vector2(0.5f, 1), new Vector2(0, -25), new Vector2(760, 40));
            Img(Rect("Trait G", divider, new Vector2(0, 0.5f), new Vector2(170, 0), new Vector2(340, 2)), null, FieldBorder, 1f);
            Img(Rect("Trait D", divider, new Vector2(1, 0.5f), new Vector2(-170, 0), new Vector2(340, 2)), null, FieldBorder, 1f);
            Label(divider, "ou", 26, Muted, TextAlignmentOptions.Center);

            var fb = Rect("Bouton Face ID", group, new Vector2(0.5f, 0), Vector2.zero, Vector2.zero);
            fb.anchorMin = new Vector2(0, 0);
            fb.anchorMax = new Vector2(1, 0);
            fb.anchoredPosition = new Vector2(0, 50);
            fb.sizeDelta = new Vector2(0, 96);
            Img(fb, rounded, Accent, 1.1f);
            var fill = Img(Inset("Fond", fb, 3), rounded, Color.white, 1.2f);
            var faceButton = fb.gameObject.AddComponent<Button>();
            faceButton.targetGraphic = fill;
            Tint(faceButton);
            var faceLabel = Label(fb, "Se connecter avec Face ID", 32, Accent, TextAlignmentOptions.Center);
            faceLabel.fontStyle = FontStyles.Bold;
            Set(screen, "faceIdLoginButton", faceButton);
            Set(screen, "faceIdLoginLabel", faceLabel);

            Set(screen, "goToRegisterButton", LinkButton(content, "Pas encore de compte ? <b><color=#C8734A>S'inscrire</color></b>"));
            panel.SetActive(false);
        }

        // Panneau plein-carte qui defile si le contenu depasse (petits ecrans, clavier ouvert)
        static RectTransform ScrollPanel(string name, RectTransform card, out GameObject panel)
        {
            var root = Stretch(name, card);
            panel = root.gameObject;
            var viewport = Stretch("Viewport", root);
            viewport.gameObject.AddComponent<RectMask2D>();
            Img(viewport, null, new Color(1, 1, 1, 0), 1f); // capte le glisser pour le defilement

            var content = Rect("Contenu", viewport, new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(60, 60, 44, 36);
            v.spacing = 16;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
            return content;
        }

        static void Title(RectTransform content, string title, string subtitle)
        {
            var t = Label(Elem("Titre", content, 66), title, 54, Ink, TextAlignmentOptions.Left);
            t.fontStyle = FontStyles.Bold;
            Label(Elem("Sous-titre", content, 44), subtitle, 28, Muted, TextAlignmentOptions.TopLeft);
        }

        // ---------------------------------------------------------------- composants

        static AuthField Field(RectTransform parent, string name, string placeholder, TMP_InputField.ContentType type, bool reveal)
        {
            var rt = Elem(name, parent, 84);
            var border = Img(rt, rounded, FieldBorder, 1.3f);
            var fill = Img(Inset("Fond", rt, 2), rounded, FieldFill, 1.4f);

            var area = Inset("Zone texte", rt, 0);
            area.offsetMin = new Vector2(28, 0);
            area.offsetMax = new Vector2(reveal ? -110 : -28, 0);
            area.gameObject.AddComponent<RectMask2D>();

            var ph = Label(area, placeholder, 30, Hint, TextAlignmentOptions.MidlineLeft);
            ph.name = "Placeholder";
            var text = Label(area, "", 30, Ink, TextAlignmentOptions.MidlineLeft);
            text.name = "Texte";
            text.raycastTarget = false;

            var input = rt.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = ph;
            input.fontAsset = text.font;
            input.pointSize = 30;
            input.targetGraphic = fill;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = type;
            input.customCaretColor = true;
            input.caretColor = Accent;
            input.caretWidth = 3;
            input.selectionColor = new Color(Accent.r, Accent.g, Accent.b, 0.3f);
            Tint(input);

            var field = rt.gameObject.AddComponent<AuthField>();
            Set(field, "border", border);
            if (reveal)
            {
                var eye = Rect("Voir", rt, new Vector2(1, 0.5f), new Vector2(-58, 0), new Vector2(100, 64));
                var eyeImg = Img(eye, null, new Color(1, 1, 1, 0), 1f);
                var eyeButton = eye.gameObject.AddComponent<Button>();
                eyeButton.targetGraphic = eyeImg;
                var eyeLabel = Label(eye, "Voir", 24, Accent, TextAlignmentOptions.Center);
                eyeLabel.fontStyle = FontStyles.Bold;
                Set(field, "revealButton", eyeButton);
                Set(field, "revealLabel", eyeLabel);
            }
            return field;
        }

        static Toggle FaceIdRow(RectTransform parent, string title, string subtitle, out GameObject row)
        {
            var rt = Elem("Face ID", parent, 84);
            row = rt.gameObject;
            Img(rt, rounded, FieldFill, 1.4f);

            var icon = Rect("Icone", rt, new Vector2(0, 0.5f), new Vector2(54, 0), new Vector2(56, 56));
            Img(icon, circle, new Color(Accent.r, Accent.g, Accent.b, 0.15f), 1f);
            Label(icon, "<b>ID</b>", 22, Accent, TextAlignmentOptions.Center);

            var texts = Inset("Textes", rt, 0);
            texts.offsetMin = new Vector2(100, 0);
            texts.offsetMax = new Vector2(-150, 0);
            Label(texts, $"<b>{title}</b>\n<size=22><color=#8A8178>{subtitle}</color></size>", 28, Ink, TextAlignmentOptions.MidlineLeft);

            var sw = Rect("Interrupteur", rt, new Vector2(1, 0.5f), new Vector2(-80, 0), new Vector2(100, 54));
            var track = Img(sw, rounded, Accent, 0.9f);
            var knob = Rect("Pastille", sw, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44));
            Img(knob, circle, Color.white, 1f).raycastTarget = false;
            knob.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.2f);

            var toggle = sw.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = track;
            toggle.isOn = true;
            toggle.transition = Selectable.Transition.None;
            var switcher = sw.gameObject.AddComponent<ToggleSwitch>();
            Set(switcher, "track", track);
            Set(switcher, "knob", knob);
            return toggle;
        }

        static TMP_Text Status(RectTransform parent)
        {
            var label = Label(Elem("Message", parent, 56), "", 26, Muted, TextAlignmentOptions.Center);
            label.name = "Message";
            return label;
        }

        static Button PrimaryButton(RectTransform parent, string text)
        {
            var rt = Elem("Bouton " + text, parent, 96);
            var img = Img(rt, rounded, Accent, 1.1f);
            rt.gameObject.AddComponent<Shadow>().effectColor = new Color(Accent.r * 0.5f, Accent.g * 0.4f, Accent.b * 0.3f, 0.35f);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            Tint(b);
            var label = Label(rt, text, 34, Color.white, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            return b;
        }

        static Button LinkButton(RectTransform parent, string richText)
        {
            var rt = Elem("Lien", parent, 56);
            var label = Label(rt, richText, 28, Muted, TextAlignmentOptions.Center);
            label.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = label;
            Tint(b);
            return b;
        }

        static void Tint(Selectable s)
        {
            var c = s.colors;
            c.normalColor = Color.white;
            c.highlightedColor = new Color(0.96f, 0.96f, 0.96f);
            c.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            c.selectedColor = Color.white;
            c.disabledColor = new Color(1, 1, 1, 0.6f);
            c.fadeDuration = 0.08f;
            s.colors = c;
        }

        static void Decor(RectTransform parent, Vector2 anchor, Vector2 pos, float size, Color color, float alpha)
        {
            var rt = Rect("Decor", parent, anchor, pos, new Vector2(size, size));
            Img(rt, circle, new Color(color.r, color.g, color.b, alpha), 1f).raycastTarget = false;
        }

        // ---------------------------------------------------------------- utilitaires UI

        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static RectTransform Stretch(string name, Transform parent) => Inset(name, parent, 0);

        static RectTransform Inset(string name, Transform parent, float inset)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        // Element d'un layout vertical, de hauteur fixe
        static RectTransform Elem(string name, Transform parent, float height)
        {
            var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, height));
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = le.minHeight = height;
            le.flexibleWidth = 1;
            return rt;
        }

        static RectTransform Row(string name, Transform parent, float height, TextAnchor align, float spacing = 0)
        {
            var rt = Elem(name, parent, height);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            return rt;
        }

        static void Fixed(RectTransform rt, float w, float h)
        {
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = w;
            le.preferredHeight = le.minHeight = h;
        }

        static Image Img(RectTransform rt, Sprite sprite, Color color, float ppuMultiplier)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = ppuMultiplier;
            }
            return img;
        }

        static TextMeshProUGUI Label(RectTransform parent, string text, float size, Color color, TextAlignmentOptions align)
        {
            var label = new GameObject("Texte", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.gameObject.layer = LayerMask.NameToLayer("UI");
            var rt = label.rectTransform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.raycastTarget = false;
            return label;
        }

        // ---------------------------------------------------------------- sprites generes

        delegate float Shape(int x, int y, int size);

        static Sprite EnsureSprite(string name, int size, int border, Shape alpha)
        {
            string path = $"{UiFolder}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        tex.SetPixel(x, y, name == "Gradient"
                            ? Color.Lerp(BgBottom, BgTop, alpha(x, y, size))
                            : new Color(1, 1, 1, alpha(x, y, size)));
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // Carre aux coins arrondis (rayon 24 px = bordure 9-slice), bords anticrenneles
        static float RoundedRect(int x, int y, int size)
        {
            const float r = 24f;
            float cx = Mathf.Clamp(x + 0.5f, r, size - r);
            float cy = Mathf.Clamp(y + 0.5f, r, size - r);
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
            return Mathf.Clamp01(r - d + 0.5f);
        }

        static float Circle(int x, int y, int size)
        {
            float r = size / 2f;
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
            return Mathf.Clamp01(r - d);
        }

        static float Gradient(int x, int y, int size) => y / (float)(size - 1);

        // ---------------------------------------------------------------- serialisation / scene

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SaveScene()
        {
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            // Premiere scene du build : l'app s'ouvre sur l'inscription / connexion
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Auth] Scene enregistree : {ScenePath} (premiere scene du build)");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;
    }
}
