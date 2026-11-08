using System.Collections.Generic;
using DecoAR.M5;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DecoAR.M5.EditorTools
{
    /// Poste d'administration derrière la caisse : bureau debout devant les étagères, PC portable Acer (écran repéré
    /// automatiquement grâce à ses UV), lampe, plante, mug, et le composant M5AdminTerminal.
    public static class M5PosteAdminSetup
    {
        const string ScenePath = "Assets/Checkout/Scenes/caisse.unity";
        const string Dir = "Assets/Checkout/Boutique/Laptop/";
        const string LaptopFbx = Dir + "Laptop_Acer.fbx";
        const string LaptopMat = Dir + "M5_Laptop.mat";
        const string LogoPath = "Assets/DecoAR/Core/Icons/DecoAR_Icon.png";
        // Dalle de l'écran dans la texture du PC (zone noire en haut à droite de Color.png)
        static readonly Rect ScreenUV = new Rect(0.545f, 0.592f, 0.235f, 0.266f);

        static readonly Vector3 Spot = new Vector3(-3.85f, 0f, -1.55f); // devant les étagères « CAISSE », entre le panier et le vendeur

        [MenuItem("DecoAR/M5/Poste administration derrière la caisse")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var old = GameObject.Find("M5_PosteAdmin");
            if (old != null) Object.DestroyImmediate(old);
            PrepareLaptopAsset();

            float floor = Physics.Raycast(new Vector3(Spot.x, 3f, Spot.z), Vector3.down, out var fh, 6f) ? fh.point.y : -0.2f;
            var root = new GameObject("M5_PosteAdmin").transform;
            root.position = new Vector3(Spot.x, floor, Spot.z);

            // Bureau (pack ithappy), long côté contre le mur
            var desk = Instance(Find("Work_Table_06"), root);
            desk.name = "Bureau";
            desk.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            Scale(desk, 1.18f, maxDim: true);
            // Bureau debout : l'écran dépasse du comptoir et se voit depuis la boutique
            var db = Bounds(desk);
            var ls = desk.transform.localScale; ls.y *= 1.12f / db.size.y; desk.transform.localScale = ls;
            Ground(desk, new Vector3(-4.18f, floor, Spot.z), alignBackX: true); // juste devant les planches murales
            var deskB = Bounds(desk);
            float top = DeskTop(desk, deskB);

            // PC portable, face aux clients (+X), légèrement tourné vers le centre de la boutique
            var pc = new GameObject("PC_Acer").transform;
            pc.SetParent(root, false);
            var laptop = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LaptopFbx));
            laptop.transform.SetParent(pc, true);
            laptop.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(LaptopMat);
            var lb = Bounds(laptop);
            laptop.transform.localScale *= 0.41f / lb.size.z;  // 41 cm de large
            lb = Bounds(laptop);
            laptop.transform.position += new Vector3(deskB.center.x + 0.02f - lb.center.x, top - lb.min.y, Spot.z - lb.center.z);
            pc.RotateAround(new Vector3(deskB.center.x, top, Spot.z), Vector3.up, -12f);

            // Écran : on vise la dalle et on garde les points dont les UV sont dans la zone de l'écran
            var screen = FindScreen(laptop, out var size);
            screen.SetParent(pc, true);
            screen.name = "Ecran";
            var box = pc.gameObject.AddComponent<BoxCollider>();
            var pcb = Bounds(laptop);
            box.center = pc.InverseTransformPoint(pcb.center); box.size = pcb.size * 1.15f; box.isTrigger = true;

            // Petits objets du bureau
            Decor(root, "Night_light_3", 0.42f, new Vector3(deskB.center.x - 0.12f, top, deskB.min.z + 0.16f), 30f);
            Decor(root, "PottedPlant_Small_2", 0.3f, new Vector3(deskB.center.x - 0.1f, top, deskB.max.z - 0.14f), 0f);
            Decor(root, "Mug", 0.11f, new Vector3(deskB.center.x + 0.16f, top, Spot.z + 0.33f), -40f);

            var terminal = pc.gameObject.AddComponent<M5AdminTerminal>();
            terminal.screen = screen;
            terminal.screenSize = size;
            terminal.logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
            terminal.logoMark = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/DecoAR/Core/Icons/DecoAR_Icon_Foreground.png");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[M5] Poste admin installé : écran {size.x * 100f:0.0} × {size.y * 100f:0.0} cm");
        }

        static void PrepareLaptopAsset()
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(LaptopFbx);
            if (!imp.isReadable) { imp.isReadable = true; imp.SaveAndReimport(); }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(LaptopMat);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, LaptopMat); }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Textures/Color.png"));
            mat.SetColor("_BaseColor", new Color(0.86f, 0.86f, 0.88f));
            mat.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Textures/AO.png"));
            mat.EnableKeyword("_OCCLUSIONMAP");
            // Emit.png éclaire presque tout le modèle : pas d'émission, l'écran est une interface
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
            mat.SetFloat("_Smoothness", 0.5f);
            mat.SetFloat("_Metallic", 0.35f);
            EditorUtility.SetDirty(mat);
        }

        static Transform FindScreen(GameObject laptop, out Vector2 size)
        {
            var mc = laptop.AddComponent<MeshCollider>();
            mc.sharedMesh = laptop.GetComponent<MeshFilter>().sharedMesh;
            Physics.SyncTransforms();
            var b = Bounds(laptop);
            var pts = new List<Vector3>();
            var normal = Vector3.zero;
            const int n = 60;
            for (int i = 0; i <= n; i++)
                for (int j = 0; j <= n; j++)
                {
                    var o = new Vector3(b.max.x + 1f, Mathf.Lerp(b.min.y, b.max.y, i / (float)n), Mathf.Lerp(b.min.z, b.max.z, j / (float)n));
                    foreach (var dir in new[] { Vector3.left, (Vector3.left + Vector3.down * 0.35f).normalized })
                        if (mc.Raycast(new Ray(o, dir), out var hit, 4f) && ScreenUV.Contains(hit.textureCoord))
                        {
                            pts.Add(hit.point);
                            normal += hit.normal;
                        }
                }
            Object.DestroyImmediate(mc);
            if (pts.Count < 10) Debug.LogWarning($"[M5] Écran du PC mal repéré ({pts.Count} points)");
            normal = normal.normalized;
            var up = Vector3.ProjectOnPlane(Vector3.up, normal).normalized;
            var right = Vector3.Cross(up, normal);
            float minR = float.MaxValue, maxR = float.MinValue, minU = float.MaxValue, maxU = float.MinValue;
            var c0 = pts.Count > 0 ? pts[0] : b.center;
            float depth = 0f;
            foreach (var p in pts)
            {
                var d = p - c0;
                minR = Mathf.Min(minR, Vector3.Dot(d, right)); maxR = Mathf.Max(maxR, Vector3.Dot(d, right));
                minU = Mathf.Min(minU, Vector3.Dot(d, up)); maxU = Mathf.Max(maxU, Vector3.Dot(d, up));
                depth = Mathf.Max(depth, Vector3.Dot(d, normal));
            }
            var center = c0 + right * (minR + maxR) * 0.5f + up * (minU + maxU) * 0.5f + normal * depth;
            size = new Vector2(maxR - minR, maxU - minU) * 0.97f;
            var t = new GameObject("Ecran").transform;
            t.SetPositionAndRotation(center, Quaternion.LookRotation(normal, up));
            return t;
        }

        static void Decor(Transform root, string prefab, float size, Vector3 at, float yaw)
        {
            var path = Find(prefab);
            if (path == null) return;
            var go = Instance(path, root);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            Scale(go, size, maxDim: true);
            Ground(go, at, alignBackX: false);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        }

        /// Surface réelle du plateau (le haut de la boîte englobante est au-dessus : le PC flottait).
        static float DeskTop(GameObject desk, Bounds b)
        {
            var temp = new List<MeshCollider>();
            foreach (var mf in desk.GetComponentsInChildren<MeshFilter>())
            {
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                temp.Add(mc);
            }
            Physics.SyncTransforms();
            float top = b.max.y;
            foreach (var mc in temp)
                if (mc.Raycast(new Ray(new Vector3(b.center.x, b.max.y + 1f, b.center.z), Vector3.down), out var hit, 3f)) { top = hit.point.y; break; }
            foreach (var mc in temp) Object.DestroyImmediate(mc);
            return top;
        }

        static GameObject Instance(string path, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            go.transform.SetParent(parent, true);
            return go;
        }

        static void Scale(GameObject go, float target, bool maxDim)
        {
            var b = Bounds(go);
            float cur = maxDim ? Mathf.Max(b.size.x, b.size.y, b.size.z) : b.size.y;
            if (cur > 0.0001f) go.transform.localScale *= target / cur;
        }

        static void Ground(GameObject go, Vector3 at, bool alignBackX)
        {
            var b = Bounds(go);
            float x = alignBackX ? at.x - b.min.x : at.x - b.center.x;
            go.transform.position += new Vector3(x, at.y - b.min.y, at.z - b.center.z);
        }

        static string Find(string name)
        {
            foreach (var g in AssetDatabase.FindAssets(name + " t:Prefab", new[] { "Assets/Checkout/Boutique", "Assets/DecoAR/M3_ProductCatalog/Meubles" }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == name) return p;
            }
            Debug.LogWarning("[M5] Prefab introuvable : " + name);
            return null;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
