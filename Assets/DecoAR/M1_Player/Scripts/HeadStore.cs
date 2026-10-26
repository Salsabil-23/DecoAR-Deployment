using System;
using System.IO;
using UnityEngine;

namespace DecoAR.Player
{
    // Tete 3D FLAME creee par M1_FaceServer, repere Unity, en metres :
    // y = haut, z = vers l'avant du visage. "neck" = articulation du cou (pose sur l'os Head du personnage).
    [Serializable]
    public class HeadData
    {
        public float[] vertices;   // x, y, z a la suite
        public float[] uv;         // u, v a la suite
        public int[] triangles;
        public float[] neck;
        public float top;          // haut du crane
        public float chin;         // bas du menton
        public float brow;         // haut du visage (front)
        public float[] nose;       // bout du nez
        public float[] skin;       // rgb 0-1
        public float[] hair;
        public string texture;     // PNG en base64 (vide une fois enregistre)
        public int views;
        public float error;

        public Vector3 Neck => neck != null && neck.Length == 3 ? new Vector3(neck[0], neck[1], neck[2]) : Vector3.zero;
        // Meme origine que le masque du visage (FaceMaskData) : centre du visage au ras du bout du nez
        public Vector3 FaceOrigin => nose != null && nose.Length == 3
            ? new Vector3(nose[0], (chin + brow) * 0.5f, nose[2]) : new Vector3(0f, (chin + brow) * 0.5f, 0.1f);
        public float FaceHeight => brow - chin;
        public Color Skin => ToColor(skin);
        public Color Hair => ToColor(hair);
        static Color ToColor(float[] c) => c != null && c.Length == 3 ? new Color(c[0], c[1], c[2]) : Color.white;
    }

    // Tete + texture, uniquement dans le stockage prive de l'application
    public static class HeadStore
    {
        static HeadData cachedData;
        static Texture2D cachedTexture;
        static Mesh cachedMesh;

        static string DataPath => Path.Combine(Application.persistentDataPath, "decoar_tete3d.json");
        static string TexturePath => Path.Combine(Application.persistentDataPath, "decoar_tete3d.png");

        public static bool Exists => cachedData != null || (File.Exists(DataPath) && File.Exists(TexturePath));

        // Reponse JSON du serveur -> fichiers ; false si la reponse est incomplete
        public static bool Save(string json)
        {
            var data = JsonUtility.FromJson<HeadData>(json);
            if (data == null || data.vertices == null || data.triangles == null || string.IsNullOrEmpty(data.texture))
                return false;

            byte[] png = Convert.FromBase64String(data.texture);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            if (!tex.LoadImage(png)) return false;

            data.texture = "";
            File.WriteAllText(DataPath, JsonUtility.ToJson(data));
            File.WriteAllBytes(TexturePath, png);
            cachedData = data;
            cachedTexture = tex;
            cachedMesh = null;
            return true;
        }

        public static bool TryLoad(out HeadData data, out Texture2D texture, out Mesh mesh)
        {
            if (cachedData == null && File.Exists(DataPath) && File.Exists(TexturePath))
            {
                cachedData = JsonUtility.FromJson<HeadData>(File.ReadAllText(DataPath));
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                if (tex.LoadImage(File.ReadAllBytes(TexturePath))) cachedTexture = tex;
                else cachedData = null;
            }

            if (cachedData != null && cachedMesh == null)
            {
                int n = cachedData.vertices.Length / 3;
                var v = new Vector3[n];
                var uv = new Vector2[n];
                for (int i = 0; i < n; i++)
                {
                    v[i] = new Vector3(cachedData.vertices[3 * i], cachedData.vertices[3 * i + 1], cachedData.vertices[3 * i + 2]);
                    uv[i] = new Vector2(cachedData.uv[2 * i], cachedData.uv[2 * i + 1]);
                }
                cachedMesh = new Mesh { name = "TeteFLAME" };
                cachedMesh.vertices = v;
                cachedMesh.uv = uv;
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
