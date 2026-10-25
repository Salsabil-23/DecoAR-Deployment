using System.Collections.Generic;
using System.IO;
using System.Linq;
using DecoAR.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DecoAR.M3.EditorTools
{
    // Menu DecoAR > M3 : genere les modeles des produits, le catalogue (3 zones x 5 produits x 2 variantes),
    // les prefabs de boutons et la scene du rayon M3, sans rien construire a la main.
    public static partial class M3SceneBuilder
    {
        const string Root = "Assets/DecoAR/M3_ProductCatalog";
        const string DataFolder = Root + "/Data";
        const string MatFolder = Root + "/Materials";
        const string PrefabFolder = Root + "/Prefabs";
        const string ProductFolder = PrefabFolder + "/Products";
        const string SceneFolder = Root + "/Scenes";

        const string CatalogPath = DataFolder + "/M3_ProductCatalog.asset";
        const string ProductButtonPath = PrefabFolder + "/M3_ProductButton_Prefab.prefab";
        const string VariantButtonPath = PrefabFolder + "/M3_VariantButton_Prefab.prefab";
        const string ScenePath = SceneFolder + "/M3.unity";

        // Charte (alignee sur la caisse M5)
        static readonly Color Dark = new Color(0.17f, 0.14f, 0.11f, 0.92f);
        static readonly Color Cream = new Color(0.98f, 0.95f, 0.89f);
        static readonly Color Brass = new Color(0.80f, 0.63f, 0.30f);
        static readonly Color Sage = new Color(0.45f, 0.58f, 0.43f);
        static readonly Color Ink = new Color(0.20f, 0.17f, 0.14f);

        // ---------------------------------------------------------------- menus

        [MenuItem("DecoAR/M3/Tout generer")]
        public static void BuildAll()
        {
            var catalog = BuildCatalog();
            BuildScene(catalog);
            Debug.Log("[M3] Generation terminee");
        }

        [MenuItem("DecoAR/M3/Generer produits et catalogue")]
        public static M3ProductCatalog BuildCatalog()
        {
            EnsureFolders();
            var catalog = AssetDatabase.LoadAssetAtPath<M3ProductCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<M3ProductCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.products = new List<ProductOption>
            {
                // --- Tableaux (mur)
                Product("TAB-01", "Tableau abstrait", M3Categories.Tableaux, 80, new Vector3(50, 70, 4), ProductPlacement.Mur,
                    "Toile abstraite aux formes organiques, cadre en chêne clair.", "toile peinture art abstrait",
                    V("Terracotta", 0.78f, 0.45f, 0.33f), V("Bleu nuit", 0.16f, 0.22f, 0.38f)),
                Product("TAB-02", "Paysage du Sahara", M3Categories.Tableaux, 150, new Vector3(90, 60, 4), ProductPlacement.Mur,
                    "Photographie des dunes du Sud tunisien, tirage sur toile.", "dune desert photo sahara",
                    V("Sable doré", 0.86f, 0.70f, 0.45f), V("Crépuscule", 0.62f, 0.36f, 0.42f)),
                Product("TAB-03", "Portrait minimaliste", M3Categories.Tableaux, 65, new Vector3(40, 50, 3), ProductPlacement.Mur,
                    "Dessin au trait d'un visage, encadré noir.", "visage dessin ligne portrait",
                    V("Crème", 0.95f, 0.92f, 0.84f), V("Gris perle", 0.78f, 0.78f, 0.76f)),
                Product("TAB-04", "Affiche botanique", M3Categories.Tableaux, 35, new Vector3(30, 40, 2), ProductPlacement.Mur,
                    "Planche de feuilles de palmier, papier mat.", "plante feuille palmier poster affiche",
                    V("Sauge", 0.55f, 0.65f, 0.50f), V("Vert forêt", 0.20f, 0.36f, 0.25f)),
                Product("TAB-05", "Miroir rond", M3Categories.Tableaux, 120, new Vector3(60, 60, 3), ProductPlacement.Mur,
                    "Miroir rond au cadre métallique fin.", "miroir rond glace",
                    V("Laiton", 0.80f, 0.63f, 0.30f), V("Noir mat", 0.10f, 0.10f, 0.10f)),

                // --- Luminaires
                Product("LUM-01", "Lampe Arc", M3Categories.Luminaires, 289, new Vector3(40, 180, 40), ProductPlacement.Sol,
                    "Lampe sur pied en métal brossé, abat-jour orientable.", "lampadaire lampe arc pied",
                    V("Noir mat", 0.10f, 0.10f, 0.10f), V("Laiton", 0.80f, 0.63f, 0.30f)),
                Product("LUM-02", "Lampe en laiton", M3Categories.Luminaires, 120, new Vector3(30, 55, 30), ProductPlacement.Surface,
                    "Lampe de table au pied en laiton, abat-jour en lin.", "lampe table chevet laiton",
                    V("Lin naturel", 0.90f, 0.85f, 0.74f), V("Vert olive", 0.45f, 0.48f, 0.30f)),
                Product("LUM-03", "Lampadaire tripode", M3Categories.Luminaires, 210, new Vector3(50, 150, 50), ProductPlacement.Sol,
                    "Lampadaire à trois pieds en bois, abat-jour tambour.", "lampadaire trepied tripode bois",
                    V("Blanc", 0.95f, 0.94f, 0.90f), V("Terracotta", 0.78f, 0.45f, 0.33f)),
                Product("LUM-04", "Applique murale", M3Categories.Luminaires, 95, new Vector3(20, 30, 15), ProductPlacement.Mur,
                    "Applique en céramique pour éclairage d'ambiance.", "applique mur murale",
                    V("Blanc cassé", 0.93f, 0.90f, 0.84f), V("Anthracite", 0.25f, 0.25f, 0.27f)),
                Product("LUM-05", "Lampe champignon", M3Categories.Luminaires, 75, new Vector3(25, 30, 25), ProductPlacement.Surface,
                    "Petite lampe à poser en verre opalin.", "lampe champignon poser verre",
                    V("Opalin", 0.97f, 0.96f, 0.92f), V("Ambre", 0.85f, 0.55f, 0.22f)),

                // --- Objets deco (sol ou table)
                Product("DEC-01", "Vase en céramique", M3Categories.ObjetsDeco, 45, new Vector3(25, 35, 25), ProductPlacement.Surface,
                    "Vase décoratif minimaliste tourné à la main.", "vase ceramique fleur",
                    V("Terracotta", 0.78f, 0.45f, 0.33f), V("Blanc cassé", 0.93f, 0.90f, 0.84f)),
                Product("DEC-02", "Bougie parfumée", M3Categories.ObjetsDeco, 25, new Vector3(10, 12, 10), ProductPlacement.Surface,
                    "Bougie en cire végétale, 40 heures.", "bougie parfum cire",
                    V("Vanille", 0.97f, 0.93f, 0.82f), V("Figue", 0.52f, 0.33f, 0.40f)),
                Product("DEC-03", "Coussin en lin", M3Categories.ObjetsDeco, 60, new Vector3(45, 45, 15), ProductPlacement.Surface,
                    "Coussin carré en lin lavé, déhoussable.", "coussin lin canape",
                    V("Sauge", 0.62f, 0.69f, 0.58f), V("Sable", 0.85f, 0.76f, 0.62f)),
                Product("DEC-04", "Plateau en bois", M3Categories.ObjetsDeco, 55, new Vector3(40, 5, 30), ProductPlacement.Surface,
                    "Plateau de service en bois massif.", "plateau bois service",
                    V("Chêne", 0.72f, 0.55f, 0.36f), V("Noyer", 0.40f, 0.27f, 0.18f)),
                Product("DEC-05", "Statuette abstraite", M3Categories.ObjetsDeco, 70, new Vector3(15, 30, 15), ProductPlacement.Surface,
                    "Sculpture en résine aux courbes douces.", "statue sculpture statuette",
                    V("Pierre", 0.80f, 0.78f, 0.72f), V("Bronze", 0.55f, 0.38f, 0.22f)),
            };

            catalog.products.AddRange(RoomProducts());

            foreach (var p in catalog.products)
                p.prefab = BuildProductPrefab(p);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[M3] Catalogue : {catalog.products.Count} produits");
            return catalog;
        }

        [MenuItem("DecoAR/M3/Creer la scene du rayon")]
        public static void BuildSceneMenu()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<M3ProductCatalog>(CatalogPath);
            if (catalog == null || catalog.products.Count == 0) catalog = BuildCatalog();
            BuildScene(catalog);
        }

        // ---------------------------------------------------------------- produits

        static ProductVariant V(string name, float r, float g, float b) => new ProductVariant { name = name, color = new Color(r, g, b) };

        static ProductOption Product(string id, string name, string category, float price, Vector3 sizeCm, ProductPlacement placement,
            string description, string keywords, params ProductVariant[] variants)
        {
            return new ProductOption
            {
                id = id, displayName = name, category = category, price = price, sizeCm = sizeCm,
                placement = placement, description = description, keywords = keywords,
                variants = variants.ToList(),
            };
        }

        // Modeles simples en primitives, a la taille reelle (m), pivot au sol, face avant vers +Z.
        // Les pieces nommees "Variant..." prennent la couleur de la variante.
        static GameObject BuildProductPrefab(ProductOption p)
        {
            var root = new GameObject("P_" + p.id);
            Vector3 s = p.sizeCm / 100f;
            Color v0 = p.variants[0].color;
            Color wood = new Color(0.72f, 0.55f, 0.36f);
            Color black = new Color(0.1f, 0.1f, 0.1f);
            Color brass = new Color(0.80f, 0.63f, 0.30f);
            Color white = new Color(0.95f, 0.94f, 0.90f);

            switch (p.id)
            {
                case "TAB-01":
                case "TAB-02":
                case "TAB-03":
                case "TAB-04":
                    {
                        Color frame = p.id == "TAB-03" ? black : p.id == "TAB-04" ? white : wood;
                        float f = 0.025f;
                        Part(root, "Cadre haut", PrimitiveType.Cube, new Vector3(0, s.y - f / 2, s.z / 2), new Vector3(s.x, f, s.z), frame);
                        Part(root, "Cadre bas", PrimitiveType.Cube, new Vector3(0, f / 2, s.z / 2), new Vector3(s.x, f, s.z), frame);
                        Part(root, "Cadre gauche", PrimitiveType.Cube, new Vector3(-s.x / 2 + f / 2, s.y / 2, s.z / 2), new Vector3(f, s.y, s.z), frame);
                        Part(root, "Cadre droit", PrimitiveType.Cube, new Vector3(s.x / 2 - f / 2, s.y / 2, s.z / 2), new Vector3(f, s.y, s.z), frame);
                        Part(root, "VariantToile", PrimitiveType.Cube, new Vector3(0, s.y / 2, s.z * 0.4f), new Vector3(s.x - 2 * f, s.y - 2 * f, s.z * 0.5f), v0);
                        // motif central pour donner du relief a la toile
                        Part(root, "Motif", PrimitiveType.Sphere, new Vector3(-s.x * 0.12f, s.y * 0.55f, s.z * 0.68f), new Vector3(s.x * 0.35f, s.x * 0.35f, 0.01f), white);
                        break;
                    }
                case "TAB-05":
                    Part(root, "VariantCadre", PrimitiveType.Cylinder, new Vector3(0, s.y / 2, s.z / 2), new Vector3(s.x, s.z / 2, s.y), v0, new Vector3(90, 0, 0));
                    Part(root, "Miroir", PrimitiveType.Cylinder, new Vector3(0, s.y / 2, s.z * 0.75f), new Vector3(s.x * 0.9f, 0.004f, s.y * 0.9f), new Color(0.80f, 0.86f, 0.90f), new Vector3(90, 0, 0), 0.95f);
                    break;

                case "LUM-01":   // lampe arc
                    Part(root, "VariantSocle", PrimitiveType.Cylinder, new Vector3(0, 0.015f, -s.z * 0.25f), new Vector3(s.x * 0.6f, 0.015f, s.x * 0.6f), v0);
                    Part(root, "VariantMat", PrimitiveType.Cylinder, new Vector3(0, s.y * 0.42f, -s.z * 0.25f), new Vector3(0.025f, s.y * 0.42f, 0.025f), v0);
                    Part(root, "VariantBras", PrimitiveType.Cylinder, new Vector3(0, s.y * 0.88f, 0), new Vector3(0.02f, s.z * 0.3f, 0.02f), v0, new Vector3(60, 0, 0));
                    Part(root, "Abat-jour", PrimitiveType.Sphere, new Vector3(0, s.y * 0.9f, s.z * 0.3f), new Vector3(s.x * 0.6f, s.y * 0.11f, s.x * 0.6f), white);
                    break;
                case "LUM-02":   // lampe de table
                    Part(root, "Pied", PrimitiveType.Cylinder, new Vector3(0, s.y * 0.3f, 0), new Vector3(0.05f, s.y * 0.3f, 0.05f), brass);
                    Part(root, "Base", PrimitiveType.Cylinder, new Vector3(0, 0.01f, 0), new Vector3(s.x * 0.5f, 0.01f, s.x * 0.5f), brass);
                    Part(root, "VariantAbatJour", PrimitiveType.Cylinder, new Vector3(0, s.y * 0.8f, 0), new Vector3(s.x, s.y * 0.2f, s.z), v0);
                    break;
                case "LUM-03":   // tripode
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i * 120f;
                        Vector3 dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                        Part(root, "Pied " + (i + 1), PrimitiveType.Cylinder, dir * s.x * 0.22f + Vector3.up * s.y * 0.36f,
                            new Vector3(0.025f, s.y * 0.37f, 0.025f), wood, new Vector3(-12f, a, 0));   // pieds resserres en haut
                    }
                    Part(root, "VariantAbatJour", PrimitiveType.Cylinder, new Vector3(0, s.y * 0.86f, 0), new Vector3(s.x, s.y * 0.14f, s.z), v0);
                    break;
                case "LUM-04":   // applique
                    Part(root, "Platine", PrimitiveType.Cube, new Vector3(0, s.y / 2, 0.01f), new Vector3(s.x * 0.5f, s.y * 0.6f, 0.02f), black);
                    Part(root, "VariantCoupe", PrimitiveType.Sphere, new Vector3(0, s.y / 2, s.z * 0.55f), new Vector3(s.x, s.y, s.z * 0.9f), v0);
                    break;
                case "LUM-05":   // champignon
                    Part(root, "Pied", PrimitiveType.Cylinder, new Vector3(0, s.y * 0.3f, 0), new Vector3(s.x * 0.35f, s.y * 0.3f, s.x * 0.35f), white);
                    Part(root, "VariantChapeau", PrimitiveType.Sphere, new Vector3(0, s.y * 0.7f, 0), new Vector3(s.x, s.y * 0.6f, s.z), v0);
                    break;

                case "DEC-01":   // vase
                    Part(root, "VariantPanse", PrimitiveType.Sphere, new Vector3(0, s.y * 0.35f, 0), new Vector3(s.x, s.y * 0.7f, s.z), v0);
                    Part(root, "VariantCol", PrimitiveType.Cylinder, new Vector3(0, s.y * 0.8f, 0), new Vector3(s.x * 0.4f, s.y * 0.2f, s.z * 0.4f), v0);
                    break;
                case "DEC-02":   // bougie
                    Part(root, "Pot", PrimitiveType.Cylinder, new Vector3(0, s.y * 0.45f, 0), new Vector3(s.x, s.y * 0.45f, s.z), new Color(0.85f, 0.85f, 0.85f), Vector3.zero, 0.9f);
                    Part(root, "VariantCire", PrimitiveType.Cylinder, new Vector3(0, s.y * 0.45f, 0), new Vector3(s.x * 0.92f, s.y * 0.42f, s.z * 0.92f), v0);
                    Part(root, "Meche", PrimitiveType.Cylinder, new Vector3(0, s.y * 0.95f, 0), new Vector3(0.004f, s.y * 0.06f, 0.004f), black);
                    break;
                case "DEC-03":   // coussin
                    Part(root, "VariantCoussin", PrimitiveType.Sphere, new Vector3(0, s.y / 2, 0), new Vector3(s.x, s.y, s.z), v0, Vector3.zero);
                    Part(root, "VariantCoeur", PrimitiveType.Cube, new Vector3(0, s.y / 2, 0), new Vector3(s.x * 0.62f, s.y * 0.62f, s.z * 0.5f), v0);
                    break;
                case "DEC-04":   // plateau
                    Part(root, "VariantFond", PrimitiveType.Cube, new Vector3(0, 0.008f, 0), new Vector3(s.x, 0.016f, s.z), v0);
                    Part(root, "VariantBord avant", PrimitiveType.Cube, new Vector3(0, s.y / 2, s.z / 2 - 0.005f), new Vector3(s.x, s.y, 0.01f), v0);
                    Part(root, "VariantBord arriere", PrimitiveType.Cube, new Vector3(0, s.y / 2, -s.z / 2 + 0.005f), new Vector3(s.x, s.y, 0.01f), v0);
                    Part(root, "VariantBord gauche", PrimitiveType.Cube, new Vector3(-s.x / 2 + 0.005f, s.y / 2, 0), new Vector3(0.01f, s.y, s.z), v0);
                    Part(root, "VariantBord droit", PrimitiveType.Cube, new Vector3(s.x / 2 - 0.005f, s.y / 2, 0), new Vector3(0.01f, s.y, s.z), v0);
                    break;
                case "DEC-05":   // statuette
                    Part(root, "Socle", PrimitiveType.Cube, new Vector3(0, 0.02f, 0), new Vector3(s.x, 0.04f, s.z), black);
                    Part(root, "VariantCorps", PrimitiveType.Capsule, new Vector3(0, s.y * 0.45f, 0), new Vector3(s.x * 0.55f, s.y * 0.32f, s.z * 0.55f), v0, new Vector3(0, 0, 12));
                    Part(root, "VariantTete", PrimitiveType.Sphere, new Vector3(s.x * 0.12f, s.y * 0.86f, 0), Vector3.one * s.x * 0.5f, v0);
                    break;

                default:
                    if (!BuildFurniture(p.id, root, s, v0))   // meubles des pieces : M3SceneBuilder.Furniture.cs
                        Part(root, "VariantCorps", PrimitiveType.Cube, new Vector3(0, s.y / 2, 0), s, v0);
                    break;
            }

            string path = $"{ProductFolder}/P_{p.id}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject Part(GameObject root, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color,
            Vector3 euler = default, float smoothness = 0.25f)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color, smoothness);
            return go;
        }

        static Material Mat(Color color, float smoothness = 0.25f)
        {
            string key = ColorUtility.ToHtmlStringRGB(color) + "_" + Mathf.RoundToInt(smoothness * 100);
            string path = $"{MatFolder}/M3_{key}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // ---------------------------------------------------------------- scene

        static void BuildScene(M3ProductCatalog catalog)
        {
            EnsureFolders();
            var productButton = BuildProductButtonPrefab();
            var variantButton = BuildVariantButtonPrefab();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.52f, 0.48f);

            // --- decor du rayon
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(40f, -20f, 0f);

            var spot = new GameObject("Spot rayon").AddComponent<Light>();
            spot.type = LightType.Point;
            spot.range = 4f;
            spot.intensity = 1.2f;
            spot.color = new Color(1f, 0.9f, 0.75f);
            spot.transform.position = new Vector3(0.3f, 2.2f, -0.6f);

            var decor = new GameObject("Decor").transform;
            Box("Sol", decor, new Vector3(0, -0.05f, 0), new Vector3(8, 0.1f, 8), new Color(0.80f, 0.74f, 0.66f));
            Box("Mur du fond", decor, new Vector3(0, 1.5f, 1.1f), new Vector3(8, 3, 0.1f), new Color(0.93f, 0.89f, 0.83f));

            // --- etagere : 2 planches x 3 emplacements (format paysage)
            var shelfGo = new GameObject("M3_Etagere");
            shelfGo.transform.position = new Vector3(0, 0, 0.75f);
            var shelfT = shelfGo.transform;
            Color wood = new Color(0.55f, 0.40f, 0.27f);
            const float width = 1.7f, depth = 0.4f, height = 1.05f;
            Box("Montant gauche", shelfT, shelfT.position + new Vector3(-width / 2, height / 2, 0), new Vector3(0.04f, height, depth), wood);
            Box("Montant droit", shelfT, shelfT.position + new Vector3(width / 2, height / 2, 0), new Vector3(0.04f, height, depth), wood);
            Box("Fond", shelfT, shelfT.position + new Vector3(0, height / 2, depth / 2), new Vector3(width, height, 0.02f), wood * 0.9f);
            float[] plankY = { 0.1f, 0.55f };
            var slots = new List<Transform>();
            for (int row = 0; row < plankY.Length; row++)
            {
                Box("Planche " + (row + 1), shelfT, shelfT.position + new Vector3(0, plankY[row], 0), new Vector3(width, 0.03f, depth), wood);
                foreach (float x in new[] { -0.55f, 0f, 0.55f })
                {
                    var slot = new GameObject($"Emplacement {slots.Count + 1}").transform;
                    slot.SetParent(shelfT, false);
                    slot.position = shelfT.position + new Vector3(x, plankY[row] + 0.015f, -0.02f);
                    slot.rotation = Quaternion.Euler(0, 180f, 0);   // face avant (+Z) des produits vers le client
                    slots.Add(slot);
                }
            }
            // remplissage du haut vers le bas
            slots = slots.OrderByDescending(t => t.position.y).ThenBy(t => t.position.x).ToList();
            var shelf = shelfGo.AddComponent<M3Shelf>();
            SetArray(shelf, "slots", slots.Cast<Object>().ToArray());
            SetFloat(shelf, "slotSize", 0.36f);

            // --- camera : l'etagere a gauche, la fiche produit a droite de l'ecran
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.93f, 0.89f, 0.83f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0.62f, 0.95f, -2.1f);
            camGo.transform.LookAt(new Vector3(0.62f, 0.42f, 0.75f));

            var inspectPoint = new GameObject("M3_InspectPoint").transform;
            inspectPoint.SetParent(camGo.transform, false);
            inspectPoint.localPosition = new Vector3(-0.25f, 0f, 0.85f);
            inspectPoint.localRotation = Quaternion.Euler(0, 180f, 0);   // +Z vers la camera

            // --- logique
            var logic = new GameObject("M3_CatalogManager");
            var inspector = logic.AddComponent<M3Inspector>();
            Set(inspector, "viewCamera", cam);
            Set(inspector, "inspectPoint", inspectPoint);
            SetFloat(inspector, "inspectSize", 0.32f);
            var shake = logic.AddComponent<M3ShakeDetector>();
            var voiceGo = new GameObject("M3_VoiceSearch");   // nom utilise par le plugin natif (UnitySendMessage)
            var voice = voiceGo.AddComponent<M3VoiceSearch>();
            var selector = logic.AddComponent<M3CatalogSelector>();

            // --- interface
            var canvas = NewCanvas("M3_CatalogCanvas");
            var ct = canvas.transform;

            // Barre du haut
            var top = Stretch("Barre", ct, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), new Vector2(0, 120));
            top.gameObject.AddComponent<Image>().color = Dark;
            var back = MakeButton(top, "M3_BackButton", "‹ Boutique", new Vector2(0, 0.5f), new Vector2(150, 0), new Vector2(260, 90), Cream, Ink, out _);
            // 7 zones : rangee d'onglets defilante (glisser horizontalement)
            var zoneRow = Stretch("M3_ZoneTabs", top, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1280, 96));
            zoneRow.gameObject.AddComponent<RectMask2D>();
            var zoneScroll = zoneRow.gameObject.AddComponent<ScrollRect>();
            zoneScroll.horizontal = true;
            zoneScroll.vertical = false;
            zoneScroll.movementType = ScrollRect.MovementType.Clamped;
            var zoneContent = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            zoneContent.gameObject.layer = LayerMask.NameToLayer("UI");
            zoneContent.SetParent(zoneRow, false);
            zoneContent.anchorMin = new Vector2(0, 0);
            zoneContent.anchorMax = new Vector2(0, 1);
            zoneContent.pivot = new Vector2(0, 0.5f);
            zoneContent.offsetMin = zoneContent.offsetMax = Vector2.zero;
            var zoneLayout = zoneContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            zoneLayout.spacing = 12;
            zoneLayout.padding = new RectOffset(0, 0, 3, 3);
            zoneLayout.childAlignment = TextAnchor.MiddleLeft;
            zoneLayout.childControlWidth = zoneLayout.childControlHeight = true;
            zoneLayout.childForceExpandWidth = zoneLayout.childForceExpandHeight = false;
            zoneContent.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            zoneScroll.viewport = zoneRow;
            zoneScroll.content = zoneContent;
            var zoneButtons = new List<Object>();
            foreach (var zone in M3Categories.All)
            {
                var zb = MakeButton(zoneContent, "Zone " + zone, zone, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 90), Cream, Ink, out var zl);
                var le = zb.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = le.minWidth = 240;
                le.preferredHeight = le.minHeight = 90;
                zl.fontSize = 32;
                zoneButtons.Add(zb);
            }
            var cart = MakeText(top, "M3_CartLabel", "Panier (0)", new Vector2(1, 0.5f), new Vector2(-170, 0), new Vector2(300, 90), 36, Cream);
            cart.alignment = TextAlignmentOptions.Right;

            // Recherche : champ texte + OK + micro, puis ce qui a ete compris
            var searchRow = Stretch("M3_Search", ct, new Vector2(0, 1), new Vector2(0, 1), new Vector2(560, -185), new Vector2(1060, 90));
            var input = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources
            {
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            });
            input.name = "M3_SearchField";
            var irt = (RectTransform)input.transform;
            irt.SetParent(searchRow, false);
            irt.anchorMin = new Vector2(0, 0);
            irt.anchorMax = new Vector2(1, 1);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = new Vector2(-380, 0);
            var field = input.GetComponent<TMP_InputField>();
            field.pointSize = 36;
            ((TMP_Text)field.placeholder).text = "Rechercher : canapé, lit, lampe…";
            field.textComponent.color = Ink;
            var searchBtn = MakeButton(searchRow, "M3_SearchButton", "OK", new Vector2(1, 0.5f), new Vector2(-300, 0), new Vector2(140, 90), Brass, Color.white, out _);
            var micBtn = MakeButton(searchRow, "M3_MicButton", "Micro", new Vector2(1, 0.5f), new Vector2(-105, 0), new Vector2(200, 90), Sage, Color.white, out var micLabel);
            var heard = MakeText(ct, "M3_HeardText", "", new Vector2(0, 1), new Vector2(560, -265), new Vector2(1060, 60), 30, Ink);
            heard.alignment = TextAlignmentOptions.Left;

            // Liste des produits de la zone (bas de l'ecran) : ScrollRect horizontal
            var strip = Stretch("M3_ProductStrip", ct, new Vector2(0, 0), new Vector2(0, 0), new Vector2(620, 105), new Vector2(1180, 170));
            strip.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.12f);
            var scroll = strip.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Stretch("Viewport", strip, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = new GameObject("M3_ProductPanel", typeof(RectTransform)).GetComponent<RectTransform>();
            content.gameObject.layer = LayerMask.NameToLayer("UI");
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0, 0);
            content.anchorMax = new Vector2(0, 1);
            content.pivot = new Vector2(0, 0.5f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var hl = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(12, 12, 12, 12);
            hl.spacing = 12;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandWidth = hl.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            var selected = MakeText(ct, "M3_SelectedText", "Touchez un produit pour l'inspecter", new Vector2(0, 0), new Vector2(620, 220), new Vector2(1180, 56), 32, Ink);

            // Fiche produit (droite)
            var sheet = Stretch("M3_ProductSheet", ct, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-340, -60), new Vector2(640, -160));
            sheet.gameObject.AddComponent<Image>().color = new Color(Dark.r, Dark.g, Dark.b, 1f);   // opaque : la liste ne transparait pas
            var name = MakeText(sheet, "Nom", "Produit", new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(580, 80), 50, Cream);
            name.fontStyle = FontStyles.Bold;
            var price = MakeText(sheet, "Prix", "0 TND", new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(580, 60), 44, Brass);
            var dims = MakeText(sheet, "Dimensions", "", new Vector2(0.5f, 1), new Vector2(0, -200), new Vector2(580, 70), 28, Cream);
            var desc = MakeText(sheet, "Description", "", new Vector2(0.5f, 1), new Vector2(0, -300), new Vector2(580, 130), 30, Cream);
            desc.alignment = TextAlignmentOptions.TopLeft;
            MakeText(sheet, "Titre variantes", "Variantes", new Vector2(0.5f, 1), new Vector2(0, -395), new Vector2(580, 50), 30, Brass);
            var variants = Stretch("M3_Variants", sheet, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -470), new Vector2(580, 100));
            var vl = variants.gameObject.AddComponent<HorizontalLayoutGroup>();
            vl.spacing = 16;
            vl.childAlignment = TextAnchor.MiddleCenter;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = vl.childForceExpandHeight = false;
            var ar = MakeButton(sheet, "M3_ViewInAR", "Voir en AR", new Vector2(0.5f, 0), new Vector2(0, 260), new Vector2(560, 100), Brass, Color.white, out _);
            var add = MakeButton(sheet, "M3_AddToCart", "Ajouter au panier", new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(560, 100), Sage, Color.white, out _);
            var putBack = MakeButton(sheet, "M3_PutBack", "Reposer (ou secouez)", new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(560, 80), Cream, Ink, out _);

            var toast = MakeText(ct, "M3_Toast", "", new Vector2(0.5f, 1), new Vector2(-300, -340), new Vector2(900, 70), 36, Ink);
            toast.fontStyle = FontStyles.Bold;

            // Disposition portrait (reference 1080 x 1920) ; la disposition paysage est celle construite ci-dessus
            var responsive = canvas.gameObject.AddComponent<M3ResponsiveLayout>();
            Set(responsive, "scaler", canvas.GetComponent<CanvasScaler>());
            Set(responsive, "viewCamera", cam);
            Set(responsive, "inspectPoint", inspectPoint);
            Set(responsive, "inspector", inspector);
            SetSlots(responsive, new[]
            {
                PortraitSlot(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -110), new Vector2(0, 220)),
                PortraitSlot((RectTransform)back.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(135, -60), new Vector2(240, 90)),
                PortraitSlot(cart.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-140, -60), new Vector2(250, 90)),
                PortraitSlot(zoneRow, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(1040, 90)),
                PortraitSlot(searchRow, new Vector2(0, 1), new Vector2(0, 1), new Vector2(540, -280), new Vector2(1040, 90)),
                PortraitSlot(heard.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(540, -355), new Vector2(1040, 60)),
                PortraitSlot(strip, new Vector2(0, 0), new Vector2(0, 0), new Vector2(540, 105), new Vector2(1040, 170)),
                PortraitSlot(selected.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(540, 220), new Vector2(1040, 56)),
                PortraitSlot(sheet, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 440), new Vector2(-40, 860)),
                PortraitSlot(toast.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -420), new Vector2(1000, 70)),
            });

            AddEventSystem();

            // --- cablage
            // Recharge apres NewScene : la creation de scene decharge les assets non references,
            // les instances chargees avant deviennent invalides (reference vide dans la scene)
            catalog = AssetDatabase.LoadAssetAtPath<M3ProductCatalog>(CatalogPath);
            productButton = AssetDatabase.LoadAssetAtPath<GameObject>(ProductButtonPath);
            variantButton = AssetDatabase.LoadAssetAtPath<GameObject>(VariantButtonPath);
            if (catalog == null) Debug.LogError("[M3] Catalogue introuvable : " + CatalogPath);
            Set(selector, "catalog", catalog);
            Set(selector, "shelf", shelf);
            Set(selector, "inspector", inspector);
            Set(selector, "shake", shake);
            Set(selector, "voice", voice);
            Set(selector, "productRoot", content);
            Set(selector, "productButtonPrefab", productButton);
            Set(selector, "selectedLabel", selected);
            SetArray(selector, "zoneButtons", zoneButtons.ToArray());
            Set(selector, "sheetPanel", sheet.gameObject);
            Set(selector, "sheetName", name);
            Set(selector, "sheetPrice", price);
            Set(selector, "sheetDimensions", dims);
            Set(selector, "sheetDescription", desc);
            Set(selector, "variantRoot", variants);
            Set(selector, "variantButtonPrefab", variantButton);
            Set(selector, "viewInARButton", ar);
            Set(selector, "addToCartButton", add);
            Set(selector, "putBackButton", putBack);
            Set(selector, "searchField", field);
            Set(selector, "searchButton", searchBtn);
            Set(selector, "micButton", micBtn);
            Set(selector, "micLabel", micLabel);
            Set(selector, "heardText", heard);
            Set(selector, "backButton", back);
            Set(selector, "cartLabel", cart);
            Set(selector, "toastText", toast);

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            AddToBuild(ScenePath);
            AddToBuild("Assets/Checkout/Scenes/caisse.unity");
            AssetDatabase.SaveAssets();
            Debug.Log($"[M3] Scene enregistree : {ScenePath}");
        }

        // Bouton produit : LayoutElement pour que le layout de la liste lui donne une vraie taille
        // (sans lui, le HorizontalLayoutGroup ecrase les boutons a la taille du sprite : ~20 px, non cliquables)
        static GameObject BuildProductButtonPrefab()
        {
            var go = new GameObject("M3_ProductButton_Prefab", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            ((RectTransform)go.transform).sizeDelta = new Vector2(300, 146);
            var img = go.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = Cream;
            img.raycastTarget = true;
            var b = go.AddComponent<Button>();
            b.targetGraphic = img;
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = 300;
            le.preferredHeight = le.minHeight = 146;
            var label = Label((RectTransform)go.transform, "Produit\n0 TND", 30, Ink);
            label.margin = new Vector4(10, 6, 10, 6);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, ProductButtonPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject BuildVariantButtonPrefab()
        {
            var go = new GameObject("M3_VariantButton_Prefab", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var img = go.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            var b = go.AddComponent<Button>();
            b.targetGraphic = img;
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = 270;
            le.preferredHeight = le.minHeight = 96;

            var swatch = new GameObject("Swatch", typeof(RectTransform)).AddComponent<Image>();
            swatch.gameObject.layer = go.layer;
            swatch.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            swatch.raycastTarget = false;
            var srt = swatch.rectTransform;
            srt.SetParent(go.transform, false);
            srt.anchorMin = srt.anchorMax = new Vector2(0, 0.5f);
            srt.anchoredPosition = new Vector2(46, 0);
            srt.sizeDelta = new Vector2(60, 60);

            var label = Label((RectTransform)go.transform, "Variante", 28, Ink);
            label.rectTransform.offsetMin = new Vector2(84, 0);
            label.alignment = TextAlignmentOptions.Left;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, VariantButtonPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ---------------------------------------------------------------- utilitaires

        static void Box(string name, Transform parent, Vector3 pos, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color, 0.15f);
        }

        static Canvas NewCanvas(string name)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);   // meme reference que M1 (paysage)
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static void AddEventSystem()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        // RectTransform ancre entre anchorMin et anchorMax ; pos/size relatifs a ces ancres
        static RectTransform Stretch(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static TextMeshProUGUI Label(RectTransform parent, string text, float size, Color color)
        {
            var label = new GameObject("Texte", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.gameObject.layer = parent.gameObject.layer;
            var lrt = label.rectTransform;
            lrt.SetParent(parent, false);
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        static TextMeshProUGUI MakeText(Transform parent, string name, string text, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize, Color color)
        {
            var rt = Stretch(name, parent, anchor, anchor, pos, size);
            var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        static Button MakeButton(Transform parent, string name, string text, Vector2 anchor, Vector2 pos, Vector2 size,
            Color background, Color textColor, out TMP_Text label)
        {
            var rt = Stretch(name, parent, anchor, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = background;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            label = Label(rt, text, 36, textColor);
            return b;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[M3] Champ introuvable : {target.GetType().Name}.{field}"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static (RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 size) PortraitSlot(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
            => (rt, min, max, pos, size);

        static void SetSlots(M3ResponsiveLayout layout, (RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)[] slots)
        {
            var so = new SerializedObject(layout);
            var prop = so.FindProperty("slots");
            prop.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
            {
                var e = prop.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("target").objectReferenceValue = slots[i].rt;
                e.FindPropertyRelative("anchorMin").vector2Value = slots[i].min;
                e.FindPropertyRelative("anchorMax").vector2Value = slots[i].max;
                e.FindPropertyRelative("position").vector2Value = slots[i].pos;
                e.FindPropertyRelative("size").vector2Value = slots[i].size;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddToBuild(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) return;
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void EnsureFolders()
        {
            foreach (var f in new[] { DataFolder, MatFolder, PrefabFolder, ProductFolder, SceneFolder })
                EnsureFolder(f);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
