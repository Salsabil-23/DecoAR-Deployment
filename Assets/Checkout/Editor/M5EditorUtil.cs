using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DecoAR.M5
{
    public static class M5EditorUtil
    {
        public const string Root = "Assets/Checkout";
        public static readonly string[] VendorKeys = { "vendeur", "vendor", "seller", "npc", "pnj", "cashier", "character", "mixamo", "x bot", "xbot", "y bot", "ybot" };
        public static readonly string[] CounterKeys = { "counter", "comptoir", "caisse", "desk", "cash", "register", "checkout" };
        public static readonly string[] BasketKeys = { "basket", "panier" };

        public static bool Has(string s, string[] keys) { s = s.ToLowerInvariant(); return keys.Any(k => s.Contains(k)); }

        public static GameObject FindByKeywords(string label, string[] keys, System.Func<GameObject, bool> filter = null)
        {
            GameObject best = null; int bestDepth = int.MaxValue;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith("M5_") || !Has(t.name, keys)) continue;
                    if (filter != null && !filter(t.gameObject)) continue;
                    int d = 0; for (var p = t.parent; p; p = p.parent) d++;
                    if (d < bestDepth) { best = t.gameObject; bestDepth = d; }
                }
            if (!best && Selection.activeGameObject) { best = Selection.activeGameObject; Debug.Log($"[M5] {label} : aucun nom reconnu → utilisation de la sélection '{best.name}'."); }
            else if (best) Debug.Log($"[M5] {label} trouvé : '{best.name}'.");
            else Debug.LogWarning($"[M5] ⚠️ {label} introuvable (mots-clés : {string.Join(", ", keys)}). Sélectionnez-le puis relancez le menu.");
            return best;
        }

        public static GameObject FindVendor(bool log = true) => FindByKeywords("Vendeur", VendorKeys,
            g => g.GetComponentInChildren<SkinnedMeshRenderer>(true) || g.GetComponentInChildren<Animator>(true));
        public static GameObject FindCounter() => FindByKeywords("Comptoir", CounterKeys, g => g.GetComponentInChildren<Renderer>(true));
        public static GameObject FindBasket() => FindByKeywords("Panier", BasketKeys, g => g.GetComponentInChildren<Renderer>(true));

        public static Bounds GetBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.5f);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        /// Direction "client" : opposée au vendeur par rapport au comptoir.
        public static Vector3 FrontOf(Bounds counter, GameObject counterGo, GameObject vendor)
        {
            if (vendor)
            {
                var back = vendor.transform.position - counter.center; back.y = 0;
                if (back.sqrMagnitude > 0.0025f) return -back.normalized;
            }
            var f = counterGo ? counterGo.transform.forward : Vector3.forward; f.y = 0;
            return f.sqrMagnitude > 0.001f ? f.normalized : Vector3.forward;
        }

        public static void LocalExtents(Bounds b, Vector3 front, out float halfW, out float halfD)
        {
            var right = Vector3.Cross(Vector3.up, front); halfW = 0; halfD = 0;
            for (int i = 0; i < 8; i++)
            {
                var c = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, b.center.y, (i & 2) == 0 ? b.min.z : b.max.z) - b.center;
                halfW = Mathf.Max(halfW, Mathf.Abs(Vector3.Dot(c, right)));
                halfD = Mathf.Max(halfD, Mathf.Abs(Vector3.Dot(c, front)));
            }
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        public static bool IsURP => GraphicsSettings.currentRenderPipeline != null;

        public static Material Mat(string name, Color c, float metal = 0f, float smooth = 0.3f, Color? emission = null)
        {
            EnsureFolder(Root + "/Materials");
            string path = $"{Root}/Materials/M5_{name}.mat";
            var shader = Shader.Find(IsURP ? "Universal Render Pipeline/Lit" : "Standard");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            else m.shader = shader;
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            m.SetFloat("_Metallic", metal);
            m.SetFloat(IsURP ? "_Smoothness" : "_Glossiness", smooth);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material m, bool keepCollider = false)
        {
            var g = GameObject.CreatePrimitive(type);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            if (!keepCollider) Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }

        public static GameObject Group(string name, Transform parent, Vector3 localPos)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            return g;
        }

        public static TMPro.TextMeshPro Text3D(string name, string text, Transform parent, Vector3 localPos, Vector2 size, Color color)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localRotation = Quaternion.Euler(0, 180, 0); // lisible depuis +Z (côté client)
            var t = g.AddComponent<TMPro.TextMeshPro>();
            t.rectTransform.sizeDelta = size;
            t.text = text;
            t.color = color;
            t.alignment = TMPro.TextAlignmentOptions.Center;
            t.fontStyle = TMPro.FontStyles.Bold;
            t.enableAutoSizing = true; t.fontSizeMin = 0.1f; t.fontSizeMax = 50f;
            return t;
        }

        /// Instancie le premier prefab trouvé (mots-clés) et le met à l'échelle à la hauteur voulue.
        public static GameObject TryPrefab(string[] keys, Transform parent, Vector3 localPos, float targetHeight)
        {
            foreach (var k in keys)
                foreach (var guid in AssetDatabase.FindAssets($"{k} t:Prefab", new[] { "Assets" }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("TextMesh Pro")) continue;
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (!prefab || !prefab.GetComponentInChildren<Renderer>()) continue;
                    var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    g.transform.SetParent(parent, false);
                    g.transform.localPosition = localPos;
                    var b = GetBounds(g);
                    if (b.size.y > 0.001f) g.transform.localScale *= targetHeight / b.size.y;
                    b = GetBounds(g);
                    g.transform.position += Vector3.up * (parent.TransformPoint(localPos).y - b.min.y);
                    Debug.Log($"[M5] Prefab utilisé : {path}");
                    return g;
                }
            return null;
        }

        public static void SetStaticRecursive(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (!r.GetComponent<TMPro.TMP_Text>())
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic);
        }

        public static Light AddLight(string name, Transform parent, Vector3 localPos, LightType type, float range, float intensity, Color c)
        {
            var g = Group(name, parent, localPos);
            var l = g.AddComponent<Light>();
            l.type = type; l.range = range; l.intensity = intensity; l.color = c; l.shadows = LightShadows.None;
            return l;
        }
    }
}
