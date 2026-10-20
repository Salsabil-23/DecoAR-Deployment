using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static DecoAR.M5.M5EditorUtil;

namespace DecoAR.M5
{
    public static class M5BasketParcelSetup
    {
        [MenuItem("DecoAR/M5/3. Installer panier 3D + colis")]
        public static void Run()
        {
            var basket = FindBasket();
            var counter = FindCounter();
            var vendor = FindVendor();

            if (basket)
            {
                var bk = basket.GetComponent<Basket3D>();
                if (!bk) bk = Undo.AddComponent<Basket3D>(basket);
                Undo.RecordObject(bk, "M5 Basket");
                bk.itemMaterial = Mat("Article", Color.white, 0f, 0.4f);
                EditorUtility.SetDirty(bk);
            }

            var old = GameObject.Find("M5_Parcel");
            if (old) Undo.DestroyObjectImmediate(old);

            Vector3 pos; Vector3 front = Vector3.forward;
            if (counter)
            {
                var cb = GetBounds(counter);
                front = FrontOf(cb, counter, vendor);
            }
            var right = Vector3.Cross(Vector3.up, front);
            if (basket)
            {
                var bb = GetBounds(basket);
                pos = new Vector3(bb.center.x, bb.min.y, bb.center.z) + right * (bb.extents.x + bb.extents.z + 0.25f);
            }
            else if (counter)
            {
                var cb = GetBounds(counter);
                pos = new Vector3(cb.center.x, cb.max.y, cb.center.z) + right * cb.extents.x * 0.4f;
                Debug.LogWarning("[M5] Panier introuvable : colis posé sur le haut du comptoir, ajustez sa position.");
            }
            else pos = Vector3.zero;

            var model = FindByKeywords("Colis (modèle)", new[] { "cartton", "carton", "colis", "parcel" }, g => g.GetComponentInChildren<Renderer>(true));
            if (model && model == Selection.activeGameObject && !Has(model.name, new[] { "cartton", "carton", "colis", "parcel" })) model = null;
            if (model) { var mb = GetBounds(model); pos = new Vector3(mb.center.x, mb.min.y, mb.center.z); }
            var go = new GameObject("M5_Parcel");
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(front));
            var parcel = go.AddComponent<ParcelAnimation>();
            if (model) parcel.existingParcel = model.transform;
            parcel.kraftMaterial = Mat("Kraft", new Color(0.72f, 0.56f, 0.38f), 0f, 0.15f);
            parcel.tapeMaterial = Mat("Scotch", new Color(0.93f, 0.88f, 0.75f), 0f, 0.6f);
            if (vendor) parcel.vendor = vendor.GetComponent<VendorNPC>();
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false; src.spatialBlend = 0f;
            parcel.audioSource = src;
            var clipGuid = AssetDatabase.FindAssets("t:AudioClip", new[] { Root });
            if (clipGuid.Length > 0) parcel.confirmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(clipGuid[0]));
            else Debug.LogWarning($"[M5] ⚠️ Aucun son dans {Root} : assignez 'Confirm Clip' sur M5_Parcel.");

            Undo.RegisterCreatedObjectUndo(go, "M5 Colis");
            EditorSceneManager.MarkSceneDirty(go.scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = go;
            Debug.Log("[M5] ✅ Panier 3D + colis installés. Test : clic droit sur les composants (Play Mode) → 'Test : …'.");
        }
    }
}
