using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using static DecoAR.M5.M5EditorUtil;

namespace DecoAR.M5
{
    /// Boutique complète de meubles et décoration autour de la caisse : murs, plafond, vitrines, rayons meublés,
    /// éclairage, panneaux et étiquettes de prix. Relançable : tout est reconstruit sous « M5_Boutique ».
    /// Les modèles utilisés sont déplacés des packs du store vers Assets/Checkout/Boutique (GUID conservés).
    public static class M5ShopBuilder
    {
        const string RootName = "M5_Boutique";
        const string ShopDir = Root + "/Boutique";
        static readonly string[] Packs = { "Assets/ithappy/Cute_Furniture_Free", "Assets/LowPolyLivingRoomPack" };

        // Pièce (m) : murs intérieurs alignés sur les axes, autour du comptoir existant
        const float X0 = -4.6f, X1 = 6.6f, Z0 = -9.2f, Z1 = 4.2f, H = 3.3f, T = 0.15f;
        static float F; // niveau du sol
        static float X0i => X0 + T / 2f; static float X1i => X1 - T / 2f;
        static float Z0i => Z0 + T / 2f; static float Z1i => Z1 - T / 2f;

        static readonly Dictionary<string, string> paths = new Dictionary<string, string>();
        static Transform root;
        static readonly Color Warm = new Color(1f, 0.84f, 0.64f);

        [MenuItem("DecoAR/M5/10. Boutique complète (murs + meubles)")]
        public static void Run()
        {
            var sol = GameObject.Find("M5_SolCollider");
            F = sol && sol.GetComponent<Collider>() ? sol.GetComponent<Collider>().bounds.max.y : 0f;

            MoveUsedAssets();
            IndexPrefabs();

            var old = GameObject.Find(RootName);
            if (old) Undo.DestroyObjectImmediate(old);
            root = new GameObject(RootName).transform;
            Undo.RegisterCreatedObjectUndo(root.gameObject, "M5 Boutique");

            BuildRoom();
            RearrangeCheckout();
            BuildCounter();
            BuildLiving();
            BuildDining();
            BuildBedroom();
            BuildKitchen();
            BuildTvWall();
            BuildStudy();
            BuildDecorIsland();
            BuildLights();
            BuildSigns();

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (!r.GetComponent<TMP_Text>()) GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic);

            var player = GameObject.Find("M5_Player");
            if (player) { Undo.RecordObject(player.transform, "M5 Joueur"); player.transform.rotation = Quaternion.identity; }

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root.gameObject;
            Debug.Log($"[M5] ✅ Boutique construite ({root.GetComponentsInChildren<Transform>().Length} objets). Sauvegardez (Cmd+S).");
        }

        // ---------- Assets ----------
        static readonly string[] Used =
        {
            "Couch_08", "Armchair_18", "Armchair_02", "Coffee_Table_03", "Light_05", "Plants_19", "Plants_15", "Picture_17", "Picture_08", "Picture_21",
            "Kitchen_Table_09", "Chair_17", "Bed_02", "Nightstand_02", "Closet_01", "Closet_02", "Fridge_01", "Kitchen_D_01", "Kitchen_D_06", "Kitchen_D_10",
            "Microwave_01", "Cutting_board_02", "Utensils_01", "Work_Table_06", "Chair_PC_04", "Computer_01", "Keyboard_01", "Mouse_01", "Clock_03", "Book_03",
            "Carpet_1", "Carpet_2", "Side_table_1", "Vase_1", "Vase_2", "Vase_3", "Night_light_3", "Bookshelf_Tall", "Pillow_Square_1", "Pillow_Square_2",
            "PottedPlant_Small_1", "PottedPlant_Small_2", "PottedPlant_Tall_1", "PottedPlant_Tall_2", "Stool_1", "Box_1", "Box_2", "Clock_WallRound", "Mirror",
            "Book_1", "Book_4", "Book_7", "Speaker_Soundbar", "Mug",
        };

        /// Déplace les prefabs utilisés + leurs dépendances (modèles, matériaux, textures) dans Assets/Checkout/Boutique.
        static void MoveUsedAssets()
        {
            int moved = 0;
            foreach (var pack in Packs)
            {
                if (!AssetDatabase.IsValidFolder(pack)) continue;
                string dest = ShopDir + "/" + (pack.Contains("ithappy") ? "ithappy" : "LowPolyLivingRoom");
                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { pack }))
                {
                    var p = AssetDatabase.GUIDToAssetPath(guid);
                    if (!Used.Contains(Path.GetFileNameWithoutExtension(p))) continue;
                    foreach (var dep in AssetDatabase.GetDependencies(p, true))
                    {
                        if (!dep.StartsWith(pack + "/")) continue;
                        string target = dest + dep.Substring(pack.Length);
                        EnsureFolder(Path.GetDirectoryName(target).Replace('\\', '/'));
                        var err = AssetDatabase.MoveAsset(dep, target);
                        if (string.IsNullOrEmpty(err)) moved++; else Debug.LogWarning($"[M5] Déplacement {dep} : {err}");
                    }
                }
            }
            if (moved > 0) Debug.Log($"[M5] {moved} fichier(s) déplacé(s) des packs vers {ShopDir}.");
        }

        static void IndexPrefabs()
        {
            paths.Clear();
            var folders = new List<string> { ShopDir }.Concat(Packs).Where(AssetDatabase.IsValidFolder).ToArray();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", folders))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var n = Path.GetFileNameWithoutExtension(p);
                if (!paths.ContainsKey(n) || p.StartsWith(ShopDir)) paths[n] = p;
            }
        }

        /// Pose un modèle : centre (x, z), base à y au-dessus du sol, tourné de rotY (l'avant des modèles = +Z).
        /// h = hauteur voulue (mise à l'échelle), s = échelle directe, snap = N/S/E/W pour coller au mur.
        static GameObject Put(string name, Transform parent, float x, float z, float rotY, float h = 0f, float y = 0f, string snap = null, bool col = false, float s = 0f)
        {
            if (!paths.TryGetValue(name, out var path)) { Debug.LogWarning("[M5] Modèle introuvable : " + name); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(0, rotY, 0);
            var b = GetBounds(go);
            if (s > 0f) go.transform.localScale *= s;
            else if (h > 0f && b.size.y > 0.0001f) go.transform.localScale *= h / b.size.y;
            b = GetBounds(go);
            go.transform.position += new Vector3(x - b.center.x, F + y - b.min.y, z - b.center.z);
            b = GetBounds(go);
            Vector3 d = Vector3.zero;
            switch (snap)
            {
                case "N": d.z = Z1i - 0.01f - b.max.z; break;
                case "S": d.z = Z0i + 0.01f - b.min.z; break;
                case "E": d.x = X1i - 0.01f - b.max.x; break;
                case "W": d.x = X0i + 0.01f - b.min.x; break;
            }
            go.transform.position += d;
            if (col) AddBox(go);
            return go;
        }

        static void AddBox(GameObject go)
        {
            var b = GetBounds(go);
            var box = go.AddComponent<BoxCollider>();
            var t = go.transform;
            box.center = t.InverseTransformPoint(b.center);
            var sz = t.InverseTransformVector(b.size);
            box.size = new Vector3(Mathf.Abs(sz.x), Mathf.Abs(sz.y), Mathf.Abs(sz.z));
        }

        static Transform Zone(string name) => Group(name, root, Vector3.zero).transform;

        // ---------- Pièce ----------
        static void BuildRoom()
        {
            var room = Zone("Piece");
            var wall = Mat("MurCreme", new Color(0.93f, 0.90f, 0.84f), 0f, 0.15f);
            var sage = Mat("VertSauge", new Color(0.62f, 0.69f, 0.58f), 0f, 0.2f);
            var wood = Mat("BoisClair", new Color(0.82f, 0.66f, 0.47f), 0f, 0.35f);
            var ceil = Mat("Plafond", new Color(0.96f, 0.95f, 0.92f), 0f, 0.1f);
            var panel = Mat("DalleLumineuse", new Color(1f, 0.97f, 0.9f), 0f, 0.5f, new Color(1f, 0.93f, 0.8f) * 1.6f);
            var glass = Mat("Vitrine", new Color(0.82f, 0.9f, 0.97f), 0f, 0.9f, new Color(0.78f, 0.88f, 1f) * 1.25f);
            var frame = Mat("Huisserie", new Color(0.24f, 0.21f, 0.18f), 0.3f, 0.5f);
            var brass = Mat("Laiton", new Color(0.83f, 0.67f, 0.33f), 1f, 0.7f);

            float cx = (X0 + X1) / 2f, cz = (Z0 + Z1) / 2f, w = X1 - X0, d = Z1 - Z0, my = F + H / 2f;
            Prim(PrimitiveType.Cube, "Mur_Nord", room, new Vector3(cx, my, Z1), new Vector3(w + T, H, T), wall, true);
            Prim(PrimitiveType.Cube, "Mur_Sud", room, new Vector3(cx, my, Z0), new Vector3(w + T, H, T), wall, true);
            Prim(PrimitiveType.Cube, "Mur_Est", room, new Vector3(X1, my, cz), new Vector3(T, H, d), wall, true);
            Prim(PrimitiveType.Cube, "Mur_Ouest", room, new Vector3(X0, my, cz), new Vector3(T, H, d), wall, true);

            // Mur d'accent sauge derrière la caisse, liseré laiton
            Prim(PrimitiveType.Cube, "Accent_Caisse", room, new Vector3(X0i + 0.01f, my, -1.8f), new Vector3(0.02f, H, 5.4f), sage);
            Prim(PrimitiveType.Cube, "Accent_Liseré", room, new Vector3(X0i + 0.025f, F + 2.25f, -1.8f), new Vector3(0.02f, 0.03f, 5.4f), brass);

            // Plinthes
            Prim(PrimitiveType.Cube, "Plinthe_N", room, new Vector3(cx, F + 0.05f, Z1i - 0.015f), new Vector3(w, 0.1f, 0.03f), wood);
            Prim(PrimitiveType.Cube, "Plinthe_S", room, new Vector3(cx, F + 0.05f, Z0i + 0.015f), new Vector3(w, 0.1f, 0.03f), wood);
            Prim(PrimitiveType.Cube, "Plinthe_E", room, new Vector3(X1i - 0.015f, F + 0.05f, cz), new Vector3(0.03f, 0.1f, d), wood);
            Prim(PrimitiveType.Cube, "Plinthe_O", room, new Vector3(X0i + 0.035f, F + 0.05f, cz), new Vector3(0.03f, 0.1f, d), wood);

            // Plafond (n'arrête pas le soleil : pas d'ombre) + dalles lumineuses
            var c = Prim(PrimitiveType.Cube, "Plafond", room, new Vector3(cx, F + H + 0.05f, cz), new Vector3(w + T, 0.1f, d + T), ceil);
            c.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            for (float x = X0 + 2f; x < X1 - 1f; x += 2.8f)
                for (float z = Z0 + 2f; z < Z1 - 1f; z += 3.2f)
                {
                    var p = Prim(PrimitiveType.Cube, "Dalle", room, new Vector3(x, F + H - 0.005f, z), new Vector3(0.9f, 0.01f, 0.9f), panel);
                    p.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }

            // Façade : deux vitrines + porte d'entrée (lumière du jour)
            Window(room, -1.15f, 1.8f, glass, frame);
            Window(room, 2.4f, 1.0f, glass, frame);
            var door = Group("Porte", room, new Vector3(0.9f, F, Z0i + 0.02f)).transform;
            Prim(PrimitiveType.Cube, "Cadre", door, new Vector3(0, 1.15f, 0), new Vector3(1.75f, 2.3f, 0.04f), frame);
            Prim(PrimitiveType.Cube, "Vitre_G", door, new Vector3(-0.42f, 1.12f, 0.025f), new Vector3(0.78f, 2.18f, 0.01f), glass);
            Prim(PrimitiveType.Cube, "Vitre_D", door, new Vector3(0.42f, 1.12f, 0.025f), new Vector3(0.78f, 2.18f, 0.01f), glass);
            Prim(PrimitiveType.Cube, "Poignee_G", door, new Vector3(-0.08f, 1.05f, 0.05f), new Vector3(0.03f, 0.5f, 0.03f), brass);
            Prim(PrimitiveType.Cube, "Poignee_D", door, new Vector3(0.08f, 1.05f, 0.05f), new Vector3(0.03f, 0.5f, 0.03f), brass);
            Rug(room, "Paillasson", new Vector3(0.9f, 0, Z0i + 0.75f), new Vector2(1.7f, 0.9f), new Color(0.33f, 0.25f, 0.18f), new Color(0.45f, 0.35f, 0.25f), "BIENVENUE");
        }

        static void Window(Transform parent, float x, float width, Material glass, Material frame)
        {
            var g = Group("Vitrine", parent, new Vector3(x, F, Z0i + 0.02f)).transform;
            Prim(PrimitiveType.Cube, "Cadre", g, new Vector3(0, 1.75f, 0), new Vector3(width + 0.12f, 1.82f, 0.04f), frame);
            Prim(PrimitiveType.Cube, "Vitre", g, new Vector3(0, 1.75f, 0.025f), new Vector3(width, 1.7f, 0.01f), glass);
            Prim(PrimitiveType.Cube, "Meneau", g, new Vector3(0, 1.75f, 0.035f), new Vector3(0.04f, 1.7f, 0.02f), frame);
            Prim(PrimitiveType.Cube, "Appui", g, new Vector3(0, 0.83f, 0.08f), new Vector3(width + 0.2f, 0.04f, 0.18f), frame);
        }

        // ---------- Caisse existante : remise à plat contre le mur ouest ----------
        static void RearrangeCheckout()
        {
            var decor = GameObject.Find("M5_CheckoutDecor");
            if (decor)
            {
                foreach (Transform t in decor.transform)
                {
                    if (t.name.StartsWith("PanneauMural")) { Undo.RecordObject(t.gameObject, "M5"); t.gameObject.SetActive(false); continue; }
                    Undo.RecordObject(t, "M5 Caisse");
                    var face = Quaternion.Euler(0, 90, 0); // tourné vers les clients (+X)
                    switch (t.name)
                    {
                        case "Etagere": t.rotation = face; Align(t, X0i + 0.03f, null, -1.6f); break;
                        case "Panneau_Caisse": t.rotation = face; t.position = new Vector3(X0i + 0.06f, F + 2.62f, -1.6f); break;
                        case "Presentoir_Colis": t.rotation = face; Align(t, X0i + 0.05f, F, -3.95f); break;
                        case "Plante_G": t.position = new Vector3(0.75f, F, 0.75f); break;
                        case "Plante_D": t.position = new Vector3(0.75f, F, -4.75f); break;
                        case "Tapis": t.rotation = Quaternion.Euler(0, 90, 0); t.position = new Vector3(1.35f, F + 0.005f, -2.0f); break;
                        case "Spot_Caisse":
                            t.position = new Vector3(-2.9f, F + H - 0.2f, -1.6f);
                            t.rotation = Quaternion.LookRotation(new Vector3(X0i, F + 2.3f, -1.6f) - t.position);
                            break;
                    }
                }
            }

            // Télévision « Voir le catalogue » contre le mur nord, écran vers la boutique
            var tv = FindInstanceOf(Root + "/Models/tv/tv.FBX") ?? GameObject.Find("tv");
            if (tv)
            {
                Undo.RecordObject(tv.transform, "M5 Télé");
                var scr = tv.GetComponentInChildren<M5TVScreen>(true);
                var outward = scr ? -scr.transform.forward : tv.transform.forward; outward.y = 0;
                var turn = Vector3.Dot(outward.normalized, Vector3.back) < -0.999f ? Quaternion.Euler(0, 180, 0) : Quaternion.FromToRotation(outward.normalized, Vector3.back);
                tv.transform.rotation = turn * tv.transform.rotation;
                var b = GetBounds(tv);
                tv.transform.position += new Vector3(1.8f - b.center.x, F + 1.75f - b.center.y, Z1i - 0.03f - b.max.z);
                if (scr) { Undo.RecordObject(scr, "M5 Télé"); scr.activationRadius = 4.5f; } // la télé est au fond d'une grande pièce
            }
        }

        static void Align(Transform t, float? minX, float? minY, float centerZ)
        {
            var b = GetBounds(t.gameObject);
            var d = new Vector3(minX.HasValue ? minX.Value - b.min.x : 0, minY.HasValue ? minY.Value - b.min.y : 0, centerZ - b.center.z);
            t.position += d;
        }

        // ---------- Comptoir habillé (autour du modèle d'origine, en L) ----------
        static void BuildCounter()
        {
            var deskRoot = GameObject.Find("desk and stuff");
            if (!deskRoot) return;
            var deskT = deskRoot.transform.Find("desk");
            var b = deskT && deskT.GetComponent<Renderer>() ? deskT.GetComponent<Renderer>().bounds : GetBounds(deskRoot);
            float top = b.max.y;
            float mainX0 = b.max.x - 1.11f;   // partie longue, côté clients
            float retZ1 = b.min.z + 1.21f;    // retour au sud (jusqu'au mur)
            var z = Zone("Comptoir");
            var cream = Mat("CremeComptoir", new Color(0.95f, 0.92f, 0.86f), 0f, 0.3f);
            var oak = Mat("ChenePale", new Color(0.86f, 0.70f, 0.50f), 0f, 0.4f);
            var marble = Mat("MarbreBlanc", new Color(0.96f, 0.95f, 0.93f), 0f, 0.78f);
            var brass = Mat("Laiton", new Color(0.83f, 0.67f, 0.33f), 1f, 0.7f);
            var walnut = Mat("Noyer", new Color(0.33f, 0.22f, 0.15f), 0f, 0.45f);
            var led = Mat("LedChaude", new Color(1f, 0.9f, 0.75f), 0f, 0.5f, new Color(1f, 0.8f, 0.55f) * 3.2f);

            // Façades : lattes de chêne côté public, crème uni côté vendeur
            Face(z, new Vector3(b.max.x, 0, b.center.z), b.size.z, Vector3.right, true, cream, oak, walnut, top);
            Face(z, new Vector3((mainX0 + b.max.x) / 2f, 0, b.max.z), b.max.x - mainX0, Vector3.forward, true, cream, oak, walnut, top);
            Face(z, new Vector3(b.center.x, 0, b.min.z), b.size.x, Vector3.back, true, cream, oak, walnut, top);
            Face(z, new Vector3(b.min.x, 0, (b.min.z + retZ1) / 2f), retZ1 - b.min.z, Vector3.left, true, cream, oak, walnut, top);
            Face(z, new Vector3(mainX0, 0, (retZ1 + b.max.z) / 2f), b.max.z - retZ1, Vector3.left, false, cream, oak, walnut, top);
            Face(z, new Vector3((b.min.x + mainX0) / 2f, 0, retZ1), mainX0 - b.min.x, Vector3.forward, false, cream, oak, walnut, top);

            // Plateaux en marbre (débord de 5 cm), chant laiton, bandeau lumineux sous le débord
            const float o = 0.05f, th = 0.06f;
            float ty = top + 0.003f - th / 2f;
            Prim(PrimitiveType.Cube, "Plateau", z, new Vector3((mainX0 + b.max.x + o) / 2f, ty, b.center.z), new Vector3(b.max.x + o - mainX0, th, b.size.z + 2 * o), marble);
            Prim(PrimitiveType.Cube, "Plateau_Retour", z, new Vector3((b.min.x - o + mainX0) / 2f, ty, (b.min.z - o + retZ1) / 2f), new Vector3(mainX0 - b.min.x + o, th, retZ1 - b.min.z + o), marble);
            Prim(PrimitiveType.Cube, "Chant_Est", z, new Vector3(b.max.x + o + 0.004f, ty, b.center.z), new Vector3(0.008f, th, b.size.z + 2 * o), brass);
            Prim(PrimitiveType.Cube, "Chant_Sud", z, new Vector3(b.center.x, ty, b.min.z - o - 0.004f), new Vector3(b.size.x + 2 * o, th, 0.008f), brass);
            var l = Prim(PrimitiveType.Cube, "Bandeau_LED", z, new Vector3(b.max.x + 0.04f, top - th - 0.012f, b.center.z), new Vector3(0.015f, 0.012f, b.size.z - 0.1f), led);
            l.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            // Plaque « DecoAR » en façade
            var plate = Group("Plaque_DecoAR", z, new Vector3(b.max.x + 0.075f, F + 0.62f, b.center.z)).transform;
            Prim(PrimitiveType.Cube, "Cadre", plate, Vector3.zero, new Vector3(0.02f, 0.36f, 1.25f), brass);
            Prim(PrimitiveType.Cube, "Fond", plate, new Vector3(0.006f, 0, 0), new Vector3(0.02f, 0.31f, 1.2f), walnut);
            Sign(plate, "DecoAR", plate.position + new Vector3(0.02f, 0, 0), -90f, 1.05f, new Color(0.9f, 0.74f, 0.4f), 0.26f);

            // Sur le plateau : terminal de paiement, bouquet, paquets cadeaux, petit mot
            Terminal(z, new Vector3(b.max.x - 0.28f, top, b.center.z + 0.85f));
            Bouquet(z, new Vector3(b.max.x - 0.3f, top, b.min.z + 0.4f));
            Gifts(z, new Vector3(b.min.x + 0.35f, top, b.min.z + 0.55f));
            StyleBasket(z);
            var merci = Group("Merci", z, new Vector3(b.max.x - 0.12f, top, b.max.z - 0.15f)).transform;
            Prim(PrimitiveType.Cube, "Socle", merci, new Vector3(0, 0.01f, 0), new Vector3(0.06f, 0.02f, 0.26f), brass);
            Prim(PrimitiveType.Cube, "Carte", merci, new Vector3(0, 0.12f, 0), new Vector3(0.006f, 0.2f, 0.28f), cream);
            Sign(merci, "Merci de votre\nvisite !", merci.position + new Vector3(0.006f, 0.12f, 0), -90f, 0.25f, new Color(0.3f, 0.24f, 0.18f), 0.17f);
        }

        /// Panier 3D du comptoir aux couleurs de la boutique : osier tressé, anse en cuir, étiquette « DecoAR ».
        static void StyleBasket(Transform parent)
        {
            var basket = Object.FindAnyObjectByType<Basket3D>();
            if (!basket) return;
            var wicker = Mat("Osier", Color.white, 0f, 0.25f);
            wicker.SetTexture("_BaseMap", WickerTexture());
            wicker.SetTextureScale("_BaseMap", new Vector2(3f, 3f));
            var leather = Mat("Cuir", new Color(0.42f, 0.25f, 0.15f), 0f, 0.45f);
            foreach (var r in basket.GetComponentsInChildren<Renderer>())
            {
                if (r.GetComponent<TMPro.TMP_Text>() || r.name.StartsWith("M5_")) continue;
                Undo.RecordObject(r, "M5 Panier");
                var m = r.name.ToLowerInvariant().Contains("handle") ? leather : wicker;
                r.sharedMaterials = Enumerable.Repeat(m, r.sharedMaterials.Length).ToArray();
            }
            // Étiquette côté clients (+X)
            var b = GetBounds(basket.gameObject);
            var tag = Group("Etiquette_Panier", parent, new Vector3(b.max.x + 0.012f, b.center.y - 0.02f, b.center.z)).transform;
            Prim(PrimitiveType.Cube, "Cadre", tag, Vector3.zero, new Vector3(0.012f, 0.13f, 0.36f), Mat("Laiton", new Color(0.83f, 0.67f, 0.33f), 1f, 0.7f));
            Prim(PrimitiveType.Cube, "Fond", tag, new Vector3(0.004f, 0, 0), new Vector3(0.01f, 0.11f, 0.34f), Mat("Noyer", new Color(0.33f, 0.22f, 0.15f), 0f, 0.45f));
            Sign(tag, "DecoAR", tag.position + new Vector3(0.011f, 0, 0), -90f, 0.3f, new Color(0.9f, 0.74f, 0.4f), 0.09f);
        }

        /// Tressage d'osier (bandes croisées, tons miel).
        static Texture2D WickerTexture()
        {
            string path = Root + "/Materials/M5_Osier.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing) return existing;
            const int n = 128, cell = 16;
            var tex = new Texture2D(n, n, TextureFormat.RGB24, false);
            var light = new Color(0.86f, 0.70f, 0.47f); var dark = new Color(0.62f, 0.45f, 0.27f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int cx = x / cell, cy = y / cell;
                    bool horiz = (cx + cy) % 2 == 0;
                    float u = (horiz ? y % cell : x % cell) / (float)cell;      // travers du brin
                    float shade = Mathf.Sin(u * Mathf.PI);                       // brin arrondi
                    float along = (horiz ? x : y) * 0.35f;
                    var c = Color.Lerp(dark, light, shade * 0.85f + 0.1f * Mathf.Sin(along));
                    if (u < 0.08f || u > 0.92f) c *= 0.7f;                       // creux entre brins
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.wrapMode = TextureWrapMode.Repeat; ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// Façade : panneau + plinthe en retrait + lattes verticales (si public).
        static void Face(Transform parent, Vector3 edge, float span, Vector3 outward, bool slats, Material cream, Material oak, Material walnut, float top)
        {
            bool alongZ = Mathf.Abs(outward.x) > 0.5f;
            float h = top - 0.06f - F;
            var c = edge + outward * 0.015f; c.y = F + h / 2f;
            Vector3 Size(float thick, float hh, float len) => alongZ ? new Vector3(thick, hh, len) : new Vector3(len, hh, thick);
            Prim(PrimitiveType.Cube, "Facade", parent, c, Size(0.03f, h, span), cream);
            var kick = edge + outward * 0.005f; kick.y = F + 0.05f;
            Prim(PrimitiveType.Cube, "Plinthe", parent, kick, Size(0.03f, 0.1f, span), walnut);
            if (!slats) return;
            var dir = alongZ ? Vector3.forward : Vector3.right;
            int n = Mathf.FloorToInt((span - 0.1f) / 0.11f);
            float start = -(n - 1) * 0.11f / 2f;
            for (int i = 0; i < n; i++)
            {
                var p = edge + outward * 0.04f + dir * (start + i * 0.11f); p.y = F + 0.1f + (h - 0.1f) / 2f;
                Prim(PrimitiveType.Cube, "Latte", parent, p, Size(0.02f, h - 0.1f, 0.045f), oak);
            }
        }

        static void Terminal(Transform parent, Vector3 pos)
        {
            var dark = Mat("Terminal", new Color(0.12f, 0.12f, 0.13f), 0.2f, 0.6f);
            var screen = Mat("EcranTerminal", new Color(0.35f, 0.6f, 0.5f), 0f, 0.8f, new Color(0.4f, 0.75f, 0.6f) * 1.4f);
            var g = Group("Terminal_Paiement", parent, pos).transform;
            g.rotation = Quaternion.Euler(0, 90, 0); // écran vers les clients (+X)
            Prim(PrimitiveType.Cube, "Socle", g, new Vector3(0, 0.012f, 0), new Vector3(0.11f, 0.024f, 0.14f), dark);
            var body = Group("Corps", g, new Vector3(0, 0.02f, -0.02f)).transform;
            body.localRotation = Quaternion.Euler(-22f, 0, 0);
            Prim(PrimitiveType.Cube, "Boitier", body, new Vector3(0, 0.085f, 0), new Vector3(0.085f, 0.17f, 0.03f), dark);
            Prim(PrimitiveType.Cube, "Ecran", body, new Vector3(0, 0.12f, 0.016f), new Vector3(0.065f, 0.06f, 0.002f), screen);
            for (int r = 0; r < 3; r++)
                for (int k = 0; k < 3; k++)
                    Prim(PrimitiveType.Cube, "Touche", body, new Vector3((k - 1) * 0.022f, 0.035f + r * 0.018f, 0.016f), new Vector3(0.016f, 0.012f, 0.003f), Mat("Touche", new Color(0.8f, 0.8f, 0.78f), 0f, 0.4f));
        }

        static void Bouquet(Transform parent, Vector3 pos)
        {
            var terra = Mat("Terracotta", new Color(0.76f, 0.45f, 0.32f), 0f, 0.2f);
            var leaf = Mat("Feuillage", new Color(0.30f, 0.50f, 0.28f), 0f, 0.25f);
            Material[] petals = { Mat("FleurRose", new Color(0.93f, 0.62f, 0.66f), 0f, 0.3f), Mat("Creme", new Color(0.97f, 0.94f, 0.87f), 0f, 0.3f), Mat("FleurCorail", new Color(0.95f, 0.5f, 0.38f), 0f, 0.3f) };
            var g = Group("Bouquet", parent, pos).transform;
            Prim(PrimitiveType.Cylinder, "Vase", g, new Vector3(0, 0.11f, 0), new Vector3(0.13f, 0.11f, 0.13f), terra);
            var rnd = new System.Random(3);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 0.9f, r = 0.035f + (float)rnd.NextDouble() * 0.03f, hh = 0.32f + (float)rnd.NextDouble() * 0.12f;
                var top = new Vector3(Mathf.Cos(a) * r * 2.2f, hh, Mathf.Sin(a) * r * 2.2f);
                var stem = Prim(PrimitiveType.Cylinder, "Tige", g, (new Vector3(0, 0.2f, 0) + top) / 2f, new Vector3(0.008f, (hh - 0.2f) / 2f + 0.02f, 0.008f), leaf);
                stem.transform.localRotation = Quaternion.FromToRotation(Vector3.up, top - new Vector3(0, 0.2f, 0));
                Prim(PrimitiveType.Sphere, "Fleur", g, top, Vector3.one * (0.055f + (float)rnd.NextDouble() * 0.02f), petals[i % petals.Length]);
                if (i % 2 == 0) Prim(PrimitiveType.Sphere, "Feuille", g, (new Vector3(0, 0.2f, 0) + top) / 2f + new Vector3(0.02f, 0, 0), new Vector3(0.05f, 0.015f, 0.03f), leaf);
            }
        }

        static void Gifts(Transform parent, Vector3 pos)
        {
            var g = Group("Paquets_Cadeaux", parent, pos).transform;
            var mats = new[] { Mat("Rosepoudre", new Color(0.86f, 0.66f, 0.64f), 0f, 0.3f), Mat("VertSauge", new Color(0.62f, 0.69f, 0.58f), 0f, 0.2f), Mat("Kraft", new Color(0.72f, 0.56f, 0.38f), 0f, 0.15f) };
            var brass = Mat("Laiton", new Color(0.83f, 0.67f, 0.33f), 1f, 0.7f);
            Vector3[] sizes = { new Vector3(0.26f, 0.14f, 0.22f), new Vector3(0.19f, 0.11f, 0.16f), new Vector3(0.12f, 0.09f, 0.12f) };
            float y = 0f;
            for (int i = 0; i < sizes.Length; i++)
            {
                var c = Prim(PrimitiveType.Cube, "Paquet", g, new Vector3(0, y + sizes[i].y / 2f, 0), sizes[i], mats[i]);
                c.transform.localRotation = Quaternion.Euler(0, i * 17f - 10f, 0);
                Prim(PrimitiveType.Cube, "Ruban_A", c.transform, Vector3.zero, new Vector3(0.14f, 1.02f, 1.02f), brass);
                Prim(PrimitiveType.Cube, "Ruban_B", c.transform, Vector3.zero, new Vector3(1.02f, 1.02f, 0.14f), brass);
                y += sizes[i].y;
            }
        }

        // ---------- Rayons ----------
        static void BuildLiving()
        {
            var z = Zone("Rayon_Salon");
            Rug(z, "Tapis_Salon", new Vector3(4.75f, 0, 0.8f), new Vector2(2.4f, 3.3f), new Color(0.76f, 0.45f, 0.32f), new Color(0.95f, 0.91f, 0.83f));
            Put("Couch_08", z, 0, 0.8f, -90f, snap: "E", col: true);
            Put("Pillow_Square_1", z, X1i - 0.4f, 0.3f, -75f, s: 3.6f, y: 0.45f);
            Put("Pillow_Square_2", z, X1i - 0.4f, 1.3f, -105f, s: 3.0f, y: 0.45f);
            Put("Coffee_Table_03", z, 4.7f, 0.8f, 0f, col: true);
            Put("Vase_2", z, 4.6f, 0.7f, 0f, h: 0.32f, y: 0.42f);
            Put("Book_03", z, 4.85f, 0.95f, 20f, h: 0.12f, y: 0.42f);
            Put("Armchair_18", z, 3.3f, -0.15f, 70f, col: true);
            Put("Armchair_18", z, 3.3f, 1.75f, 110f, col: true);
            Put("Light_05", z, 6.15f, -0.75f, 0f, col: true);
            Put("Side_table_1", z, 6.2f, 2.45f, -90f, h: 0.55f, col: true);
            Put("Vase_3", z, 6.2f, 2.45f, 0f, h: 0.3f, y: 0.55f);
            Put("Plants_19", z, 6.0f, 3.55f, 0f, h: 2.3f, col: true);
            Put("Picture_17", z, 0, 0.3f, -90f, y: 1.25f, snap: "E");
            Put("Picture_08", z, 0, 1.35f, -90f, y: 1.4f, snap: "E");
            PriceTag(z, "Canapé Nova", "2 490 TND", new Vector3(5.2f, F, -0.65f), 200f);
            PriceTag(z, "Fauteuil Cosy", "690 TND", new Vector3(2.65f, F, 0.8f), 250f);
        }

        static void BuildDining()
        {
            var z = Zone("Rayon_SalleAManger");
            Put("Carpet_1", z, 3.4f, -3.2f, 0f, s: 4.0f);
            Put("Kitchen_Table_09", z, 3.4f, -3.2f, 0f, col: true);
            Put("Chair_17", z, 2.95f, -2.55f, 180f, col: true);
            Put("Chair_17", z, 3.85f, -2.55f, 180f, col: true);
            Put("Chair_17", z, 2.95f, -3.85f, 0f, col: true);
            Put("Chair_17", z, 3.85f, -3.85f, 0f, col: true);
            Put("Vase_1", z, 3.2f, -3.2f, 0f, h: 0.2f, y: 0.75f);
            Put("PottedPlant_Small_2", z, 3.65f, -3.15f, 0f, h: 0.28f, y: 0.75f);
            PriceTag(z, "Table Olivia + 4 chaises", "1 350 TND", new Vector3(2.0f, F, -3.2f), 270f);
        }

        static void BuildBedroom()
        {
            var z = Zone("Rayon_Chambre");
            Put("Carpet_1", z, 4.55f, -6.4f, 90f, s: 4.6f);
            Put("Bed_02", z, 0, -6.4f, -90f, snap: "E", col: true);
            Put("Nightstand_02", z, 0, -7.75f, -90f, snap: "E", col: true);
            Put("Nightstand_02", z, 0, -5.05f, -90f, snap: "E", col: true);
            Put("Night_light_3", z, X1i - 0.25f, -7.75f, 0f, h: 0.42f, y: 0.57f);
            Put("Night_light_3", z, X1i - 0.25f, -5.05f, 0f, h: 0.42f, y: 0.57f);
            Put("Picture_21", z, 0, -6.4f, -90f, h: 0.85f, y: 1.35f, snap: "E");
            Put("Closet_02", z, 4.45f, 0, 0f, snap: "S", col: true);
            Put("Closet_01", z, 3.45f, 0, 0f, snap: "S", col: true);
            Put("PottedPlant_Tall_1", z, 6.15f, -8.75f, 0f, h: 1.1f, col: true);
            PriceTag(z, "Lit Céleste 160", "1 890 TND", new Vector3(3.15f, F, -5.3f), 250f);
        }

        static void BuildKitchen()
        {
            var z = Zone("Rayon_Cuisine");
            Put("Fridge_01", z, -4.1f, 0, 180f, snap: "N", col: true);
            Put("Kitchen_D_01", z, -3.27f, 0, 180f, snap: "N", col: true);
            Put("Kitchen_D_01", z, -2.35f, 0, 180f, snap: "N", col: true);
            Put("Kitchen_D_10", z, -3.27f, 0, 180f, y: 1.45f, snap: "N");
            Put("Kitchen_D_10", z, -2.35f, 0, 180f, y: 1.45f, snap: "N");
            Put("Kitchen_D_01", z, -1.43f, 0, 180f, snap: "N", col: true);
            Put("Kitchen_D_10", z, -1.43f, 0, 180f, y: 1.45f, snap: "N");
            Put("Kitchen_D_10", z, -4.1f, 0, 180f, y: 1.45f, snap: "N");
            Put("PottedPlant_Small_2", z, -1.6f, Z1i - 0.3f, 0f, h: 0.3f, y: 0.85f);
            Put("Vase_1", z, -1.2f, Z1i - 0.25f, 0f, h: 0.22f, y: 0.85f);
            Put("Microwave_01", z, -2.35f, Z1i - 0.3f, 180f, y: 0.85f);
            Put("Cutting_board_02", z, -3.15f, Z1i - 0.35f, 90f, y: 0.85f);
            Put("Utensils_01", z, -3.27f, Z1i - 0.06f, 180f, y: 1.2f);
            Put("Kitchen_D_06", z, -2.8f, 2.15f, 0f, col: true);
            Put("Vase_2", z, -2.95f, 2.1f, 0f, h: 0.3f, y: 0.75f);
            Put("Mug", z, -2.6f, 2.25f, 30f, h: 0.11f, y: 0.75f);
            Put("Stool_1", z, -3.25f, 1.45f, 0f, h: 0.65f, col: true);
            Put("Stool_1", z, -2.35f, 1.45f, 0f, h: 0.65f, col: true);
            PriceTag(z, "Cuisine Corail", "6 900 TND", new Vector3(-0.95f, F, 2.35f), 120f);
        }

        static void BuildTvWall()
        {
            var z = Zone("Mur_Catalogue");
            var wood = Mat("BoisClair", new Color(0.82f, 0.66f, 0.47f), 0f, 0.35f);
            var dark = Mat("Huisserie", new Color(0.24f, 0.21f, 0.18f), 0.3f, 0.5f);
            var meuble = Group("MeubleTV", z, new Vector3(1.8f, F, Z1i - 0.25f)).transform;
            Prim(PrimitiveType.Cube, "Caisson", meuble, new Vector3(0, 0.3f, 0), new Vector3(2.6f, 0.36f, 0.42f), wood, true);
            for (int i = -1; i <= 1; i += 2)
                for (int k = -1; k <= 1; k += 2)
                    Prim(PrimitiveType.Cube, "Pied", meuble, new Vector3(i * 1.2f, 0.06f, k * 0.15f), new Vector3(0.04f, 0.12f, 0.04f), dark);
            Put("Speaker_Soundbar", z, 1.8f, Z1i - 0.18f, 0f, h: 0.08f, y: 0.48f);
            Put("Vase_3", z, 0.75f, Z1i - 0.25f, 0f, h: 0.34f, y: 0.48f);
            Put("Book_1", z, 2.75f, Z1i - 0.25f, 0f, h: 0.24f, y: 0.48f);
            Put("Book_4", z, 2.82f, Z1i - 0.25f, 0f, h: 0.26f, y: 0.48f);
            Put("Book_7", z, 2.89f, Z1i - 0.25f, 0f, h: 0.22f, y: 0.48f);
            Put("Plants_15", z, -0.05f, 3.7f, 0f, h: 1.35f, col: true);
            Put("Plants_15", z, 3.7f, 3.7f, 0f, h: 1.35f, col: true);
            Put("Clock_03", z, 4.55f, 0, 180f, snap: "N", col: true);
        }

        static void BuildStudy()
        {
            var z = Zone("Rayon_Bureau");
            var shelf = Put("Bookshelf_Tall", z, 0, -6.1f, 90f, h: 2.1f, snap: "W", col: true);
            FillShelf(z, shelf);
            Put("Closet_01", z, 0, -7.85f, 90f, snap: "W", col: true);
            Put("Work_Table_06", z, -2.9f, 0, 0f, snap: "S", col: true);
            Put("Computer_01", z, -2.9f, Z0i + 0.32f, 0f, y: 0.76f);
            Put("Keyboard_01", z, -2.95f, Z0i + 0.55f, 0f, y: 0.76f);
            Put("Mouse_01", z, -2.6f, Z0i + 0.55f, 0f, y: 0.76f);
            Put("Chair_PC_04", z, -2.9f, -7.9f, 180f, col: true);
            Put("Armchair_02", z, -3.45f, -5.15f, -120f, col: true);
            Put("Light_05", z, -4.15f, -4.85f, 0f, col: true);
            Put("Mirror", z, 0, -7.85f, 90f, h: 0.9f, y: 1.6f, snap: "W");
            PriceTag(z, "Bureau Atelier", "790 TND", new Vector3(-1.6f, F, -8.2f), 160f);
        }

        /// Îlot déco : podiums avec vases, plantes, boîtes (accessoires de la maison).
        static void BuildDecorIsland()
        {
            var z = Zone("Ilot_Deco");
            var cream = Mat("Creme", new Color(0.97f, 0.94f, 0.87f), 0f, 0.3f);
            var terra = Mat("Terracotta", new Color(0.76f, 0.45f, 0.32f), 0f, 0.2f);
            var wood = Mat("BoisClair", new Color(0.82f, 0.66f, 0.47f), 0f, 0.35f);
            var c = new Vector3(2.85f, F, -6.3f);
            var a = Prim(PrimitiveType.Cylinder, "Podium_Haut", z, c + new Vector3(0, 0.45f, 0), new Vector3(0.7f, 0.45f, 0.7f), cream, true);
            var b = Prim(PrimitiveType.Cylinder, "Podium_Moyen", z, c + new Vector3(0.6f, 0.3f, 0.35f), new Vector3(0.6f, 0.3f, 0.6f), wood, true);
            var d = Prim(PrimitiveType.Cylinder, "Podium_Bas", z, c + new Vector3(-0.55f, 0.18f, 0.4f), new Vector3(0.6f, 0.18f, 0.6f), terra, true);
            Put("Vase_2", z, c.x, c.z, 0f, h: 0.42f, y: 0.9f);
            Put("PottedPlant_Tall_2", z, c.x + 0.6f, c.z + 0.35f, 0f, h: 0.55f, y: 0.6f);
            Put("Vase_1", z, c.x - 0.62f, c.z + 0.38f, 0f, h: 0.22f, y: 0.36f);
            Put("Box_1", z, c.x - 0.45f, c.z + 0.5f, 25f, h: 0.2f, y: 0.36f);
            Put("PottedPlant_Small_1", z, c.x + 0.2f, c.z - 0.15f, 0f, h: 0.2f, y: 0.9f);
            Put("Box_2", z, c.x + 0.5f, c.z - 0.1f, -15f, h: 0.18f, y: 0f);
            PriceTag(z, "Collection Terracotta", "dès 25 TND", new Vector3(c.x - 0.9f, F, c.z - 0.55f), 210f);
        }

        // ---------- Lumière ----------
        static void BuildLights()
        {
            var z = Zone("Eclairage");
            var bulb = Mat("Ampoule", new Color(1f, 0.92f, 0.75f), 0f, 0.5f, new Color(1f, 0.8f, 0.5f) * 2f);
            var brass = Mat("Laiton", new Color(0.83f, 0.67f, 0.33f), 1f, 0.7f);
            Vector3[] spots = { new Vector3(4.8f, 0, 0.8f), new Vector3(3.4f, 0, -3.2f), new Vector3(4.6f, 0, -6.4f), new Vector3(-2.8f, 0, 2.6f), new Vector3(0.9f, 0, -7.0f), new Vector3(-2.9f, 0, -6.9f), new Vector3(2.85f, 0, -6.3f) };
            foreach (var p in spots)
            {
                var pos = new Vector3(p.x, F + H - 0.7f, p.z);
                var l = AddLight("Lumiere", z, pos, LightType.Point, 4.8f, 1.15f, Warm);
                l.shadows = LightShadows.None;
            }
            // Suspension au-dessus de la table
            var s = Group("Suspension_Table", z, new Vector3(3.4f, F + H, -3.2f)).transform;
            Prim(PrimitiveType.Cylinder, "Fil", s, new Vector3(0, -0.55f, 0), new Vector3(0.01f, 0.55f, 0.01f), brass);
            Prim(PrimitiveType.Cylinder, "Abat-jour", s, new Vector3(0, -1.12f, 0), new Vector3(0.5f, 0.07f, 0.5f), brass);
            Prim(PrimitiveType.Sphere, "Ampoule", s, new Vector3(0, -1.22f, 0), Vector3.one * 0.16f, bulb);
        }

        /// Tapis tissé : bordure + centre (+ texte facultatif).
        static void Rug(Transform parent, string name, Vector3 center, Vector2 size, Color border, Color inner, string text = null)
        {
            var g = Group(name, parent, new Vector3(center.x, F, center.z)).transform;
            Prim(PrimitiveType.Cube, "Bordure", g, new Vector3(0, 0.006f, 0), new Vector3(size.x, 0.012f, size.y), Mat("Tapis_" + ColorUtility.ToHtmlStringRGB(border), border, 0f, 0.05f));
            Prim(PrimitiveType.Cube, "Centre", g, new Vector3(0, 0.008f, 0), new Vector3(size.x - 0.22f, 0.012f, size.y - 0.22f), Mat("Tapis_" + ColorUtility.ToHtmlStringRGB(inner), inner, 0f, 0.05f));
            if (text == null) return;
            var tg = new GameObject("Texte");
            tg.transform.SetParent(g, false);
            tg.transform.localPosition = new Vector3(0, 0.016f, 0);
            tg.transform.localRotation = Quaternion.Euler(90, 0, 0); // couché, lisible en entrant
            var t = tg.AddComponent<TextMeshPro>();
            t.rectTransform.sizeDelta = new Vector2(size.x - 0.3f, size.y - 0.3f);
            t.text = text; t.color = new Color(0.86f, 0.76f, 0.58f); t.alignment = TextAlignmentOptions.Center;
            t.enableAutoSizing = true; t.fontSizeMin = 0.3f; t.fontSizeMax = 3f; t.characterSpacing = 15; t.fontStyle = FontStyles.Bold;
        }

        /// Garnit une étagère : trouve la hauteur de chaque tablette (rayons), puis y range livres, boîtes et plantes.
        static void FillShelf(Transform parent, GameObject shelf)
        {
            if (!shelf) return;
            var temp = new List<Collider>();
            foreach (var mf in shelf.GetComponentsInChildren<MeshFilter>()) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; temp.Add(mc); }
            Physics.SyncTransforms();
            var b = GetBounds(shelf);
            var levels = new List<float>();
            float yTop = b.max.y - 0.03f;
            var start = new Vector3(b.center.x, yTop, b.center.z);
            while (start.y > b.min.y + 0.3f && Physics.Raycast(start, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (temp.Contains(hit.collider) && hit.point.y > b.min.y + 0.3f && hit.point.y < b.max.y - 0.1f) levels.Add(hit.point.y);
                start = hit.point - Vector3.up * 0.03f;
            }
            foreach (var c in temp) Object.DestroyImmediate(c);

            string[] books = { "Book_1", "Book_4", "Book_7" };
            string[] deco = { "Vase_3", "Box_1", "PottedPlant_Small_1", "Box_2", "Vase_1" };
            int k = 0;
            foreach (var y in levels)
            {
                if (levels.Count > 0 && Mathf.Abs(y - levels[0]) < 0.001f && y > b.max.y - 0.2f) continue; // dessus du meuble
                float yy = y - F;
                // une rangée de livres + un objet déco par tablette
                for (int i = 0; i < 6; i++)
                    Put(books[(i + k) % books.Length], parent, b.center.x, b.min.z + 0.2f + i * 0.075f, 90f, h: 0.24f + 0.02f * ((i + k) % 3), y: yy);
                Put(deco[k % deco.Length], parent, b.center.x, b.max.z - 0.35f, 0f, h: 0.22f, y: yy);
                k++;
            }
        }

        // ---------- Panneaux ----------
        static void BuildSigns()
        {
            var z = Zone("Signaletique");
            var ink = new Color(0.25f, 0.2f, 0.15f);
            Sign(z, "SALON", new Vector3(X1i - 0.02f, F + 2.7f, 0.8f), 90f, 1.6f, ink);
            Sign(z, "CHAMBRE", new Vector3(X1i - 0.02f, F + 2.7f, -6.4f), 90f, 1.9f, ink);
            Sign(z, "CUISINE", new Vector3(-3.1f, F + 2.55f, Z1i - 0.02f), 0f, 1.8f, ink);
            Sign(z, "BUREAU & RANGEMENT", new Vector3(X0i + 0.02f, F + 2.7f, -6.9f), -90f, 2.6f, ink);
            Sign(z, "<b>DecoAR</b>\n<size=45%>MAISON & DÉCORATION</size>", new Vector3(0.9f, F + 2.75f, Z0i + 0.03f), 180f, 2.4f, ink, 0.75f);
            // plaque suspendue lisible des deux côtés
            var wood = Mat("BoisClair", new Color(0.82f, 0.66f, 0.47f), 0f, 0.35f);
            var brass = Mat("Laiton", new Color(0.83f, 0.67f, 0.33f), 1f, 0.7f);
            // au bord nord de la salle à manger (la suspension de la table reste dégagée)
            const float sz = -2.2f, sy = 2.85f;
            Prim(PrimitiveType.Cube, "Plaque", z, new Vector3(3.4f, F + sy, sz), new Vector3(1.9f, 0.42f, 0.03f), wood);
            Prim(PrimitiveType.Cylinder, "Suspente_G", z, new Vector3(2.75f, F + 3.18f, sz), new Vector3(0.01f, 0.12f, 0.01f), brass);
            Prim(PrimitiveType.Cylinder, "Suspente_D", z, new Vector3(4.05f, F + 3.18f, sz), new Vector3(0.01f, 0.12f, 0.01f), brass);
            Sign(z, "SALLE À MANGER", new Vector3(3.4f, F + sy, sz - 0.018f), 0f, 1.7f, new Color(0.98f, 0.95f, 0.89f));
            Sign(z, "SALLE À MANGER", new Vector3(3.4f, F + sy, sz + 0.018f), 180f, 1.7f, new Color(0.98f, 0.95f, 0.89f));
        }

        /// Texte 3D. yaw : direction vers laquelle « regarde » le texte (0 = vers le sud / -Z, lisible depuis -Z…).
        static TextMeshPro Sign(Transform parent, string text, Vector3 pos, float yaw, float width, Color color, float height = 0.35f)
        {
            var g = new GameObject("Panneau");
            g.transform.SetParent(parent, false);
            g.transform.position = pos;
            // Le texte TMP se lit en regardant vers +Z de l'objet : on le tourne pour faire face à l'intérieur
            g.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var t = g.AddComponent<TextMeshPro>();
            t.rectTransform.sizeDelta = new Vector2(width, height);
            t.text = text; t.color = color; t.alignment = TextAlignmentOptions.Center;
            t.enableAutoSizing = true; t.fontSizeMin = 0.5f; t.fontSizeMax = 6f; t.characterSpacing = 12;
            t.fontStyle = FontStyles.Bold;
            return t;
        }

        /// Étiquette de prix sur pied (tige laiton + carte crème).
        static void PriceTag(Transform parent, string name, string price, Vector3 basePos, float yaw)
        {
            var brass = Mat("Laiton", new Color(0.83f, 0.67f, 0.33f), 1f, 0.7f);
            var cream = Mat("Creme", new Color(0.97f, 0.94f, 0.87f), 0f, 0.3f);
            var g = Group("Prix_" + name, parent, Vector3.zero).transform;
            g.position = basePos; g.rotation = Quaternion.Euler(0, yaw, 0);
            Prim(PrimitiveType.Cylinder, "Socle", g, new Vector3(0, 0.01f, 0), new Vector3(0.22f, 0.01f, 0.22f), brass);
            Prim(PrimitiveType.Cylinder, "Tige", g, new Vector3(0, 0.45f, 0), new Vector3(0.015f, 0.45f, 0.015f), brass);
            Prim(PrimitiveType.Cube, "Carte", g, new Vector3(0, 0.98f, 0), new Vector3(0.46f, 0.3f, 0.015f), cream);
            for (int side = 0; side < 2; side++)
            {
                var tg = new GameObject("Texte");
                tg.transform.SetParent(g, false);
                tg.transform.localPosition = new Vector3(0, 0.98f, side == 0 ? -0.009f : 0.009f);
                tg.transform.localRotation = Quaternion.Euler(0, side == 0 ? 0 : 180, 0);
                var t = tg.AddComponent<TextMeshPro>();
                t.rectTransform.sizeDelta = new Vector2(0.42f, 0.26f);
                t.text = $"<size=70%>{name}</size>\n<b><color=#9C6B2E>{price}</color></b>";
                t.color = new Color(0.22f, 0.18f, 0.14f); t.alignment = TextAlignmentOptions.Center;
                t.enableAutoSizing = true; t.fontSizeMin = 0.2f; t.fontSizeMax = 1.2f;
            }
        }
    }
}
