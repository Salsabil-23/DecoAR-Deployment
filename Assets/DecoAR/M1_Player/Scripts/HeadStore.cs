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

        // Cheveux 3D (calotte qui suit le crane, bord transparent) ; vides si la personne n'a pas de cheveux
        public float[] hairVertices;
        public float[] hairUv;
        public int[] hairTriangles;
        public string hairTexture; // PNG RGBA en base64 (vide une fois enregistre)
        public float hairThickness;

        public bool HasHair => hairVertices != null && hairVertices.Length > 0 && hairTriangles != null && hairTriangles.Length > 0;

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
        static Texture2D cachedHairTexture;
        static Mesh cachedHairMesh;

        static string DataPath => Path.Combine(Application.persistentDataPath, "decoar_tete3d.json");
        static string TexturePath => Path.Combine(Application.persistentDataPath, "decoar_tete3d.png");
        static string HairTexturePath => Path.Combine(Application.persistentDataPath, "decoar_cheveux3d.png");

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

            cachedHairTexture = null;
            if (File.Exists(HairTexturePath)) File.Delete(HairTexturePath);
            if (data.HasHair && !string.IsNullOrEmpty(data.hairTexture))
            {
                byte[] hairPng = Convert.FromBase64String(data.hairTexture);
                var hairTex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                if (hairTex.LoadImage(hairPng))
                {
                    File.WriteAllBytes(HairTexturePath, hairPng);
                    cachedHairTexture = hairTex;
                }
            }

            data.texture = "";
            data.hairTexture = "";
            File.WriteAllText(DataPath, JsonUtility.ToJson(data));
            File.WriteAllBytes(TexturePath, png);
            cachedData = data;
            cachedTexture = tex;
            cachedMesh = null;
            cachedHairMesh = null;
            return true;
        }

        // Cheveux 3D : false si la personne n'en a pas (ou ancienne tete sans cheveux)
        public static bool TryLoadHair(out Texture2D texture, out Mesh mesh)
        {
            texture = null;
            mesh = null;
            if (!TryLoad(out var data, out _, out _) || !data.HasHair) return false;

            if (cachedHairTexture == null && File.Exists(HairTexturePath))
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                if (tex.LoadImage(File.ReadAllBytes(HairTexturePath))) cachedHairTexture = tex;
            }
            if (cachedHairTexture == null) return false;
            cachedHairTexture.wrapMode = TextureWrapMode.Clamp;

            if (cachedHairMesh == null)
                cachedHairMesh = BuildMesh("CheveuxFLAME", data.hairVertices, data.hairUv, data.hairTriangles);
            texture = cachedHairTexture;
            mesh = cachedHairMesh;
            return true;
        }

        static Mesh BuildMesh(string name, float[] vertices, float[] uvs, int[] triangles)
        {
            int n = vertices.Length / 3;
            var v = new Vector3[n];
            var uv = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                v[i] = new Vector3(vertices[3 * i], vertices[3 * i + 1], vertices[3 * i + 2]);
                uv[i] = new Vector2(uvs[2 * i], uvs[2 * i + 1]);
            }
            var mesh = new Mesh { name = name };
            mesh.vertices = v;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
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
                cachedMesh = BuildMesh("TeteFLAME", cachedData.vertices, cachedData.uv, cachedData.triangles);
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
            if (File.Exists(HairTexturePath)) File.Delete(HairTexturePath);
            cachedData = null;
            cachedTexture = null;
            cachedMesh = null;
            cachedHairTexture = null;
            cachedHairMesh = null;
        }
    }
}
