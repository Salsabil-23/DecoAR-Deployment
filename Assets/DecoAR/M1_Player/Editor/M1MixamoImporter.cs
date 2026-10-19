using UnityEditor;

namespace DecoAR.Player.EditorTools
{
    // Tout FBX depose dans M1_Player/Mixamo est importe en Humanoid ;
    // les animations idle / walk bouclent.
    public class M1MixamoImporter : AssetPostprocessor
    {
        public const string MixamoFolder = "Assets/DecoAR/M1_Player/Mixamo";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(MixamoFolder)) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(MixamoFolder)) return;
            var importer = (ModelImporter)assetImporter;
            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            bool loop = file.Contains("idle") || file.Contains("walk");

            var clips = importer.defaultClipAnimations;
            foreach (var c in clips)
            {
                c.loopTime = loop;
                c.lockRootRotation = true;
                c.lockRootHeightY = true;
                c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true;
                c.keepOriginalPositionY = true;
            }
            importer.clipAnimations = clips;
        }
    }
}
