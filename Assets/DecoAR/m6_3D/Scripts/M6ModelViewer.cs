using System;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Materials;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// Visionneuse du modele genere (GLB) facon showroom : socle, eclairage studio, rotation automatique,
// glisser pour tourner, pincer / molette pour zoomer, double toucher pour recentrer.
// Saisie : nouveau Input System (le projet n'active pas l'ancien Input Manager).
public class M6ModelViewer : MonoBehaviour
{
    public Transform modelRoot;
    public Camera viewerCamera;
    public float targetSize = 1.5f;
    public float distance = 3.4f;
    public float rotationSpeed = 0.25f;
    public float zoomSpeed = 0.005f;
    public float minDistance = 1.6f;
    public float maxDistance = 7f;
    [Tooltip("Rotation automatique (degres/s) tant que personne ne touche le modele")]
    public float turntableSpeed = 18f;

    /// Zone de l'ecran ou le modele est rendu (rectangle normalise) ; null = plein ecran.
    public Rect? Viewport { get; set; }
    public bool HasModel => currentModel != null;
    public string CurrentPath { get; private set; }
    public bool AutoRotate { get; set; } = true;
    public event Action<string> ModelLoaded;

    GameObject currentModel, pedestal;
    GltfImport currentImporter;
    float yaw = 20f, pitch = 14f, idleUntil;
    float defaultDistance;
    float focusHeight; // hauteur du centre du modele au-dessus de modelRoot
    Vector2 lastPointer;
    bool dragging;
    float lastTap;

    void Awake()
    {
        if (viewerCamera == null) viewerCamera = Camera.main;
        if (modelRoot == null) modelRoot = new GameObject("ModelRoot").transform;
        defaultDistance = distance;
        if (viewerCamera != null)
        {
            viewerCamera.fieldOfView = 32f;
            viewerCamera.nearClipPlane = 0.01f;
            viewerCamera.clearFlags = CameraClearFlags.SolidColor;
            viewerCamera.backgroundColor = new Color(0.95f, 0.92f, 0.87f);
        }
        SetupStudio();
        PositionCamera();
    }

    void OnEnable() => EnhancedTouchSupport.Enable();

    /// Lumieres douces + socle : le modele est presente comme un produit.
    void SetupStudio()
    {
        var key = FindAnyObjectByType<Light>();
        if (key != null && key.type == LightType.Directional)
        {
            key.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            key.color = new Color(1f, 0.96f, 0.9f);
            key.intensity = 1.25f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.55f;
        }
        var fill = new GameObject("M6_LumiereDouce").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.transform.rotation = Quaternion.Euler(20f, 150f, 0f);
        fill.color = new Color(0.85f, 0.9f, 1f);
        fill.intensity = 0.45f;
        fill.shadows = LightShadows.None;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.92f, 0.9f, 0.86f);
        RenderSettings.ambientEquatorColor = new Color(0.7f, 0.65f, 0.6f);
        RenderSettings.ambientGroundColor = new Color(0.4f, 0.36f, 0.32f);
    }

    void BuildPedestal(Bounds b)
    {
        if (pedestal != null) Destroy(pedestal);
        pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestal.name = "M6_Socle";
        Destroy(pedestal.GetComponent<Collider>());
        float r = Mathf.Max(b.extents.x, b.extents.z) * 2.5f; // diametre : 1,25 x la largeur du modele
        pedestal.transform.SetParent(modelRoot, false);
        pedestal.transform.position = new Vector3(b.center.x, b.min.y - 0.03f, b.center.z);
        pedestal.transform.localScale = new Vector3(r, 0.03f, r);
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit != null)
        {
            var m = new Material(lit);
            m.SetColor("_BaseColor", new Color(0.97f, 0.95f, 0.91f));
            m.SetFloat("_Smoothness", 0.35f);
            pedestal.GetComponent<Renderer>().sharedMaterial = m;
        }
    }

    public async Task<bool> LoadModel(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("[M6] GLB introuvable : " + filePath);
            return false;
        }

        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null)
        {
            Debug.LogError("[M6] Active URP dans Graphics et Quality.");
            return false;
        }

        var importer = new GltfImport(materialGenerator: new UniversalRPMaterialGenerator(pipeline));
        var container = new GameObject("GeneratedGLB");
        container.transform.SetParent(modelRoot, false);

        try
        {
            if (!await importer.Load(new Uri(Path.GetFullPath(filePath)).AbsoluteUri))
                throw new Exception("Import GLB impossible");
            if (!await importer.InstantiateMainSceneAsync(container.transform))
                throw new Exception("Instanciation GLB impossible");

            var renderers = container.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new Exception("Aucun Renderer trouvé");

            // Taille normalisee, pose au centre, base au niveau du socle
            var bounds = Bounds(renderers);
            float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (size > 0.0001f) container.transform.localScale *= targetSize / size;
            bounds = Bounds(renderers);
            container.transform.position += new Vector3(modelRoot.position.x - bounds.center.x, modelRoot.position.y - bounds.min.y - 0.4f, modelRoot.position.z - bounds.center.z);
            foreach (var r in renderers) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }

            if (currentModel != null) Destroy(currentModel);
            currentImporter?.Dispose();
            currentModel = container;
            currentImporter = importer;
            CurrentPath = filePath;
            bounds = Bounds(renderers);
            BuildPedestal(bounds);
            Frame(bounds);
            ResetView();
            Debug.Log("[M6] Modèle 3D affiché !");
            ModelLoaded?.Invoke(filePath);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[M6] " + ex);
            Destroy(container);
            importer.Dispose();
            return false;
        }
    }

    /// Cadrage : la camera vise le centre du modele et recule juste assez pour le voir en entier.
    void Frame(Bounds b)
    {
        focusHeight = b.center.y - modelRoot.position.y;
        float radius = Mathf.Max(b.extents.magnitude, 0.1f);
        float vFov = (viewerCamera != null ? viewerCamera.fieldOfView : 32f) * 0.5f * Mathf.Deg2Rad;
        float aspect = viewerCamera != null ? Mathf.Max(viewerCamera.aspect, 0.1f) : 1f;
        float hFov = Mathf.Atan(Mathf.Tan(vFov) * aspect);
        defaultDistance = radius * 1.15f / Mathf.Sin(Mathf.Min(vFov, hFov));
        minDistance = defaultDistance * 0.45f;
        maxDistance = defaultDistance * 2.2f;
    }

    static Bounds Bounds(Renderer[] rs)
    {
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    public void ResetView()
    {
        distance = defaultDistance;
        yaw = 20f; pitch = 14f;
        idleUntil = 0f;
        PositionCamera();
    }

    Vector3 Target => modelRoot.position + Vector3.up * (HasModel ? focusHeight : targetSize * 0.5f - 0.4f);

    void PositionCamera()
    {
        if (viewerCamera == null || modelRoot == null) return;
        // Plateau tournant : c'est le modele (et son socle) qui tourne, l'eclairage reste bien place
        modelRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
        var rot = Quaternion.Euler(pitch, 0f, 0f);
        viewerCamera.transform.position = Target - rot * Vector3.forward * distance;
        viewerCamera.transform.LookAt(Target);
        viewerCamera.rect = Viewport ?? new Rect(0, 0, 1, 1);
    }

    bool InViewport(Vector2 screen)
    {
        var r = Viewport ?? new Rect(0, 0, 1, 1);
        return r.Contains(new Vector2(screen.x / Screen.width, screen.y / Screen.height));
    }

    void Update()
    {
        if (currentModel == null) { PositionCamera(); return; }
        bool touched = false;

        // Tactile : 1 doigt = tourner, 2 doigts = zoomer, double toucher = recentrer
        var touches = ETouch.activeTouches;
        if (touches.Count == 1)
        {
            var t = touches[0];
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began && InViewport(t.screenPosition))
            {
                if (Time.unscaledTime - lastTap < 0.3f) ResetView();
                lastTap = Time.unscaledTime;
                dragging = true;
            }
            if (dragging && t.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                Orbit(t.delta);
                touched = true;
            }
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended || t.phase == UnityEngine.InputSystem.TouchPhase.Canceled) dragging = false;
        }
        else if (touches.Count == 2 && InViewport(touches[0].screenPosition))
        {
            var a = touches[0]; var b = touches[1];
            float prev = Vector2.Distance(a.screenPosition - a.delta, b.screenPosition - b.delta);
            float cur = Vector2.Distance(a.screenPosition, b.screenPosition);
            distance = Mathf.Clamp(distance - (cur - prev) * zoomSpeed, minDistance, maxDistance);
            touched = true;
        }
        else dragging = false;

        // Souris (editeur / PC)
        var mouse = Mouse.current;
        if (mouse != null && touches.Count == 0)
        {
            var pos = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame && InViewport(pos))
            {
                if (Time.unscaledTime - lastTap < 0.3f) ResetView();
                lastTap = Time.unscaledTime;
                dragging = true;
            }
            if (mouse.leftButton.wasReleasedThisFrame) dragging = false;
            if (dragging && mouse.leftButton.isPressed) { Orbit(mouse.delta.ReadValue() * 1.6f); touched = true; }
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f && InViewport(pos))
            {
                distance = Mathf.Clamp(distance - Mathf.Sign(scroll) * 0.25f, minDistance, maxDistance);
                touched = true;
            }
        }

        if (touched) idleUntil = Time.unscaledTime + 3f;
        if (AutoRotate && Time.unscaledTime > idleUntil) yaw += turntableSpeed * Time.unscaledDeltaTime;
        PositionCamera();
    }

    void Orbit(Vector2 delta)
    {
        yaw -= delta.x * rotationSpeed;
        pitch = Mathf.Clamp(pitch - delta.y * rotationSpeed * 0.6f, -5f, 55f);
    }

    void OnDestroy() => currentImporter?.Dispose();
}
