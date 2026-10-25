using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DecoAR.Player.EditorTools
{
    // Charte UI de DecoAR (bois, lin, terracotta, sauge) et briques uGUI utilisees par M1SceneBuilder
    public static class M1UIKit
    {
        public const string UIFolder = "Assets/DecoAR/M1_Player/UI";

        public static readonly Color Cream = Hex("F5EFE6");
        public static readonly Color Linen = Hex("EDE3D6");
        public static readonly Color Card = Hex("FFFCF7");
        public static readonly Color Sand = Hex("E2D3BF");
        public static readonly Color Wood = Hex("A47551");
        public static readonly Color Ink = Hex("3B2F2A");
        public static readonly Color Muted = Hex("8B7B70");
        public static readonly Color Accent = Hex("C8664A");
        public static readonly Color Sage = Hex("7F9172");
        public static readonly Color SageLight = Hex("E4E9DD");
        public static readonly Color Night = Hex("231B17");

        public static Sprite Rounded { get; private set; }
        public static Sprite Circle { get; private set; }
        public static Sprite Ring { get; private set; }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        public static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        // ---------------------------------------------------------------- sprites generes

        public static void EnsureSprites()
        {
            const int s = 128;
            Vector2 center = new Vector2(s / 2f, s / 2f);

            Rounded = EnsureSprite("M1_Rounded", s, new Vector4(44, 44, 44, 44), (x, y) =>
            {
                const float r = 40f;
                float qx = Mathf.Max(Mathf.Abs(x - center.x) - (s / 2f - r), 0f);
                float qy = Mathf.Max(Mathf.Abs(y - center.y) - (s / 2f - r), 0f);
                return 0.5f - (Mathf.Sqrt(qx * qx + qy * qy) - r);
            });
            Circle = EnsureSprite("M1_Circle", s, Vector4.zero, (x, y) =>
                0.5f - (Vector2.Distance(new Vector2(x, y), center) - (s / 2f - 1f)));
            Ring = EnsureSprite("M1_Ring", s, Vector4.zero, (x, y) =>
            {
                const float t = 8f;
                float d = Mathf.Abs(Vector2.Distance(new Vector2(x, y), center) - (s / 2f - 1f - t / 2f)) - t / 2f;
                return 0.5f - d;
            });
        }

        static Sprite EnsureSprite(string name, int size, Vector4 border, Func<float, float, float> alpha)
        {
            string path = $"{UIFolder}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(x + 0.5f, y + 0.5f)) * 255));
                tex.SetPixels32(px);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != border)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------------------------------------------------------------- briques uGUI

        public static Canvas MakeCanvas(string name, Transform parent)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer("UI") };
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        // Ancre en un point
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        // Remplit le parent, avec des marges
        public static RectTransform Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        // radiusScale > 1 : coins plus petits
        public static Image Fill(RectTransform rt, Color color, Sprite sprite = null, float radiusScale = 1f)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = radiusScale;
            }
            return img;
        }

        public static TextMeshProUGUI LabelOn(RectTransform rt, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.raycastTarget = false;
            return t;
        }

        public static TextMeshProUGUI ChildLabel(Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false)
        {
            return LabelOn(Stretch(Node("Texte", parent)), text, size, color, align, bold);
        }

        public static Button MakeButton(Transform parent, string name, string text, Color fill, Color textColor,
            float fontSize, out TMP_Text label, float radiusScale = 1.5f)
        {
            var rt = Node(name, parent);
            var img = Fill(rt, fill, Rounded, radiusScale);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            b.colors = colors;
            label = ChildLabel(rt, text, fontSize, textColor, TextAlignmentOptions.Center, true);
            return b;
        }

        public static LayoutElement Size(Component c, float prefHeight = -1, float prefWidth = -1, float flexWidth = -1, float flexHeight = -1)
        {
            if (!c.TryGetComponent<LayoutElement>(out var le)) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = prefHeight;
            le.preferredWidth = prefWidth;
            le.flexibleWidth = flexWidth;
            le.flexibleHeight = flexHeight;
            return le;
        }

        public static VerticalLayoutGroup Column(RectTransform rt, int padding, float spacing, bool controlWidth = true)
        {
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.padding = new RectOffset(padding, padding, padding, padding);
            g.spacing = spacing;
            g.childControlWidth = controlWidth;
            g.childControlHeight = true;
            g.childForceExpandWidth = controlWidth;
            g.childForceExpandHeight = false;
            return g;
        }

        public static HorizontalLayoutGroup Row(RectTransform rt, int padding, float spacing)
        {
            var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.padding = new RectOffset(padding, padding, padding, padding);
            g.spacing = spacing;
            g.childAlignment = TextAnchor.MiddleLeft;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = true;
            return g;
        }
    }
}
