// DecoAR Design System — réglages d'import automatiques.
// À placer dans un dossier « Editor ». Agit sur tout fichier dont le nom commence par « dc- ».
//   dc-rect-rNN(.png / -contour)  -> Sprite, bordure 9-slice = NN + 1 px
//   dc-ombre-rNN                  -> Sprite, bordure 9-slice = NN + 40 px
//   dc-cercle-*                   -> Sprite, bordure 9-slice = moitié de la taille
//   dc-icon-*                     -> Sprite, sans compression, mipmaps (teinter via Image.color)
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

namespace DecoAR.Design.Editor
{
    public class DecoSpriteImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (!file.StartsWith("dc-")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = file.StartsWith("dc-icon-");
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.spritePixelsPerUnit = 100;

            var m = Regex.Match(file, @"^dc-rect-r(\d+)");
            if (m.Success) { int b = int.Parse(m.Groups[1].Value) + 1; ti.spriteBorder = new Vector4(b, b, b, b); return; }
            m = Regex.Match(file, @"^dc-ombre-r(\d+)");
            if (m.Success) { int b = int.Parse(m.Groups[1].Value) + 40; ti.spriteBorder = new Vector4(b, b, b, b); return; }
            m = Regex.Match(file, @"^dc-cercle-(\d+)");
            if (m.Success) { int b = int.Parse(m.Groups[1].Value) / 2 - 1; ti.spriteBorder = new Vector4(b, b, b, b); }
            if (file == "dc-voile-haut") ti.spriteBorder = Vector4.zero;
        }
    }
}
