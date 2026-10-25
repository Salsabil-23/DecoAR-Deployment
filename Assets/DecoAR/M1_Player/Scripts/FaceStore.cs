using System.IO;
using UnityEngine;

namespace DecoAR.Player
{
    // Photo du visage scanne : un seul fichier PNG, uniquement dans le stockage prive de l'application
    public static class FaceStore
    {
        static Texture2D cached;

        static string FilePath => Path.Combine(Application.persistentDataPath, "decoar_visage.png");

        public static bool Exists => cached != null || File.Exists(FilePath);

        public static void Save(Texture2D face)
        {
            File.WriteAllBytes(FilePath, face.EncodeToPNG());
            cached = face;
        }

        public static Texture2D Load()
        {
            if (cached != null) return cached;
            if (!File.Exists(FilePath)) return null;

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            if (!tex.LoadImage(File.ReadAllBytes(FilePath))) return null;
            tex.wrapMode = TextureWrapMode.Clamp;
            cached = tex;
            return tex;
        }

        public static void Delete()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            cached = null;
        }
    }
}
