using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static DecoAR.M5.M5EditorUtil;

namespace DecoAR.M5
{
    /// Ambiance boutique chaleureuse : pipeline propre à la caisse (lumières d'appoint + HDR), post-traitement, éclairage et brume légère.
    /// Rien n'est modifié dans les réglages partagés du projet.
    public static class M5AmbianceSetup
    {
        const string SettingsDir = Root + "/Settings";
        const string PipelinePath = SettingsDir + "/M5_URP.asset";
        const string ProfilePath = SettingsDir + "/M5_PostProcess.asset";

        [MenuItem("DecoAR/M5/7. Ambiance (lumières + rendu)")]
        public static void Run()
        {
            EnsureFolder(SettingsDir);
            var pipeline = MakePipeline();
            var profile = BuildProfile();

            var old = GameObject.Find("M5_Ambiance");
            if (old) Undo.DestroyObjectImmediate(old);
            var go = new GameObject("M5_Ambiance");
            Undo.RegisterCreatedObjectUndo(go, "M5 Ambiance");
            go.AddComponent<M5Ambiance>().pipeline = pipeline;
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 10; vol.sharedProfile = profile;

            // Caméra : post-traitement + anticrénelage léger (mobile)
            var cam = Camera.main;
            if (cam)
            {
                Undo.RecordObject(cam, "M5 Camera");
                cam.allowHDR = true;
                var data = cam.GetUniversalAdditionalCameraData();
                Undo.RecordObject(data, "M5 Camera");
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                EditorUtility.SetDirty(data);
            }
            else Debug.LogWarning("[M5] ⚠️ Pas de Main Camera : post-traitement non activé.");

            // Soleil chaud, ombres douces
            var sun = RenderSettings.sun;
            if (!sun) foreach (var l in Object.FindObjectsByType<Light>()) if (l.type == LightType.Directional) { sun = l; break; }
            if (sun)
            {
                Undo.RecordObject(sun, "M5 Soleil");
                sun.color = new Color(1f, 0.93f, 0.84f);
                sun.intensity = 1.15f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.75f;
            }

            // Lumière ambiante en dégradé (plafond clair, sol plus sombre) + brume légère pour la profondeur
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.86f, 0.83f, 0.79f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.56f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.33f, 0.29f, 0.25f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.84f, 0.79f, 0.72f);

            BuildFloor();

            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = go;
            AssetDatabase.SaveAssets();
            Debug.Log("[M5] ✅ Ambiance : lumières d'appoint actives, bloom, tons chauds, vignette (visible en Play).");
        }

        /// Copie du pipeline du projet avec les lumières d'appoint par pixel et le HDR (lueur de la télé).
        static RenderPipelineAsset MakePipeline()
        {
            var asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);
            if (!asset)
            {
                var src = QualitySettings.renderPipeline ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline;
                if (!src || !AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(src), PipelinePath))
                { Debug.LogWarning("[M5] ⚠️ Pipeline URP introuvable : ambiance sans lumières d'appoint."); return null; }
                asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);
            }
            var so = new SerializedObject(asset);
            void Set(string name, int v) { var p = so.FindProperty(name); if (p != null) p.intValue = v; }
            void SetB(string name, bool v) { var p = so.FindProperty(name); if (p != null) p.boolValue = v; }
            void SetF(string name, float v) { var p = so.FindProperty(name); if (p != null) p.floatValue = v; }
            SetB("m_SupportsHDR", true);
            Set("m_AdditionalLightsRenderingMode", (int)LightRenderingMode.PerPixel);
            Set("m_AdditionalLightsPerObjectLimit", 4);
            Set("m_MainLightShadowmapResolution", 2048);
            SetF("m_ShadowDistance", 15f);
            SetB("m_SoftShadowsSupported", true);
            Set("m_MSAA", 2);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// Parquet chêne au niveau du sol du joueur (la scène n'avait qu'un sol invisible).
        static void BuildFloor()
        {
            var sol = GameObject.Find("M5_SolCollider");
            var col = sol ? sol.GetComponent<Collider>() : null;
            Vector3 c;
            if (col) c = new Vector3(col.bounds.center.x, col.bounds.max.y, col.bounds.center.z);
            else if (FindCounter() is GameObject counter) { var b = GetBounds(counter); c = new Vector3(b.center.x, b.min.y, b.center.z); }
            else c = Vector3.zero;

            var old = GameObject.Find("M5_Sol");
            if (old) Undo.DestroyObjectImmediate(old);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            floor.name = "M5_Sol";
            Object.DestroyImmediate(floor.GetComponent<Collider>()); // la collision reste sur M5_SolCollider
            floor.transform.SetPositionAndRotation(c + Vector3.up * 0.002f, Quaternion.Euler(90f, 0f, 0f));
            floor.transform.localScale = new Vector3(16f, 16f, 1f);
            floor.GetComponent<Renderer>().sharedMaterial = ParquetMaterial(8f);
            GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
            Undo.RegisterCreatedObjectUndo(floor, "M5 Sol");
        }

        static Material ParquetMaterial(float tiling)
        {
            string texPath = Root + "/Materials/M5_Parquet.png";
            if (!System.IO.File.Exists(texPath))
            {
                System.IO.File.WriteAllBytes(texPath, ParquetTexture(512).EncodeToPNG());
                AssetDatabase.ImportAsset(texPath);
                var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
                ti.wrapMode = TextureWrapMode.Repeat; ti.anisoLevel = 4; ti.SaveAndReimport();
            }
            var m = Mat("Parquet", Color.white, 0f, 0.45f);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            m.SetTextureScale("_BaseMap", Vector2.one * tiling);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// 4 rangées de lames (50 cm de large pour une tuile de 2 m), joints décalés, teintes et veinage variés.
        static Texture2D ParquetTexture(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGB24, false);
            var px = new Color32[n * n];
            var rnd = new System.Random(11);
            var oak = new Color(0.72f, 0.54f, 0.36f);
            int rows = 4, rowH = n / rows;
            for (int r = 0; r < rows; r++)
            {
                int x = -rnd.Next(n / 2);
                while (x < n)
                {
                    int len = n / 2 + rnd.Next(n / 2);
                    float tone = 0.85f + (float)rnd.NextDouble() * 0.25f, seed = (float)rnd.NextDouble() * 100f;
                    for (int px0 = Mathf.Max(0, x); px0 < Mathf.Min(n, x + len); px0++)
                        for (int y = r * rowH; y < (r + 1) * rowH; y++)
                        {
                            bool gap = y == r * rowH || px0 == x || px0 == x + len - 1;
                            float grain = Mathf.PerlinNoise(px0 * 0.012f + seed, y * 0.35f) * 0.25f + Mathf.PerlinNoise(px0 * 0.08f, y * 0.9f + seed) * 0.08f;
                            var col = oak * (tone - grain + 0.12f);
                            if (gap) col *= 0.55f;
                            col.a = 1f;
                            px[y * n + px0] = col;
                        }
                    x += len;
                }
            }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        static VolumeProfile BuildProfile()
        {
            AssetDatabase.DeleteAsset(ProfilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            var tone = Add<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.Neutral);

            var bloom = Add<Bloom>(profile);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.7f);
            bloom.scatter.Override(0.65f);
            bloom.tint.Override(new Color(1f, 0.92f, 0.82f));

            var color = Add<ColorAdjustments>(profile);
            color.postExposure.Override(0.15f);
            color.contrast.Override(10f);
            color.saturation.Override(8f);

            var wb = Add<WhiteBalance>(profile);
            wb.temperature.Override(8f);

            var vig = Add<Vignette>(profile);
            vig.intensity.Override(0.24f);
            vig.smoothness.Override(0.45f);
            vig.color.Override(new Color(0.12f, 0.08f, 0.05f));

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var c = profile.Add<T>(true);
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }
    }
}
