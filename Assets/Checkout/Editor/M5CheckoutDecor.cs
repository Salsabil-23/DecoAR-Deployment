using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static DecoAR.M5.M5EditorUtil;

namespace DecoAR.M5
{
    public static class M5CheckoutDecor
    {
        const string RootName = "M5_CheckoutDecor";
        static readonly Color Warm = new Color(1f, 0.82f, 0.6f);

        [MenuItem("DecoAR/M5/2. Décorer la caisse")]
        public static void Run()
        {
            var counter = FindCounter();
            if (!counter) return;
            var vendor = FindVendor();

            var old = GameObject.Find(RootName);
            if (old) Undo.DestroyObjectImmediate(old);

            var b = GetBounds(counter);
            var front = FrontOf(b, counter, vendor);
            LocalExtents(b, front, out float hw, out float hd);
            float h = b.size.y;

            var root = new GameObject(RootName).transform;
            root.position = new Vector3(b.center.x, b.min.y, b.center.z);
            root.rotation = Quaternion.LookRotation(front);

            // Palette boutique déco
            var wood = Mat("BoisClair", new Color(0.82f, 0.66f, 0.47f), 0f, 0.35f);
            var beige = Mat("Beige", new Color(0.91f, 0.85f, 0.74f), 0f, 0.1f);
            var sage = Mat("VertSauge", new Color(0.62f, 0.69f, 0.58f), 0f, 0.2f);
            var leaf = Mat("Feuillage", new Color(0.30f, 0.50f, 0.28f), 0f, 0.25f);
            var brass = Mat("Laiton", new Color(0.83f, 0.67f, 0.33f), 1f, 0.7f);
            var kraft = Mat("Kraft", new Color(0.72f, 0.56f, 0.38f), 0f, 0.15f);
            var terracotta = Mat("Terracotta", new Color(0.76f, 0.45f, 0.32f), 0f, 0.2f);
            var cream = Mat("Creme", new Color(0.97f, 0.94f, 0.87f), 0f, 0.3f);
            var bulb = Mat("Ampoule", new Color(1f, 0.92f, 0.75f), 0f, 0.5f, new Color(1f, 0.8f, 0.5f) * 2f);
            var rose = Mat("Rosepoudre", new Color(0.86f, 0.66f, 0.64f), 0f, 0.3f);

            // Tapis devant le comptoir
            Prim(PrimitiveType.Cube, "Tapis", root, new Vector3(0, 0.005f, hd + 0.8f), new Vector3(hw * 2f + 0.4f, 0.01f, 1.3f), beige);

            // Mur / étagère derrière le vendeur
            float vendorZ = vendor ? Vector3.Dot(vendor.transform.position - root.position, front) : -(hd + 0.6f);
            float shelfZ = Mathf.Min(-(hd + 1.2f), vendorZ - 0.6f);
            float wallW = Mathf.Max(hw * 2f + 1f, 2.4f);
            Prim(PrimitiveType.Cube, "PanneauMural", root, new Vector3(0, 1.4f, shelfZ - 0.16f), new Vector3(wallW, 2.8f, 0.05f), sage, true);
            BuildShelf(root, shelfZ, Mathf.Min(wallW - 0.6f, 1.8f), wood, brass, cream, terracotta, rose, kraft, bulb);

            // Panneau « CAISSE » + spot chaud
            var sign = Group("Panneau_Caisse", root, new Vector3(0, 2.45f, shelfZ - 0.1f)).transform;
            Prim(PrimitiveType.Cube, "Cadre", sign, Vector3.zero, new Vector3(1.3f, 0.38f, 0.04f), brass);
            Prim(PrimitiveType.Cube, "Fond", sign, new Vector3(0, 0, 0.022f), new Vector3(1.2f, 0.3f, 0.01f), wood);
            Text3D("Texte", "CAISSE", sign, new Vector3(0, 0, 0.03f), new Vector2(1.1f, 0.26f), new Color(0.22f, 0.16f, 0.1f));
            var spot = AddLight("Spot_Caisse", root, new Vector3(0, 2.95f, shelfZ + 0.7f), LightType.Spot, 3.5f, 2.5f, Warm);
            spot.spotAngle = 55f;
            spot.transform.rotation = Quaternion.LookRotation(sign.position - spot.transform.position);

            // Plantes de chaque côté
            BuildPlant(root, "Plante_G", new Vector3(-(hw + 0.45f), 0, hd * 0.5f), terracotta, leaf);
            BuildPlant(root, "Plante_D", new Vector3(hw + 0.45f, 0, hd * 0.5f), terracotta, leaf);

            // Présentoir sacs / colis
            var rack = Group("Presentoir_Colis", root, new Vector3(hw + 0.5f, 0, -hd - 0.35f));
            Prim(PrimitiveType.Cube, "Socle", rack.transform, new Vector3(0, 0.2f, 0), new Vector3(0.6f, 0.4f, 0.5f), wood);
            float y = 0.4f;
            Vector3[] sizes = { new Vector3(0.45f, 0.22f, 0.38f), new Vector3(0.36f, 0.18f, 0.3f), new Vector3(0.26f, 0.15f, 0.22f) };
            for (int i = 0; i < sizes.Length; i++)
            {
                var c = Prim(PrimitiveType.Cube, "Colis_" + i, rack.transform, new Vector3(0, y + sizes[i].y / 2f, 0), sizes[i], kraft);
                c.transform.localRotation = Quaternion.Euler(0, i * 12f - 8f, 0);
                Prim(PrimitiveType.Cube, "Ruban", c.transform, new Vector3(0, 0, 0), new Vector3(0.12f, 1.02f, 1.02f), cream);
                y += sizes[i].y;
            }
            var sac = Prim(PrimitiveType.Cube, "Sac", rack.transform, new Vector3(-0.42f, 0.2f, 0), new Vector3(0.12f, 0.4f, 0.3f), kraft);
            Prim(PrimitiveType.Cube, "Anse", sac.transform, new Vector3(0, 0.6f, 0), new Vector3(0.2f, 0.25f, 0.5f), brass);
            var box = rack.AddComponent<BoxCollider>(); box.center = new Vector3(0, 0.5f, 0); box.size = new Vector3(0.8f, 1f, 0.55f);

            // Suspensions au-dessus du comptoir
            for (int i = 0; i < 2; i++)
            {
                float x = (i == 0 ? -1 : 1) * Mathf.Max(hw * 0.5f, 0.3f);
                var p = Group("Suspension_" + i, root, new Vector3(x, h + 0.9f, 0)).transform;
                Prim(PrimitiveType.Cylinder, "Fil", p, new Vector3(0, 0.5f, 0), new Vector3(0.01f, 0.4f, 0.01f), brass);
                Prim(PrimitiveType.Cylinder, "Abat-jour", p, new Vector3(0, 0.1f, 0), new Vector3(0.3f, 0.06f, 0.3f), brass);
                Prim(PrimitiveType.Sphere, "Ampoule", p, Vector3.zero, Vector3.one * 0.14f, bulb);
                AddLight("Lumiere", p, new Vector3(0, -0.1f, 0), LightType.Point, 2.5f, 1.2f, Warm);
            }

            SetStaticRecursive(root.gameObject);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "M5 Décor caisse");
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            AssetDatabase.SaveAssets();
            Debug.Log($"[M5] ✅ Décor créé autour de '{counter.name}' (largeur {hw * 2f:F2} m, profondeur {hd * 2f:F2} m).");
        }

        static void BuildShelf(Transform root, float z, float width, Material wood, Material brass, Material cream,
            Material terracotta, Material rose, Material kraft, Material bulb)
        {
            var shelf = Group("Etagere", root, new Vector3(0, 0, z)).transform;
            var rnd = new System.Random(7);
            float[] heights = { 1.05f, 1.5f, 1.95f };
            foreach (var sy in heights)
            {
                Prim(PrimitiveType.Cube, "Planche", shelf, new Vector3(0, sy, 0), new Vector3(width, 0.04f, 0.26f), wood);
                for (int s = -1; s <= 1; s += 2)
                    Prim(PrimitiveType.Cube, "Equerre", shelf, new Vector3(s * width * 0.4f, sy - 0.06f, -0.08f), new Vector3(0.03f, 0.1f, 0.1f), brass);

                float x = -width / 2f + 0.15f;
                while (x < width / 2f - 0.15f)
                {
                    var pos = new Vector3(x, sy + 0.02f, 0);
                    int kind = rnd.Next(4);
                    if (kind == 0 && TryPrefab(new[] { "vase" }, shelf, pos, 0.22f) is GameObject pv) { StripColliders(pv); x += 0.25f; continue; }
                    switch (kind)
                    {
                        case 0: // vase
                            Prim(PrimitiveType.Cylinder, "Vase", shelf, pos + Vector3.up * 0.11f, new Vector3(0.1f, 0.11f, 0.1f), rnd.Next(2) == 0 ? terracotta : cream);
                            Prim(PrimitiveType.Sphere, "Col", shelf, pos + Vector3.up * 0.2f, new Vector3(0.08f, 0.05f, 0.08f), brass);
                            break;
                        case 1: // boîte cadeau
                            var g = Prim(PrimitiveType.Cube, "BoiteCadeau", shelf, pos + Vector3.up * 0.07f, new Vector3(0.14f, 0.14f, 0.14f), rnd.Next(2) == 0 ? rose : kraft);
                            Prim(PrimitiveType.Cube, "Ruban", g.transform, Vector3.zero, new Vector3(0.18f, 1.02f, 1.02f), brass);
                            break;
                        case 2: // bougies
                            for (int c = 0; c < 2; c++)
                            {
                                float ch = 0.08f + c * 0.05f;
                                Prim(PrimitiveType.Cylinder, "Bougie", shelf, pos + new Vector3(c * 0.07f, ch / 2f, 0), new Vector3(0.05f, ch / 2f, 0.05f), cream);
                                Prim(PrimitiveType.Sphere, "Flamme", shelf, pos + new Vector3(c * 0.07f, ch + 0.012f, 0), new Vector3(0.015f, 0.025f, 0.015f), bulb);
                            }
                            break;
                        default: // livres
                            for (int k = 0; k < 3; k++)
                                Prim(PrimitiveType.Cube, "Livre", shelf, pos + new Vector3(k * 0.035f, 0.09f + k * 0.005f, 0), new Vector3(0.03f, 0.18f + k * 0.01f, 0.16f), k == 1 ? rose : (k == 0 ? terracotta : cream));
                            break;
                    }
                    x += 0.22f + (float)rnd.NextDouble() * 0.08f;
                }
            }
        }

        static void BuildPlant(Transform root, string name, Vector3 pos, Material pot, Material leaf)
        {
            var plant = TryPrefab(new[] { "plant", "plante", "ficus", "monstera" }, root, pos, 1.1f);
            if (plant) { StripColliders(plant); plant.name = name; }
            else
            {
                plant = Group(name, root, pos);
                var t = plant.transform;
                Prim(PrimitiveType.Cylinder, "Pot", t, new Vector3(0, 0.2f, 0), new Vector3(0.38f, 0.2f, 0.38f), pot);
                Prim(PrimitiveType.Cylinder, "Tige", t, new Vector3(0, 0.55f, 0), new Vector3(0.03f, 0.2f, 0.03f), pot);
                Vector3[] leaves = { new Vector3(0, 0.85f, 0), new Vector3(0.14f, 0.7f, 0.05f), new Vector3(-0.13f, 0.72f, -0.06f), new Vector3(0.02f, 0.68f, 0.15f), new Vector3(0, 1.02f, 0.02f) };
                for (int i = 0; i < leaves.Length; i++)
                    Prim(PrimitiveType.Sphere, "Feuillage", t, leaves[i], new Vector3(0.42f, 0.3f, 0.42f) * (i == 4 ? 0.7f : 1f), leaf);
            }
            var box = plant.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 0.55f, 0); box.size = new Vector3(0.5f, 1.1f, 0.5f);
            var s = plant.transform.lossyScale.x;
            if (s > 0.0001f && Mathf.Abs(s - 1f) > 0.01f) { box.center /= s; box.size /= s; }
        }

        static void StripColliders(GameObject g) { foreach (var c in g.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c); }
    }
}
