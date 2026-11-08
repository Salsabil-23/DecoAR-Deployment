using System;
using System.IO;
using UnityEngine;

namespace DecoAR.Player
{
    // Visage 3D scanne (maillage ARCore / ARKit) dans un repere normalise :
    // origine au centre du visage au niveau du bout du nez, x = droite de la personne, y = haut, z = vers l'avant.
    [Serializable]
    public class FaceMaskData
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uv;
        public float height;   // menton -> front, en metres
        public float width;    // largeur au niveau des pommettes, en metres
    }

    // Maillage + texture, uniquement dans le stockage prive de l'application
    public static class FaceMaskStore
    {
        static FaceMaskData cachedData;
        static Texture2D cachedTexture;
        static Mesh cachedMesh;

        static string DataPath => Path.Combine(Application.persistentDataPath, "decoar_visage3d.json");
        static string TexturePath => Path.Combine(Application.persistentDataPath, "decoar_visage3d.png");

        public static bool Exists => cachedData != null || (File.Exists(DataPath) && File.Exists(TexturePath));

        public static void Save(FaceMaskData data, Texture2D texture)
        {
            File.WriteAllText(DataPath, JsonUtility.ToJson(data));
            File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
            cachedData = data;
            cachedTexture = texture;
            cachedMesh = null;
        }

        public static bool TryLoad(out FaceMaskData data, out Texture2D texture, out Mesh mesh)
        {
            if (cachedData == null && File.Exists(DataPath) && File.Exists(TexturePath))
            {
                cachedData = JsonUtility.FromJson<FaceMaskData>(File.ReadAllText(DataPath));
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(File.ReadAllBytes(TexturePath))) cachedTexture = tex;
                else cachedData = null;
            }

            if (cachedData != null && cachedMesh == null)
            {
                cachedMesh = new Mesh { name = "VisageScanne3D" };
                cachedMesh.vertices = cachedData.vertices;
                cachedMesh.uv = cachedData.uv;
                cachedMesh.triangles = cachedData.triangles;
                cachedMesh.RecalculateNormals();
                cachedMesh.RecalculateBounds();
            }
            if (cachedTexture != null) cachedTexture.wrapMode = TextureWrapMode.Clamp;

            data = cachedData;
            texture = cachedTexture;
            mesh = cachedMesh;
            return data != null && texture != null;
        }

        public static void Delete()
        {
            if (File.Exists(DataPath)) File.Delete(DataPath);
            if (File.Exists(TexturePath)) File.Delete(TexturePath);
            cachedData = null;
            cachedTexture = null;
            cachedMesh = null;
        }
    }
}
