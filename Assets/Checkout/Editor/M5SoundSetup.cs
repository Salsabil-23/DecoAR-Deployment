using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static DecoAR.M5.M5EditorUtil;

namespace DecoAR.M5
{
    /// Sons de la boutique (musique + effets de caisse) et point de départ de l'argent qui s'envole.
    /// Les sons sont dans Assets/Checkout/Audio : remplacez un fichier en gardant son nom pour changer le son.
    public static class M5SoundSetup
    {
        const string Dir = Root + "/Audio";

        [MenuItem("DecoAR/M5/11. Sons + argent de la caisse")]
        public static void Run()
        {
            var music = Clip("M5_Musique_Boutique", true);
            var item = Clip("M5_Article_Panier", false);
            var register = Clip("M5_Caisse_Ching", false);
            var whoosh = Clip("M5_Colis_Envol", false);
            var applause = Clip("M5_Applaudissements", false);

            var old = GameObject.Find("M5_Audio");
            if (old) Undo.DestroyObjectImmediate(old);
            var go = new GameObject("M5_Audio");
            Undo.RegisterCreatedObjectUndo(go, "M5 Sons");
            var a = go.AddComponent<M5ShopAudio>();
            a.music = music; a.item = item; a.register = register; a.whoosh = whoosh; a.applause = applause;

            // L'argent s'envole de la caisse enregistreuse
            var ui = Object.FindAnyObjectByType<M5CheckoutUI>();
            var desk = GameObject.Find("desk and stuff");
            var reg = desk ? desk.transform.Find("Cash register") : null;
            if (ui)
            {
                Undo.RecordObject(ui, "M5 Argent");
                ui.moneyOrigin = reg ? reg : (ui.basket ? ui.basket.transform : null);
                EditorUtility.SetDirty(ui);
            }

            EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log($"[M5] ✅ Sons installés (musique {(music ? "✓" : "✗")}, panier {(item ? "✓" : "✗")}, caisse {(register ? "✓" : "✗")}, colis {(whoosh ? "✓" : "✗")}, applaudissements {(applause ? "✓" : "✗")}). Argent depuis : {(ui && ui.moneyOrigin ? ui.moneyOrigin.name : "—")}.");
        }

        /// Réglages d'import adaptés au mobile : musique en streaming compressée, effets décompressés (latence nulle).
        static AudioClip Clip(string name, bool isMusic)
        {
            // .wav, .mp3, .ogg ou .aiff : le premier fichier portant ce nom
            string path = null;
            foreach (var ext in new[] { ".wav", ".mp3", ".ogg", ".aiff", ".aif" })
                if (System.IO.File.Exists($"{Dir}/{name}{ext}")) { path = $"{Dir}/{name}{ext}"; break; }
            if (path == null || !(AssetImporter.GetAtPath(path) is AudioImporter imp)) { Debug.LogWarning($"[M5] ⚠️ Son introuvable : {Dir}/{name}.(wav|mp3|ogg)"); return null; }
            var s = imp.defaultSampleSettings;
            if (isMusic) { s.loadType = AudioClipLoadType.Streaming; s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = 0.6f; imp.loadInBackground = true; }
            else { s.loadType = AudioClipLoadType.DecompressOnLoad; s.compressionFormat = AudioCompressionFormat.ADPCM; }
            imp.defaultSampleSettings = s;
            imp.forceToMono = true;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
