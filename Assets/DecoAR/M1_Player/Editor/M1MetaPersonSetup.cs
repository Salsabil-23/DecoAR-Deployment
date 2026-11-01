using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DecoAR.Player.EditorTools
{
    // Avatars MetaPerson (metaperson.avatarsdk.com) : un dossier Mixamo/Avatars/MetaPerson... par avatar, avec son FBX.
    // Unity ne relie pas toutes les textures integrees au FBX (tete et corps restent blancs) :
    // on extrait les textures et on cree des materiaux URP Lit relies par nom, dans le dossier de chaque avatar.
    public static class M1MetaPersonSetup
    {
        const string AvatarsFolder = M1MixamoImporter.MixamoFolder + "/Avatars";
        const string ProfilePath = "Assets/DecoAR/M1_Player/Data/M1_CharacterProfile.asset";

        // materiau du FBX -> debut du nom des textures MetaPerson (Male ou Female selon l'avatar) ;
        // les autres (cils transparents, tenue) gardent le materiau integre
        static readonly (string material, string texture)[] Map =
        {
            ("AvatarHead", "AvatarHead"),
            ("AvatarBody", "AvatarBody"),
            ("AvatarLeftEyeball", "AvatarEyes"),
            ("AvatarRightEyeball", "AvatarEyes"),
            ("AvatarTeethLower", "AvatarTeeth"),
            ("AvatarTeethUpper", "AvatarTeeth"),
            // "outfit" s'affiche deja correctement avec son materiau integre
        };

        [MenuItem("DecoAR/M1/Reparer les materiaux MetaPerson")]
        public static void Setup()
        {
            int n = 0;
            foreach (var folder in MetaPersonFolders())
                if (FixFolder(folder)) n++;
            if (n == 0) EditorUtility.DisplayDialog("MetaPerson", "Aucun FBX dans " + AvatarsFolder + "/MetaPerson...", "OK");
        }

        // Nouveaux avatars : repare leurs materiaux puis les ajoute a l'onglet Avatar de la personnalisation
        [MenuItem("DecoAR/M1/Mettre a jour la liste des avatars")]
        public static void UpdateAvatarList()
        {
            var profile = AssetDatabase.LoadAssetAtPath<CharacterProfile>(ProfilePath);
            if (profile == null)
            {
                EditorUtility.DisplayDialog("Avatars", "Profil introuvable : " + ProfilePath + " (menu DecoAR > M1 > Tout generer)", "OK");
                return;
            }

            var avatars = new List<GameObject>();
            foreach (var folder in MetaPersonFolders())
            {
                FixFolder(folder);
                var model = FindModel(folder);
                if (model != null) avatars.Add(AssetDatabase.LoadAssetAtPath<GameObject>(model));
            }

            if (profile.prefab == null && avatars.Count > 0) profile.prefab = avatars[0];
            profile.otherAvatars = avatars.Where(a => a != null && a != profile.prefab).ToArray();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            string names = string.Join(", ", new[] { profile.prefab }.Concat(profile.otherAvatars).Where(a => a != null).Select(a => a.name));
            Debug.Log($"[M1] {profile.AvatarCount} avatar(s) dans l'onglet Avatar : {names}");
        }

        static IEnumerable<string> MetaPersonFolders()
        {
            if (!AssetDatabase.IsValidFolder(AvatarsFolder)) yield break;
            foreach (var folder in AssetDatabase.GetSubFolders(AvatarsFolder).OrderBy(f => f))
                if (Path.GetFileName(folder).StartsWith("MetaPerson")) yield return folder;
        }

        static string FindModel(string folder)
        {
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { folder }))
                return AssetDatabase.GUIDToAssetPath(g);
            return null;
        }

        static bool FixFolder(string folder)
        {
            string fbx = FindModel(folder);
            if (fbx == null) return false;

            var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
            string texFolder = folder + "/Textures";
            string matFolder = folder + "/Materials";
            if (!AssetDatabase.IsValidFolder(texFolder)) AssetDatabase.CreateFolder(folder, "Textures");
            if (!AssetDatabase.IsValidFolder(matFolder)) AssetDatabase.CreateFolder(folder, "Materials");

            importer.ExtractTextures(texFolder);
            AssetDatabase.Refresh();

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var (matName, prefix) in Map)
            {
                var color = FindTexture(texFolder, prefix, "_Color");
                if (color == null) { Debug.LogWarning($"[M1] {folder} : texture {prefix}..._Color introuvable"); continue; }
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
            Debug.Log($"[M1] Materiaux MetaPerson relies a leurs textures : {folder}");
            return true;
        }

        // ex. prefix "AvatarHead", kind "_Color" -> AvatarHeadMale_Color.png ou AvatarHeadFemale_Color.png
        static Texture2D FindTexture(string folder, string prefix, string kind)
        {
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                string name = Path.GetFileNameWithoutExtension(p);
                if (name.StartsWith(prefix) && name.Contains(kind))
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
