using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace DecoAR.Design.Editor
{
    // Mise en place du design system (docs/12-integration-unity.md) :
    //  1. font assets TextMeshPro Outfit (Regular, Medium, SemiBold, Bold) dans Resources/DecoAR/Fonts,
    //     avec les caractères français, graisses reliées pour <b> ;
    //  2. Outfit-Regular SDF devient la police par défaut de TextMeshPro (LiberationSans reste en secours) ;
    //  3. logos, visuels et illustrations importés en sprites.
    public static class DecoDesignSetup
    {
        const string Root = "Assets/DecoAR/Design/";
        static readonly (string file, string name)[] Weights =
        {
            ("Outfit_400Regular.ttf", "Outfit-Regular SDF"),
            ("Outfit_500Medium.ttf", "Outfit-Medium SDF"),
            ("Outfit_600SemiBold.ttf", "Outfit-SemiBold SDF"),
            ("Outfit_700Bold.ttf", "Outfit-Bold SDF"),
        };

        [MenuItem("DecoAR/Design system/Installer (polices, sprites, police par défaut)")]
        public static void Run()
        {
            var fonts = CreateFonts();
            SetDefaultFont(fonts);
            ImportBrand();
            AssetDatabase.SaveAssets();
            Debug.Log("[Design] Design system installé : Outfit par défaut, sprites et logos prêts");
        }

        static TMP_FontAsset[] CreateFonts()
        {
            string chars = File.ReadAllText(Root + "Fonts/caracteres-fr.txt");
            var result = new TMP_FontAsset[Weights.Length];
            for (int i = 0; i < Weights.Length; i++)
            {
                string path = $"{Root}Resources/DecoAR/Fonts/{Weights[i].name}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (existing != null) { result[i] = existing; continue; }

                var ttf = AssetDatabase.LoadAssetAtPath<Font>(Root + "Fonts/" + Weights[i].file);
                // Atlas dynamique : les caractères français sont ajoutés tout de suite, les autres à la demande
                var fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                fa.name = Weights[i].name;
                AssetDatabase.CreateAsset(fa, path);
                fa.atlasTextures[0].name = fa.name + " Atlas";
                AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
                fa.material.name = fa.name + " Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
                fa.TryAddCharacters(chars, out _);
                EditorUtility.SetDirty(fa);
                result[i] = fa;
            }
            AssetDatabase.SaveAssets();

            // <b> et fontWeight choisissent la bonne graisse d'Outfit
            var regular = result[0];
            var table = regular.fontWeightTable;
            if (table != null && table.Length > 7)
            {
                table[4].regularTypeface = result[0];   // 400
                table[5].regularTypeface = result[1];   // 500
                table[6].regularTypeface = result[2];   // 600
                table[7].regularTypeface = result[3];   // 700
                EditorUtility.SetDirty(regular); // le tableau est modifié sur place
            }
            // Caractères absents d'Outfit (●, ›…) : LiberationSans en secours
            var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            foreach (var f in result)
            {
                if (liberation != null && (f.fallbackFontAssetTable == null || !f.fallbackFontAssetTable.Contains(liberation)))
                {
                    if (f.fallbackFontAssetTable == null) f.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                    f.fallbackFontAssetTable.Add(liberation);
                }
                EditorUtility.SetDirty(f);
            }
            return result;
        }

        static void SetDefaultFont(TMP_FontAsset[] fonts)
        {
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings == null) { Debug.LogWarning("[Design] TMP Settings introuvable"); return; }
            var so = new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = fonts[0];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        static void ImportBrand()
        {
            foreach (var dir in new[] { Root + "Brand", Root + "Resources/DecoAR/Brand" })
                foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { dir }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                    bool splash = path.Contains("/Splash/");
                    if (ti.textureType == TextureImporterType.Sprite && ti.alphaIsTransparency) continue;
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.alphaIsTransparency = true;
                    ti.mipmapEnabled = !splash;
                    ti.wrapMode = path.Contains("motif-surface") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                    ti.maxTextureSize = 2048;
                    ti.textureCompression = TextureImporterCompression.CompressedHQ;
                    ti.SaveAndReimport();
                }
        }
    }
}
