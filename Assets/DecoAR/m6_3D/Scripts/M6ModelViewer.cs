
using System;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Materials;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class M6ModelViewer : MonoBehaviour
{
    public Transform modelRoot;
    public Camera viewerCamera;
    public float targetSize = 1.5f;
    public float distance = 3f;
    public float rotationSpeed = 0.25f;
    public float zoomSpeed = 0.005f;
    public float minDistance = 1.5f;
    public float maxDistance = 8f;

    private GameObject currentModel;
    private GltfImport currentImporter;

    private void Awake()
    {
        if (viewerCamera == null)
            viewerCamera = Camera.main;

        if (modelRoot == null)
            modelRoot = new GameObject("ModelRoot").transform;

        PositionCamera();
    }

    public async Task<bool> LoadModel(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("[M6] GLB introuvable : " + filePath);
            return false;
        }

        var pipeline =
            GraphicsSettings.currentRenderPipeline
            as UniversalRenderPipelineAsset;

        if (pipeline == null)
        {
            Debug.LogError(
                "[M6] Active URP dans Graphics et Quality."
            );
            return false;
        }

        var generator = new UniversalRPMaterialGenerator(pipeline);
        var importer = new GltfImport(materialGenerator: generator);

        var container = new GameObject("GeneratedGLB");
        container.transform.SetParent(modelRoot, false);

        try
        {
            string uri =
                new Uri(Path.GetFullPath(filePath)).AbsoluteUri;

            bool loaded = await importer.Load(uri);

            if (!loaded)
                throw new Exception("Import GLB impossible");

            bool instantiated =
                await importer.InstantiateMainSceneAsync(
                    container.transform
                );

            if (!instantiated)
                throw new Exception("Instanciation GLB impossible");

            Renderer[] renderers =
                container.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
                throw new Exception("Aucun Renderer trouvé");

            Bounds bounds = renderers[0].bounds;

            foreach (Renderer renderer in renderers)
                bounds.Encapsulate(renderer.bounds);

            float size = Mathf.Max(
                bounds.size.x,
                bounds.size.y,
                bounds.size.z
            );

            if (size > 0.0001f)
                container.transform.localScale *= targetSize / size;

            renderers =
                container.GetComponentsInChildren<Renderer>(true);

            bounds = renderers[0].bounds;

            foreach (Renderer renderer in renderers)
                bounds.Encapsulate(renderer.bounds);

            container.transform.position +=
                modelRoot.position - bounds.center;

            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                        Debug.Log(
                            "[M6] Shader : " + material.shader.name
                        );
                }
            }

            if (currentModel != null)
                Destroy(currentModel);

            if (currentImporter != null)
                currentImporter.Dispose();

            currentModel = container;
            currentImporter = importer;

            distance = 3f;
            PositionCamera();

            Debug.Log("[M6] Modèle 3D affiché !");
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

    private void PositionCamera()
    {
        if (viewerCamera == null || modelRoot == null)
            return;

        viewerCamera.transform.position =
            modelRoot.position + Vector3.back * distance;

        viewerCamera.transform.LookAt(modelRoot.position);
        viewerCamera.nearClipPlane = 0.01f;
    }

    private void Update()
    {
        if (currentModel == null)
            return;

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Moved)
            {
                currentModel.transform.Rotate(
                    Vector3.up,
                    -touch.deltaPosition.x * rotationSpeed,
                    Space.World
                );
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch a = Input.GetTouch(0);
            Touch b = Input.GetTouch(1);

            float previous = Vector2.Distance(
                a.position - a.deltaPosition,
                b.position - b.deltaPosition
            );

            float current =
                Vector2.Distance(a.position, b.position);

            distance = Mathf.Clamp(
                distance - (current - previous) * zoomSpeed,
                minDistance,
                maxDistance
            );
        }

#if UNITY_EDITOR
        if (Input.GetMouseButton(0))
        {
            currentModel.transform.Rotate(
                Vector3.up,
                -Input.GetAxis("Mouse X") * 4f,
                Space.World
            );
        }

        distance = Mathf.Clamp(
            distance - Input.mouseScrollDelta.y * 0.25f,
            minDistance,
            maxDistance
        );
#endif

        PositionCamera();
    }

    private void OnDestroy()
    {
        if (currentImporter != null)
            currentImporter.Dispose();
    }
}
