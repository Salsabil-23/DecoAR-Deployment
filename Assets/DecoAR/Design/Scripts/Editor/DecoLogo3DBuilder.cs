using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DecoAR.Design.Editor
{
    /// Réplique 3D du symbole « Maison AR » (docs/07-logo.md) : plaque pêche arrondie, maison terracotta
    /// (contour épais aux angles adoucis + cheminée), fauteuil arrondi sur son socle lumineux, coussin lin,
    /// coins de visée blancs, anneau en orbite. Maillages générés et enregistrés avec le prefab.
    /// Repère : 1 unité ≈ largeur de la plaque, origine au centre de la plaque, face vers -Z.
    public static class DecoLogo3DBuilder
    {
        const string Folder = "Assets/DecoAR/Design/Brand/Logo3D";
        public const string PrefabPath = Folder + "/DecoAR_Logo3D.prefab";
        static readonly List<Object> meshes = new List<Object>();

        [MenuItem("DecoAR/Design system/Construire le logo 3D")]
        public static GameObject Build()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/DecoAR/Design/Brand", "Logo3D");
            meshes.Clear();

            var terracotta = Mat("Terracotta", DecoPalette.Terracotta500, 0.5f);
            var terracottaClair = Mat("TerracottaClair", DecoPalette.Terracotta400, 0.45f);
            var plaque = Mat("Plaque", DecoPalette.Terracotta100, 0.35f, emission: (Color)DecoPalette.Terracotta50 * 0.12f);
            var socle = Mat("Socle", DecoPalette.Terracotta300, 0.4f);
            var lumiere = Mat("Lumiere", DecoPalette.Laiton100, 0.6f, emission: new Color(1f, 0.86f, 0.62f) * 2.4f);
            var bois = Mat("Bois", new Color32(0xC8, 0x76, 0x3F, 255), 0.35f);
            var coussin = Mat("Coussin", DecoPalette.Lin100, 0.2f);
            var anneau = Mat("Anneau", DecoPalette.Terracotta100, 0.65f, emission: (Color)DecoPalette.Terracotta200 * 0.45f);
            var visee = Mat("Visee", Color.white, 0.5f, emission: Color.white * 1.6f);

            var root = new GameObject("DecoAR_Logo3D");
            var logo = root.AddComponent<DecoLogo3D>();

            // Plaque arrondie (fond pêche du symbole)
            Part(root.transform, "Plaque", RoundedBox("Plaque", new Vector3(1f, 1f, 0.07f), 0.2f, 8), plaque, new Vector3(0, 0, 0.14f));

            // Maison : contour épais, toit à deux pentes, angles adoucis
            var path = new List<Vector2>
            {
                new Vector2(-0.30f, -0.27f), new Vector2(-0.30f, 0.10f), new Vector2(0f, 0.36f),
                new Vector2(0.30f, 0.10f), new Vector2(0.30f, -0.27f),
            };
            Part(root.transform, "Maison", Sweep("Maison", Fillet(path, new[] { 0f, 0.06f, 0.07f, 0.06f, 0f }, 10), 0.075f, 0.11f), terracotta, new Vector3(0, 0, 0.04f));
            Part(root.transform, "Cheminee", RoundedBox("Cheminee", new Vector3(0.07f, 0.17f, 0.09f), 0.018f, 4), terracotta, new Vector3(0.205f, 0.245f, 0.05f));

            // Socle lumineux
            float baseY = -0.30f;
            var s = Part(root.transform, "Socle", Disc("Socle", 0.25f, 0.055f, 48), socle, new Vector3(0, baseY, -0.02f));
            var glow = Part(root.transform, "Lumiere", Torus("Lumiere", 0.235f, 0.009f, 64, 10), lumiere, new Vector3(0, baseY + 0.056f, -0.02f));

            // Fauteuil (assise, dossier, accoudoirs, pieds) et coussin
            var chair = new GameObject("Fauteuil").transform;
            chair.SetParent(root.transform, false);
            chair.localPosition = new Vector3(0, baseY + 0.055f, -0.02f);
            chair.localRotation = Quaternion.Euler(0, -18f, 0);
            chair.localScale = Vector3.one * 1.18f;
            float legH = 0.07f;
            foreach (var (x, z) in new[] { (-0.10f, -0.07f), (0.10f, -0.07f), (-0.10f, 0.07f), (0.10f, 0.07f) })
            {
                var leg = Part(chair, "Pied", Cylinder("Pied", 0.012f, 0.008f, legH, 12), bois, new Vector3(x, legH * 0.5f, z));
                leg.localRotation = Quaternion.Euler(z > 0 ? -10f : 10f, 0, x > 0 ? -10f : 10f);
            }
            Part(chair, "Assise", RoundedBox("Assise", new Vector3(0.27f, 0.085f, 0.23f), 0.042f, 6), terracotta, new Vector3(0, legH + 0.04f, 0));
            Part(chair, "Coussin assise", RoundedBox("CoussinAssise", new Vector3(0.21f, 0.045f, 0.18f), 0.02f, 5), terracottaClair, new Vector3(0, legH + 0.09f, -0.01f));
            var back = Part(chair, "Dossier", RoundedBox("Dossier", new Vector3(0.30f, 0.21f, 0.08f), 0.04f, 7), terracotta, new Vector3(0, legH + 0.165f, 0.082f));
            back.localRotation = Quaternion.Euler(-10f, 0, 0);
            foreach (float x in new[] { -0.135f, 0.135f })
                Part(chair, "Accoudoir", RoundedBox("Accoudoir", new Vector3(0.07f, 0.145f, 0.22f), 0.034f, 6), terracotta, new Vector3(x, legH + 0.105f, 0.005f));
            var pillow = Part(chair, "Coussin", RoundedBox("Coussin", new Vector3(0.13f, 0.11f, 0.04f), 0.02f, 5), coussin, new Vector3(0.01f, legH + 0.165f, 0.035f));
            pillow.localRotation = Quaternion.Euler(-16f, 8f, 4f);

            // Coins de visée devant le fauteuil
            var corners = new List<Renderer>();
            float cx = 0.19f, yBottom = baseY + 0.085f, yTop = baseY + 0.43f, zc = -0.13f, arm = 0.055f, th = 0.011f;
            foreach (var (x, y, sx, sy) in new[] { (-cx, yTop, 1f, -1f), (cx, yTop, -1f, -1f), (-cx, yBottom, 1f, 1f), (cx, yBottom, -1f, 1f) })
            {
                var c = new GameObject("Visee").transform;
                c.SetParent(root.transform, false);
                c.localPosition = new Vector3(x, y, zc);
                var h = Part(c, "H", RoundedBox("ViseeH", new Vector3(arm, th, th), th * 0.45f, 2), visee, new Vector3(sx * arm * 0.5f, 0, 0));
                Part(c, "V", RoundedBox("ViseeV", new Vector3(th, arm, th), th * 0.45f, 2), visee, new Vector3(0, sy * arm * 0.5f, 0));
                corners.AddRange(c.GetComponentsInChildren<Renderer>());
            }

            // Anneau en orbite, incliné comme sur le symbole
            var ringPivot = new GameObject("Anneau").transform;
            ringPivot.SetParent(root.transform, false);
            ringPivot.localPosition = new Vector3(0, -0.04f, -0.01f);
            ringPivot.localRotation = Quaternion.Euler(-16f, 0f, -13f); // presque à plat, vu en ellipse comme sur le symbole
            var ringSpin = new GameObject("Orbite").transform;
            ringSpin.SetParent(ringPivot, false);
            Part(ringSpin, "Ruban", Ribbon("Anneau", 0.47f, 0.03f, 0.008f, 96), anneau, Vector3.zero);

            logo.ring = ringPivot; logo.ringSpin = ringSpin; logo.glow = glow.GetComponent<Renderer>();
            logo.corners = corners.ToArray(); logo.chair = chair;

            // Maillages enregistrés dans un seul fichier, à côté du prefab
            var meshPath = Folder + "/DecoAR_Logo3D_Maillages.asset";
            if (File.Exists(meshPath)) AssetDatabase.DeleteAsset(meshPath);
            var holder = new Mesh { name = "Logo3D" };
            AssetDatabase.CreateAsset(holder, meshPath);
            foreach (var m in meshes) AssetDatabase.AddObjectToAsset(m, meshPath);
            AssetDatabase.SaveAssets();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[Design] Logo 3D construit : " + PrefabPath);
            return prefab;
        }

        /// Boutique (M5) : enseigne 3D suspendue au-dessus de l'allée centrale, face à l'entrée.
        [MenuItem("DecoAR/Design system/Installer le logo 3D dans la boutique")]
        public static void PlaceInShop()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Checkout/Scenes/caisse.unity");
            var old = GameObject.Find("DecoAR_Enseigne3D");
            if (old != null) Object.DestroyImmediate(old);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) ?? Build();

            var root = new GameObject("DecoAR_Enseigne3D").transform;
            root.position = new Vector3(1.0f, 2.2f, -3.2f);
            var logo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            logo.transform.localPosition = Vector3.zero;
            logo.transform.localScale = Vector3.one * 1.1f;
            foreach (var r in logo.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Deux câbles en laiton jusqu'au plafond
            var brass = Mat("Cable", DecoPalette.Laiton500, 0.7f);
            float ceiling = Physics.Raycast(root.position, Vector3.up, out var hit, 3f) ? hit.point.y : 3.15f;
            float top = root.position.y + 0.55f;
            foreach (float x in new[] { -0.3f, 0.3f })
            {
                var cable = Part(root, "Cable", Cylinder("Cable", 0.004f, 0.004f, ceiling - top, 6), brass, new Vector3(x, (ceiling - top) * 0.5f + 0.55f, 0.15f));
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("[Design] Enseigne 3D DecoAR installée dans la boutique");
        }

        // ------------------------------------------------------------ pièces

        static Transform Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go.transform;
        }

        static Material Mat(string name, Color c, float smooth, Color? emission = null)
        {
            var path = $"{Folder}/M_Logo_{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", 0f);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else { m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); }
            EditorUtility.SetDirty(m);
            return m;
        }

        static Mesh Register(Mesh m, string name)
        {
            m.name = name;
            m.RecalculateBounds();
            meshes.Add(m);
            return m;
        }

        /// Boîte aux arêtes arrondies (rayon r), lisse.
        static Mesh RoundedBox(string name, Vector3 size, float r, int seg)
        {
            var h = size * 0.5f;
            r = Mathf.Min(r, h.x, h.y, h.z);
            var inner = h - Vector3.one * r;
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tris = new List<int>();
            Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            int n = seg * 2 + 2;
            foreach (var d in dirs)
            {
                var u = Mathf.Abs(d.y) > 0.5f ? Vector3.right : Vector3.up;
                var v = Vector3.Cross(d, u);
                int start = verts.Count;
                for (int i = 0; i <= n; i++)
                    for (int j = 0; j <= n; j++)
                    {
                        var p = d + (i / (float)n * 2f - 1f) * u + (j / (float)n * 2f - 1f) * v; // cube unité [-1,1]
                        p = Vector3.Scale(p, h);
                        var q = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                        var nrm = (p - q).sqrMagnitude > 1e-10f ? (p - q).normalized : d;
                        verts.Add(q + nrm * r); norms.Add(nrm);
                    }
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                    {
                        int a = start + i * (n + 1) + j, b = a + 1, c = a + n + 1, e = c + 1;
                        tris.AddRange(new[] { a, b, e, a, e, c });
                    }
            }
            var m = new Mesh();
            m.SetVertices(verts); m.SetNormals(norms); m.SetTriangles(tris, 0);
            FixWinding(m);
            return Register(m, name);
        }

        /// Contour balayé le long d'un chemin 2D (plan XY) : section rectangulaire largeur × profondeur, bords arrondis.
        static Mesh Sweep(string name, List<Vector2> path, float width, float depth)
        {
            // Section : rectangle aux coins arrondis
            var section = new List<Vector2>();
            float hw = width * 0.5f, hd = depth * 0.5f, rr = Mathf.Min(hw, hd) * 0.45f;
            int arc = 4;
            Vector2[] centers = { new Vector2(hw - rr, hd - rr), new Vector2(-hw + rr, hd - rr), new Vector2(-hw + rr, -hd + rr), new Vector2(hw - rr, -hd + rr) };
            for (int c = 0; c < 4; c++)
                for (int k = 0; k <= arc; k++)
                {
                    float a = (c * 90f + k * 90f / arc) * Mathf.Deg2Rad;
                    section.Add(centers[c] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr);
                }
            int sc = section.Count;
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tris = new List<int>();
            for (int i = 0; i < path.Count; i++)
            {
                var t = (i == 0 ? path[1] - path[0] : i == path.Count - 1 ? path[i] - path[i - 1] : path[i + 1] - path[i - 1]).normalized;
                var side = new Vector3(-t.y, t.x, 0f); // perpendiculaire dans le plan
                for (int k = 0; k < sc; k++)
                {
                    var o = section[k];
                    verts.Add(new Vector3(path[i].x, path[i].y, 0f) + side * o.x + Vector3.forward * o.y);
                    var nk = (section[(k + 1) % sc] - section[(k - 1 + sc) % sc]);
                    var n2 = new Vector2(nk.y, -nk.x).normalized;
                    norms.Add((side * n2.x + Vector3.forward * n2.y).normalized);
                }
            }
            for (int i = 0; i < path.Count - 1; i++)
                for (int k = 0; k < sc; k++)
                {
                    int a = i * sc + k, b = i * sc + (k + 1) % sc, c = a + sc, d = b + sc;
                    tris.AddRange(new[] { a, c, b, b, c, d });
                }
            // Bouts fermés
            foreach (int i in new[] { 0, path.Count - 1 })
            {
                var t = (i == 0 ? path[0] - path[1] : path[i] - path[i - 1]).normalized;
                int center = verts.Count;
                verts.Add(new Vector3(path[i].x, path[i].y, 0)); norms.Add(new Vector3(t.x, t.y, 0));
                int ring = verts.Count;
                for (int k = 0; k < sc; k++) { verts.Add(verts[i * sc + k]); norms.Add(new Vector3(t.x, t.y, 0)); }
                for (int k = 0; k < sc; k++) tris.AddRange(new[] { center, ring + k, ring + (k + 1) % sc });
            }
            var m = new Mesh();
            m.SetVertices(verts); m.SetNormals(norms); m.SetTriangles(tris, 0);
            FixWinding(m);
            return Register(m, name);
        }

        /// Arrondit chaque angle d'une ligne brisée par un arc de rayon donné (0 = angle vif ou extrémité).
        static List<Vector2> Fillet(List<Vector2> pts, float[] radius, int steps)
        {
            var o = new List<Vector2> { pts[0] };
            for (int i = 1; i < pts.Count - 1; i++)
            {
                var a = (pts[i - 1] - pts[i]).normalized; var b = (pts[i + 1] - pts[i]).normalized;
                float r = radius[i];
                float half = Vector2.Angle(a, b) * 0.5f * Mathf.Deg2Rad;
                float d = r / Mathf.Tan(half);                       // recul sur chaque segment
                var p0 = pts[i] + a * d; var p1 = pts[i] + b * d;
                var bis = (a + b).normalized;
                var center = pts[i] + bis * (r / Mathf.Sin(half));
                float a0 = Mathf.Atan2(p0.y - center.y, p0.x - center.x), a1 = Mathf.Atan2(p1.y - center.y, p1.x - center.x);
                float delta = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                for (int k = 0; k <= steps; k++)
                {
                    float t = a0 + delta * k / steps;
                    o.Add(center + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * r);
                }
            }
            o.Add(pts[pts.Count - 1]);
            // points intermédiaires sur les longs segments droits (tangentes plus stables)
            var dense = new List<Vector2> { o[0] };
            for (int i = 1; i < o.Count; i++)
            {
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(o[i - 1], o[i]) / 0.05f));
                for (int k = 1; k <= n; k++) dense.Add(Vector2.Lerp(o[i - 1], o[i], k / (float)n));
            }
            return dense;
        }

        /// Arrondit les angles d'une ligne brisée (extrémités conservées).
        static List<Vector2> Chaikin(List<Vector2> pts, int iterations)
        {
            for (int it = 0; it < iterations; it++)
            {
                var o = new List<Vector2> { pts[0] };
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    o.Add(Vector2.Lerp(pts[i], pts[i + 1], 0.25f));
                    o.Add(Vector2.Lerp(pts[i], pts[i + 1], 0.75f));
                }
                o.Add(pts[pts.Count - 1]);
                pts = o;
            }
            return pts;
        }

        static Mesh Disc(string name, float radius, float height, int seg)
        {
            var m = Cylinder(name, radius, radius * 0.96f, height, seg);
            return m;
        }

        /// Cylindre (rayon bas / haut), centré, axe Y.
        static Mesh Cylinder(string name, float rBottom, float rTop, float height, int seg)
        {
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tris = new List<int>();
            float hh = height * 0.5f;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var n = new Vector3(d.x, (rBottom - rTop) / height, d.z).normalized;
                verts.Add(d * rBottom + Vector3.down * hh); norms.Add(n);
                verts.Add(d * rTop + Vector3.up * hh); norms.Add(n);
            }
            for (int i = 0; i < seg; i++) { int a = i * 2; tris.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 }); }
            foreach (var (y, r, up) in new[] { (hh, rTop, true), (-hh, rBottom, false) })
            {
                int c = verts.Count;
                verts.Add(new Vector3(0, y, 0)); norms.Add(up ? Vector3.up : Vector3.down);
                for (int i = 0; i <= seg; i++)
                {
                    float a = i / (float)seg * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)); norms.Add(up ? Vector3.up : Vector3.down);
                }
                for (int i = 0; i < seg; i++) tris.AddRange(up ? new[] { c, c + i + 2, c + i + 1 } : new[] { c, c + i + 1, c + i + 2 });
            }
            var m = new Mesh();
            m.SetVertices(verts); m.SetNormals(norms); m.SetTriangles(tris, 0);
            FixWinding(m);
            return Register(m, name);
        }

        static Mesh Torus(string name, float R, float r, int seg, int side)
        {
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tris = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                var c = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                for (int j = 0; j <= side; j++)
                {
                    float b = j / (float)side * Mathf.PI * 2f;
                    var n = c * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                    verts.Add(c * R + n * r); norms.Add(n);
                }
            }
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < side; j++)
                {
                    int a = i * (side + 1) + j, b = a + side + 1;
                    tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
            var m = new Mesh();
            m.SetVertices(verts); m.SetNormals(norms); m.SetTriangles(tris, 0);
            FixWinding(m);
            return Register(m, name);
        }

        /// Ruban plat (anneau en orbite) : largeur variable, plus fin d'un côté, comme une traînée.
        static Mesh Ribbon(string name, float R, float width, float thick, int seg)
        {
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tris = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                var c = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                float w = width * (0.35f + 0.65f * (0.5f + 0.5f * Mathf.Cos(a))); // s'affine à l'arrière
                // 4 sommets : dessus intérieur / extérieur, dessous intérieur / extérieur
                verts.Add(c * (R - w * 0.5f) + Vector3.up * thick); norms.Add(Vector3.up);
                verts.Add(c * (R + w * 0.5f) + Vector3.up * thick); norms.Add(Vector3.up);
                verts.Add(c * (R - w * 0.5f) - Vector3.up * thick); norms.Add(Vector3.down);
                verts.Add(c * (R + w * 0.5f) - Vector3.up * thick); norms.Add(Vector3.down);
            }
            for (int i = 0; i < seg; i++)
            {
                int a = i * 4, b = a + 4;
                tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });             // dessus
                tris.AddRange(new[] { a + 2, a + 3, b + 2, a + 3, b + 3, b + 2 }); // dessous
            }
            // Bords intérieur et extérieur
            int edge = verts.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                var c = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                for (int k = 0; k < 4; k++) { verts.Add(verts[i * 4 + k]); norms.Add(k % 2 == 0 ? -c : c); }
            }
            for (int i = 0; i < seg; i++)
            {
                int a = edge + i * 4, b = a + 4;
                tris.AddRange(new[] { a + 1, b + 1, a + 3, a + 3, b + 1, b + 3 }); // extérieur
                tris.AddRange(new[] { a, a + 2, b, a + 2, b + 2, b });             // intérieur
            }
            var m = new Mesh();
            m.SetVertices(verts); m.SetNormals(norms); m.SetTriangles(tris, 0);
            FixWinding(m);
            return Register(m, name);
        }

        /// Oriente chaque triangle selon les normales fournies (faces visibles de l'extérieur).
        static void FixWinding(Mesh m)
        {
            var v = m.vertices; var n = m.normals; var t = m.triangles;
            for (int i = 0; i < t.Length; i += 3)
            {
                var face = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                var avg = n[t[i]] + n[t[i + 1]] + n[t[i + 2]];
                if (Vector3.Dot(face, avg) < 0f) { int tmp = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = tmp; }
            }
            m.triangles = t;
        }
    }
}
