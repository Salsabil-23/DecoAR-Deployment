using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DecoAR.Auth.EditorTools
{
    // Briques d'interface communes aux ecrans Auth, Profil et a la couche de verrouillage :
    // palette, sprites generes, champs, boutons, panneaux defilants
    static class AuthUiKit
    {
        public const string Root = "Assets/DecoAR/Auth";
        public const string UiFolder = Root + "/UI";

        // Palette deco : expresso, creme, terracotta
        public static readonly Color BgTop = Hex("3A2E27");
        public static readonly Color BgBottom = Hex("1C1613");
        public static readonly Color Cream = Hex("F3E9DD");
        public static readonly Color Gold = Hex("D9B48A");
        public static readonly Color Accent = Hex("C8734A");
        public static readonly Color Card = Color.white;
        public static readonly Color FieldFill = Hex("F8F4EF");
        public static readonly Color FieldBorder = Hex("E2DACF");
        public static readonly Color Ink = Hex("2E2A26");
        public static readonly Color Muted = Hex("8A8178");
        public static readonly Color Hint = Hex("A79E94");
        public static readonly Color Danger = Hex("D64545");

        public static Sprite Rounded, CircleSprite, GradientSprite, ArrowSprite;

        public static void LoadSprites()
        {
            EnsureFolder(UiFolder);
            Rounded = EnsureSprite("Rounded", 64, 24, RoundedRect);
            CircleSprite = EnsureSprite("Circle", 128, 0, Circle);
            GradientSprite = EnsureSprite("Gradient", 256, 0, Gradient);
            ArrowSprite = EnsureSprite("Arrow", 128, 0, Arrow);
        }

        // ---------------------------------------------------------------- canvas et fonds

        public static GameObject NewCanvas(string name, int sortingOrder = 0)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        public static RectTransform Background(Transform parent)
        {
            var bg = Stretch("Fond", parent);
            Img(bg, GradientSprite, Color.white, 1f);
            Decor(bg, new Vector2(0, 0), new Vector2(120, 80), 820, Accent, 0.18f);
            Decor(bg, new Vector2(0.38f, 1), new Vector2(-40, -60), 380, Gold, 0.10f);
            Decor(bg, new Vector2(1, 0), new Vector2(-60, 40), 300, Accent, 0.08f);
            return bg;
        }

        public static RectTransform CardRect(Transform parent, float width)
        {
            var card = Rect("Carte", parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            card.anchorMin = new Vector2(0.5f, 0);
            card.anchorMax = new Vector2(0.5f, 1);
            card.sizeDelta = new Vector2(width, -70);
            Img(card, Rounded, Card, 0.66f);
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.35f);
            shadow.effectDistance = new Vector2(0, -10);
            return card;
        }

        static void Decor(RectTransform parent, Vector2 anchor, Vector2 pos, float size, Color color, float alpha)
        {
            var rt = Rect("Decor", parent, anchor, pos, new Vector2(size, size));
            Img(rt, CircleSprite, new Color(color.r, color.g, color.b, alpha), 1f).raycastTarget = false;
        }

        // Pastille ronde avec la lettre du logo
        public static RectTransform Logo(Transform parent, float size)
        {
            var logo = Rect("Logo", parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            Img(logo, CircleSprite, Accent, 1f);
            logo.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -6);
            Label(logo, "D", size * 0.64f, Color.white, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
            return logo;
        }

        // ---------------------------------------------------------------- panneaux

        // Panneau plein-carte qui defile si le contenu depasse (petits ecrans, clavier ouvert)
        public static RectTransform ScrollPanel(string name, RectTransform card, out GameObject panel)
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

        // Pile verticale (enfants Elem / Row), qui prend la hauteur de son contenu
        public static RectTransform Stack(string name, Transform parent, float spacing = 16)
        {
            var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return rt;
        }

        public static TMP_Text Title(RectTransform content, string title, string subtitle)
        {
            var t = Label(Elem("Titre", content, 66), title, 54, Ink, TextAlignmentOptions.Left);
            t.fontStyle = FontStyles.Bold;
            return Label(Elem("Sous-titre", content, 72), subtitle, 28, Muted, TextAlignmentOptions.TopLeft);
        }

        public static void Section(RectTransform content, string title)
        {
            var rt = Elem("Section " + title, content, 70);
            var t = Label(rt, title, 34, Ink, TextAlignmentOptions.BottomLeft);
            t.fontStyle = FontStyles.Bold;
            var line = Rect("Trait", rt, new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 2));
            line.anchorMin = new Vector2(0, 0);
            line.anchorMax = new Vector2(1, 0);
            line.anchoredPosition = new Vector2(0, -8);
            Img(line, null, FieldBorder, 1f);
        }

        // ---------------------------------------------------------------- composants

        public static AuthField Field(RectTransform parent, string name, string placeholder, TMP_InputField.ContentType type, bool reveal)
        {
            var rt = Elem(name, parent, 84);
            var border = Img(rt, Rounded, FieldBorder, 1.3f);
            var fill = Img(Inset("Fond", rt, 2), Rounded, FieldFill, 1.4f);

            var area = Inset("Zone texte", rt, 0);
            area.offsetMin = new Vector2(28, 0);
            area.offsetMax = new Vector2(reveal ? -110 : -28, 0);
            area.gameObject.AddComponent<RectMask2D>();

            var ph = Label(area, placeholder, 30, Hint, TextAlignmentOptions.MidlineLeft);
            ph.name = "Placeholder";
            var text = Label(area, "", 30, Ink, TextAlignmentOptions.MidlineLeft);
            text.name = "Texte";

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

        // Grand champ centre pour un code a 6 chiffres
        public static AuthField CodeField(RectTransform parent, string name)
        {
            var field = Field(parent, name, "• • • • • •", TMP_InputField.ContentType.IntegerNumber, false);
            var rt = (RectTransform)field.transform;
            var le = rt.GetComponent<LayoutElement>();
            le.preferredHeight = le.minHeight = 110;
            var input = field.Input;
            input.characterLimit = 6;
            input.pointSize = 52;
            input.keyboardType = TouchScreenKeyboardType.NumberPad;
            foreach (var t in rt.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                t.alignment = TextAlignmentOptions.Center;
                t.characterSpacing = 40;
                t.fontStyle = FontStyles.Bold;
            }
            return field;
        }

        public static TMP_Text Status(RectTransform parent)
        {
            var label = Label(Elem("Message", parent, 64), "", 26, Muted, TextAlignmentOptions.Center);
            label.name = "Message";
            return label;
        }

        public static Button PrimaryButton(RectTransform parent, string text, float height = 96)
        {
            var rt = Elem("Bouton " + text, parent, height);
            var img = Img(rt, Rounded, Accent, 1.1f);
            rt.gameObject.AddComponent<Shadow>().effectColor = new Color(Accent.r * 0.5f, Accent.g * 0.4f, Accent.b * 0.3f, 0.35f);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            Tint(b);
            Label(rt, text, 34, Color.white, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
            return b;
        }

        // Bouton blanc a bordure de couleur
        public static Button OutlineButton(RectTransform parent, string text, Color color, out TMP_Text label, float height = 96)
        {
            var rt = Elem("Bouton " + text, parent, height);
            Img(rt, Rounded, color, 1.1f);
            var fill = Img(Inset("Fond", rt, 3), Rounded, Color.white, 1.2f);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = fill;
            Tint(b);
            label = Label(rt, text, 32, color, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            return b;
        }

        public static Button LinkButton(RectTransform parent, string richText, TextAlignmentOptions align = TextAlignmentOptions.Center, float height = 56)
        {
            var rt = Elem("Lien", parent, height);
            var label = Label(rt, richText, 28, Muted, align);
            label.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = label;
            Tint(b);
            return b;
        }

        // Photo ronde : RawImage masquee par un cercle, initiales en dessous si pas de photo
        public static RawImage RoundPhoto(Transform parent, float size, out TMP_Text initials)
        {
            var ring = Rect("Photo", parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            Img(ring, CircleSprite, Accent, 1f);
            var mask = Inset("Masque", ring, Mathf.Max(3, size * 0.03f));
            Img(mask, CircleSprite, FieldFill, 1f);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            initials = Label(mask, "", size * 0.36f, Accent, TextAlignmentOptions.Center);
            initials.fontStyle = FontStyles.Bold;
            var photo = new GameObject("Image", typeof(RectTransform)).AddComponent<RawImage>();
            photo.rectTransform.SetParent(mask, false);
            photo.rectTransform.anchorMin = Vector2.zero;
            photo.rectTransform.anchorMax = Vector2.one;
            photo.rectTransform.offsetMin = photo.rectTransform.offsetMax = Vector2.zero;
            photo.raycastTarget = false;
            photo.enabled = false;
            return photo;
        }

        // Cercle avec l'apercu de la camera (composant FaceCamera sur le bloc) ; ring = contour colore
        static FaceCamera CameraCircle(RectTransform block, float size, out RectTransform circle, out Image ring)
        {
            var previewRow = Elem("Apercu", block, size + 10);
            circle = Rect("Cercle", previewRow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            ring = Img(circle, CircleSprite, Accent, 1f);
            var mask = Inset("Masque", circle, 7);
            Img(mask, CircleSprite, Ink, 1f);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var preview = new GameObject("Camera", typeof(RectTransform)).AddComponent<RawImage>();
            preview.rectTransform.SetParent(mask, false);
            preview.rectTransform.anchorMin = Vector2.zero;
            preview.rectTransform.anchorMax = Vector2.one;
            preview.rectTransform.offsetMin = preview.rectTransform.offsetMax = Vector2.zero;
            preview.raycastTarget = false;

            var cam = block.gameObject.AddComponent<FaceCamera>();
            Set(cam, "preview", preview);
            return cam;
        }

        // Bloc photo de profil : apercu rond, consigne, Prendre / Utiliser / Reprendre / (cancelText)
        public static SelfieCapture SelfieBlock(RectTransform content, string cancelText, float size = 370)
        {
            var block = Stack("Bloc photo", content, 12);
            var cam = CameraCircle(block, size, out _, out _);
            var hint = Label(Elem("Consigne", block, 44), "", 28, Muted, TextAlignmentOptions.Center);
            var capture = PrimaryButton(block, "Prendre la photo");
            var use = PrimaryButton(block, "Utiliser cette photo");
            var retake = LinkButton(block, "<b><color=#C8734A>Reprendre</color></b>");
            var cancel = LinkButton(block, cancelText);

            var selfie = block.gameObject.AddComponent<SelfieCapture>();
            Set(selfie, "faceCamera", cam);
            Set(selfie, "hint", hint);
            Set(selfie, "captureButton", capture);
            Set(selfie, "useButton", use);
            Set(selfie, "retakeButton", retake);
            Set(selfie, "cancelButton", cancel);
            return selfie;
        }

        // Bloc defi de mouvements : apercu rond avec fleche, etape, consigne, Commencer / (cancelText)
        public static FaceChallenge ChallengeBlock(RectTransform content, string cancelText, float size = 370, Color? textColor = null)
        {
            var block = Stack("Bloc defi", content, 12);
            var cam = CameraCircle(block, size, out var circle, out var ring);

            // Fleche semi-transparente par-dessus la camera, tournee selon le mouvement demande
            var arrow = Rect("Fleche", circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.42f, size * 0.42f));
            var arrowImg = Img(arrow, ArrowSprite, new Color(1, 1, 1, 0.85f), 1f);
            arrowImg.raycastTarget = false;
            arrow.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.5f);

            var color = textColor ?? Ink;
            var step = Label(Elem("Etape", block, 34), "", 24, Accent, TextAlignmentOptions.Center);
            step.fontStyle = FontStyles.Bold;
            var instruction = Label(Elem("Consigne", block, 84), "", 32, color, TextAlignmentOptions.Center);
            instruction.fontStyle = FontStyles.Bold;
            var start = PrimaryButton(block, "Commencer");
            var cancel = LinkButton(block, cancelText);

            var challenge = block.gameObject.AddComponent<FaceChallenge>();
            Set(challenge, "faceCamera", cam);
            Set(challenge, "instruction", instruction);
            Set(challenge, "stepLabel", step);
            Set(challenge, "arrow", arrow);
            Set(challenge, "ring", ring);
            Set(challenge, "startButton", start);
            Set(challenge, "startLabel", start.GetComponentInChildren<TMP_Text>());
            Set(challenge, "cancelButton", cancel);
            return challenge;
        }

        public static void Tint(Selectable s)
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

        // ---------------------------------------------------------------- utilitaires UI

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
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

        public static RectTransform Stretch(string name, Transform parent) => Inset(name, parent, 0);

        public static RectTransform Inset(string name, Transform parent, float inset)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        // Element d'un layout vertical, de hauteur fixe
        public static RectTransform Elem(string name, Transform parent, float height)
        {
            var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, height));
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = le.minHeight = height;
            le.flexibleWidth = 1;
            return rt;
        }

        public static RectTransform Row(string name, Transform parent, float height, TextAnchor align, float spacing = 0)
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

        public static void Fixed(RectTransform rt, float w, float h)
        {
            if (!rt.TryGetComponent<LayoutElement>(out var le)) le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = w;
            le.preferredHeight = le.minHeight = h;
            le.flexibleWidth = 0;
        }

        public static Image Img(RectTransform rt, Sprite sprite, Color color, float ppuMultiplier)
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

        public static TextMeshProUGUI Label(RectTransform parent, string text, float size, Color color, TextAlignmentOptions align)
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

        // Fleche pleine pointant vers la droite : tige + pointe triangulaire, bords adoucis
        static float Arrow(int x, int y, int size)
        {
            float u = (x + 0.5f) / size, v = (y + 0.5f) / size - 0.5f;
            float shaft = (u >= 0.12f && u <= 0.58f) ? Mathf.Clamp01((0.11f - Mathf.Abs(v)) * size) : 0f;
            float head = 0f;
            if (u >= 0.5f && u <= 0.9f)
            {
                float half = (0.9f - u) / 0.4f * 0.32f; // la pointe s'affine vers la droite
                head = Mathf.Clamp01((half - Mathf.Abs(v)) * size);
            }
            return Mathf.Max(shaft, head);
        }

        // ---------------------------------------------------------------- serialisation / dossiers

        public static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) throw new System.ArgumentException($"{target.GetType().Name}.{field} introuvable");
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;
    }
}
