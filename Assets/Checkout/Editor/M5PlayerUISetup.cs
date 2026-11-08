using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static DecoAR.M5.M5EditorUtil;

namespace DecoAR.M5
{
    public static class M5PlayerUISetup
    {
        [MenuItem("DecoAR/M5/4. Créer Player + UI caisse")]
        public static void Run()
        {
            var counter = FindCounter();
            if (!counter) return;
            var vendor = FindVendor();
            var basket = FindBasket();
            var cb = GetBounds(counter);
            var front = FrontOf(cb, counter, vendor);
            LocalExtents(cb, front, out _, out float hd);
            float floorY = cb.min.y;

            // Collider du comptoir (le joueur ne le traverse pas)
            if (!counter.GetComponentInChildren<Collider>())
            {
                var box = Undo.AddComponent<BoxCollider>(counter);
                var t = counter.transform;
                box.center = t.InverseTransformPoint(cb.center);
                var sz = t.InverseTransformVector(cb.size);
                box.size = new Vector3(Mathf.Abs(sz.x), Mathf.Abs(sz.y), Mathf.Abs(sz.z));
            }

            // Sol (collider invisible si rien sous le joueur)
            var spawn = new Vector3(cb.center.x, floorY, cb.center.z) + front * (hd + 3f);
            if (!Physics.Raycast(spawn + Vector3.up * 2f, Vector3.down, 4f) && !GameObject.Find("M5_SolCollider"))
            {
                var floor = new GameObject("M5_SolCollider");
                floor.transform.position = new Vector3(cb.center.x, floorY - 0.05f, cb.center.z);
                floor.AddComponent<BoxCollider>().size = new Vector3(40f, 0.1f, 40f);
                Undo.RegisterCreatedObjectUndo(floor, "M5 Sol");
            }

            // Player
            var camGo = Camera.main ? Camera.main.gameObject : Object.FindAnyObjectByType<Camera>()?.gameObject;
            var old = GameObject.Find("M5_Player");
            if (old)
            {
                if (camGo && camGo.transform.IsChildOf(old.transform)) Undo.SetTransformParent(camGo.transform, null, "M5 Cam");
                Undo.DestroyObjectImmediate(old);
            }
            var player = new GameObject("M5_Player") { tag = "Player" };
            player.transform.SetPositionAndRotation(spawn, Quaternion.LookRotation(-front));
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.75f; cc.radius = 0.3f; cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.3f;
            Undo.RegisterCreatedObjectUndo(player, "M5 Player");

            if (!camGo)
            {
                camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
                Undo.RegisterCreatedObjectUndo(camGo, "M5 Cam");
            }
            Undo.SetTransformParent(camGo.transform, player.transform, "M5 Cam");
            Undo.RecordObject(camGo.transform, "M5 Cam");
            camGo.transform.localPosition = new Vector3(0, 1.6f, 0);
            camGo.transform.localRotation = Quaternion.Euler(12f, 0, 0);
            var cam = camGo.GetComponent<Camera>();
            Undo.RecordObject(cam, "M5 Cam"); cam.fieldOfView = 65f; cam.nearClipPlane = 0.05f;
            var pc = player.AddComponent<M5PlayerController>();
            pc.cam = camGo.transform;

            // UI caisse
            var oldUi = GameObject.Find("M5_CheckoutUI");
            if (oldUi) Undo.DestroyObjectImmediate(oldUi);
            var ui = new GameObject("M5_CheckoutUI");
            var c = ui.AddComponent<M5CheckoutUI>();
            c.zoneCenter = counter.transform;
            c.zoneRadius = hd + 2f;
            c.player = pc;
            if (basket) c.basket = basket.GetComponent<Basket3D>();
            if (vendor) c.vendor = vendor.GetComponent<VendorNPC>();
            var parcel = GameObject.Find("M5_Parcel");
            if (parcel) c.parcel = parcel.GetComponent<ParcelAnimation>();
            if (!c.basket) Debug.LogWarning("[M5] ⚠️ Basket3D absent : lancez d'abord le menu 3.");
            Undo.RegisterCreatedObjectUndo(ui, "M5 UI");

            // Le pivot du comptoir peut être décalé : la zone est centrée sur ses bounds
            var zone = new GameObject("M5_ZoneCaisse");
            zone.transform.position = new Vector3(cb.center.x, floorY, cb.center.z) + front * hd;
            zone.transform.SetParent(ui.transform, true);
            c.zoneCenter = zone.transform;

            EditorSceneManager.MarkSceneDirty(player.scene);
            Selection.activeGameObject = player;
            Debug.Log("[M5] ✅ Player + UI caisse créés. Play : ZQSD/flèches + clic droit (PC), glisser (mobile).");
        }

        [MenuItem("DecoAR/M5/Tout installer (1 → 4)")]
        public static void All()
        {
            M5VendorSetup.Run();
            M5CheckoutDecor.Run();
            M5BasketParcelSetup.Run();
            Run();
            Debug.Log("[M5] ✅ Installation complète — pensez à sauvegarder la scène (Ctrl/Cmd+S).");
        }

        [MenuItem("DecoAR/M5/Installer les nouveautés (5 → 7)")]
        public static void New()
        {
            M5AjSetup.Run();
            M5TVSetup.Run();
            M5AmbianceSetup.Run();
            Debug.Log("[M5] ✅ Aj, télévision et ambiance installés — sauvegardez la scène (Cmd+S) puis Play.");
        }
    }
}
