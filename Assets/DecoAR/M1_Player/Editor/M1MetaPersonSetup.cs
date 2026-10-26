using System.IO;
using UnityEditor;
using UnityEngine;

namespace DecoAR.Player.EditorTools
{
    // Avatar MetaPerson : Unity ne relie pas toutes les textures integrees au FBX (tete et corps restent blancs).
    // On extrait les textures et on cree des materiaux URP Lit relies par nom.
    public static class M1MetaPersonSetup
    {
        const string Folder = M1MixamoImporter.MixamoFolder + "/Avatars/MetaPerson";

        // materiau du FBX -> prefixe des textures MetaPerson ; les autres (cils transparents, tenue) gardent le materiau integre
        static readonly (string material, string texture)[] Map =
        {
            ("AvatarHead", "AvatarHeadMale"),
            ("AvatarBody", "AvatarBodyMale"),
            ("AvatarLeftEyeball", "AvatarEyes"),
            ("AvatarRightEyeball", "AvatarEyes"),
            ("AvatarTeethLower", "AvatarTeeth"),
            ("AvatarTeethUpper", "AvatarTeeth"),
            // "outfit" s'affiche deja correctement avec son materiau integre
        };

        [MenuItem("DecoAR/M1/Reparer les materiaux MetaPerson")]
        public static void Setup()
        {
            string fbx = null;
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { Folder }))
            {
                fbx = AssetDatabase.GUIDToAssetPath(g);
                break;
            }
            if (fbx == null)
            {
                EditorUtility.DisplayDialog("MetaPerson", "Aucun FBX dans " + Folder, "OK");
                return;
            }

            var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
            string texFolder = Folder + "/Textures";
            string matFolder = Folder + "/Materials";
            if (!AssetDatabase.IsValidFolder(texFolder)) AssetDatabase.CreateFolder(Folder, "Textures");
            if (!AssetDatabase.IsValidFolder(matFolder)) AssetDatabase.CreateFolder(Folder, "Materials");

            importer.ExtractTextures(texFolder);
            AssetDatabase.Refresh();

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var (matName, prefix) in Map)
            {
                var color = FindTexture(texFolder, prefix, "_Color");
                if (color == null) { Debug.LogWarning($"[M1] Texture {prefix}_Color introuvable"); continue; }
                var normal = FindTexture(texFolder, prefix, "_Normal");
                var metal = FindTexture(texFolder, prefix, "_UnityMetallicSmoothness");
                if (normal != null) MarkNormal(normal);
                if (metal != null) MarkLinear(metal);

                string path = $"{matFolder}/{matName}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(lit);
                    AssetDatabase.CreateAsset(mat, path);
                }
                mat.shader = lit;
                mat.SetTexture("_BaseMap", color);
                mat.SetColor("_BaseColor", Color.white);
                if (normal != null)
                {
                    mat.SetTexture("_BumpMap", normal);
                    mat.EnableKeyword("_NORMALMAP");
                }
                if (metal != null)
                {
                    mat.SetTexture("_MetallicGlossMap", metal);
                    mat.SetFloat("_Smoothness", 1f); // l'alpha de la texture porte la brillance
                    mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                }
                else mat.SetFloat("_Smoothness", 0.35f);
                EditorUtility.SetDirty(mat);

                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), matName), mat);
            }

            AssetDatabase.SaveAssets();
            importer.SaveAndReimport();
            Debug.Log("[M1] Materiaux MetaPerson relies a leurs textures");
        }

        static Texture2D FindTexture(string folder, string prefix, string kind)
        {
            foreach (var g in AssetDatabase.FindAssets(prefix + kind, new[] { folder }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p).StartsWith(prefix + kind))
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            }
            return null;
        }

        static void MarkNormal(Texture2D tex)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
            if (ti.textureType == TextureImporterType.NormalMap) return;
            ti.textureType = TextureImporterType.NormalMap;
            ti.SaveAndReimport();
        }

        // Metal / brillance : donnees, pas une couleur
        static void MarkLinear(Texture2D tex)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
            if (!ti.sRGBTexture) return;
            ti.sRGBTexture = false;
            ti.SaveAndReimport();
        }
    }
}
