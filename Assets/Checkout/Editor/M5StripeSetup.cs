using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DecoAR.M5
{
    /// Ajoute le paiement Stripe (via Supabase) à la caisse : il reste à coller l'URL de la fonction et la clé anon.
    public static class M5StripeSetup
    {
        [MenuItem("DecoAR/M5/9. Paiement Stripe (Supabase)")]
        public static void Run()
        {
            var ui = Object.FindAnyObjectByType<M5CheckoutUI>();
            if (!ui) { Debug.LogWarning("[M5] ⚠️ M5_CheckoutUI introuvable : lancez d'abord le menu 4."); return; }
            var pay = ui.GetComponent<M5StripePayment>();
            if (!pay) pay = Undo.AddComponent<M5StripePayment>(ui.gameObject);
            Undo.RecordObject(ui, "M5 Stripe");
            ui.payment = pay;
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
            Selection.activeGameObject = ui.gameObject;
            Debug.Log("[M5] ✅ Paiement Stripe ajouté sur M5_CheckoutUI : remplissez Function Url et Supabase Anon Key dans l'Inspector, puis Cmd+S.");
        }
    }
}
