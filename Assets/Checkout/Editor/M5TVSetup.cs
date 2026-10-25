using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static DecoAR.M5.M5EditorUtil;

namespace DecoAR.M5
{
    /// Télévision : matériau URP avec ses textures (couleur, métal/brillance, relief) + écran animé « Voir le catalogue ».
    public static class M5TVSetup
    {
        const string Dir = Root + "/Models/tv";
        const string FbxPath = Dir + "/tv.FBX";
        static readonly string[] TvKeys = { "tv", "tele", "télé", "television", "télévision", "ecran", "écran" };

        [MenuItem("DecoAR/M5/6. Télévision (texture + écran catalogue)")]
        public static void Run()
        {
            var mat = BuildMaterial();
            var glow = GlowMaterial();

            var tv = FindInstanceOf(FbxPath);
            if (!tv) tv = FindByKeywords("Télévision", TvKeys, g => g.GetComponentInChildren<MeshFilter>(true) && !g.name.StartsWith("M5_"));
            if (!tv) return;

            foreach (var r in tv.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.GetComponentInParent<M5TVScreen>()) continue;
                Undo.RecordObject(r, "M5 TV");
                r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
            }

            var mf = tv.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh && !f.GetComponentInParent<M5TVScreen>())
                .OrderByDescending(f => f.sharedMesh.bounds.size.sqrMagnitude).FirstOrDefault();
            if (!mf) { Debug.LogWarning("[M5] ⚠️ Mesh de la télé introuvable."); return; }
            ScreenRect(mf, out var center, out var outward, out var up, out var size);

            foreach (var old in tv.GetComponentsInChildren<M5TVScreen>(true)) Undo.DestroyObjectImmediate(old.gameObject);
            var go = new GameObject("M5_EcranTV");
            Undo.RegisterCreatedObjectUndo(go, "M5 Ecran TV");
            go.transform.SetPositionAndRotation(center, Quaternion.LookRotation(-outward, up)); // Z vers l'intérieur de la télé
            go.transform.SetParent(tv.transform, true);
            var screen = go.AddComponent<M5TVScreen>();
            screen.screenSize = size;
            screen.glowMaterial = glow;

            EditorSceneManager.MarkSceneDirty(tv.scene);
            Selection.activeGameObject = go;
            Debug.Log($"[M5] ✅ Télévision '{tv.name}' : texture appliquée, écran {size.x:F2} × {size.y:F2} m. Approchez-vous en Play pour l'allumer.");
        }

        static Material BuildMaterial()
        {
            Texture2D albedo = null, metal = null, normal = null;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Dir }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var ti = (TextureImporter)AssetImporter.GetAtPath(p);
                string n = p.ToLowerInvariant();
                bool isNormal = n.Contains("normal"), isMetal = n.Contains("metal");
                bool dirty = false;
                if (isNormal && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
                if (isMetal && ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
                if (ti.maxTextureSize > 2048) { ti.maxTextureSize = 2048; dirty = true; }
                if (dirty) ti.SaveAndReimport();
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                if (isNormal) normal = tex; else if (isMetal) metal = tex; else if (n.Contains("albedo") || n.Contains("diffuse") || n.Contains("color")) albedo = tex;
            }

            EnsureFolder(Root + "/Materials");
            string path = Root + "/Materials/M5_TV.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (!m) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); }
            else m.shader = lit;
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BaseMap", albedo);
            m.SetTexture("_MetallicGlossMap", metal);
            m.SetFloat("_Metallic", 1f);
            m.SetFloat("_Smoothness", 0.9f);
            m.SetFloat("_SmoothnessTextureChannel", 0f); // brillance dans l'alpha de la carte métal
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", 1f);
            m.SetFloat("_Surface", 0f);
            UnityEditor.BaseShaderGUI.SetMaterialKeywords(m, UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.SetMaterialKeywords);
            EditorUtility.SetDirty(m);

            // Le FBX utilise ce matériau partout où la télé est posée
            if (AssetImporter.GetAtPath(FbxPath) is ModelImporter imp)
            {
                imp.importCameras = false; imp.importLights = false;
                foreach (var src in AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<Material>())
                    imp.AddRemap(new AssetImporter.SourceAssetIdentifier(src), m);
                imp.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            if (!albedo) Debug.LogWarning("[M5] ⚠️ Texture couleur de la télé introuvable dans " + Dir);
            return m;
        }

        static Material GlowMaterial()
        {
            string path = Root + "/Materials/M5_TVLueur.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (!m) { m = new Material(unlit); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", new Color(0.01f, 0.012f, 0.015f));
            EditorUtility.SetDirty(m);
            return m;
        }

        /// Trouve l'écran dans le mesh : face la plus fine = profondeur, face avant = la plus détaillée, écran = rectangle en retrait.
        static void ScreenRect(MeshFilter mf, out Vector3 centerW, out Vector3 outwardW, out Vector3 upW, out Vector2 sizeW)
        {
            var mesh = mf.sharedMesh; var b = mesh.bounds; var verts = mesh.vertices; var t = mf.transform;
            Vector3 Axis(int i) { var v = Vector3.zero; v[i] = 1f; return v; }

            int thin = 0; for (int i = 1; i < 3; i++) if (b.size[i] < b.size[thin]) thin = i;
            int a1 = (thin + 1) % 3, a2 = (thin + 2) % 3;
            float up1 = Mathf.Abs(Vector3.Dot(t.TransformDirection(Axis(a1)), Vector3.up));
            float up2 = Mathf.Abs(Vector3.Dot(t.TransformDirection(Axis(a2)), Vector3.up));
            int tall = up1 >= up2 ? a1 : a2, wide = tall == a1 ? a2 : a1;

            float dMin = b.min[thin], dMax = b.max[thin], range = Mathf.Max(dMax - dMin, 1e-5f), band = range * 0.25f;
            int nMax = verts.Count(v => v[thin] > dMax - band), nMin = verts.Count(v => v[thin] < dMin + band);
            int sign = nMax >= nMin ? 1 : -1;
            if (Mathf.Abs(nMax - nMin) <= Mathf.Max(nMax, nMin) * 0.2f && Camera.main)
            {
                // Indécis : la face tournée vers le joueur
                var toCam = Camera.main.transform.position - t.TransformPoint(b.center);
                sign = Vector3.Dot(t.TransformDirection(Axis(thin)), toCam) >= 0 ? 1 : -1;
            }
            float frontD = sign > 0 ? dMax : dMin;

            var front = verts.Where(v => Mathf.Abs(v[thin] - frontD) < band).ToList();
            float deepest = front.Max(v => Mathf.Abs(v[thin] - frontD));
            var level = front.Where(v => Mathf.Abs(Mathf.Abs(v[thin] - frontD) - deepest) < range * 0.01f + 1e-6f).ToList();
            var lb = new Bounds(level[0], Vector3.zero); foreach (var v in level) lb.Encapsulate(v);

            Vector3 c; Vector2 sz;
            if (level.Count >= 4 && lb.size[wide] > b.size[wide] * 0.6f && lb.size[tall] > b.size[tall] * 0.4f)
            { c = lb.center; sz = new Vector2(lb.size[wide], lb.size[tall]); }
            else
            { c = b.center; c[thin] = frontD; sz = new Vector2(b.size[wide] * 0.94f, b.size[tall] * 0.9f); }
            c[thin] += sign * range * 0.03f; // juste devant la dalle

            centerW = t.TransformPoint(c);
            outwardW = t.TransformDirection(Axis(thin) * sign).normalized;
            upW = t.TransformDirection(Axis(tall)).normalized;
            if (Vector3.Dot(upW, Vector3.up) < 0) upW = -upW;
            upW = Vector3.ProjectOnPlane(upW, outwardW).normalized;
            sizeW = new Vector2(t.TransformVector(Axis(wide) * sz.x).magnitude, t.TransformVector(Axis(tall) * sz.y).magnitude);
        }
    }
}
