using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace DecoAR.AR
{
    public class M4LightMatcher : MonoBehaviour
    {
        [SerializeField] ARCameraManager cameraManager;
        [SerializeField] Light sun;
        [SerializeField] float intensityMultiplier = 1f;
        [SerializeField] float smoothing = 4f;

        float targetIntensity = 1f;
        float targetTemperature = 6500f;
        float nextLog;

        void OnEnable()
        {
            cameraManager.requestedLightEstimation =
                LightEstimation.AmbientIntensity | LightEstimation.AmbientColor;
            cameraManager.frameReceived += OnFrame;
            sun.useColorTemperature = true;
        }

        void OnDisable() => cameraManager.frameReceived -= OnFrame;


        void OnFrame(ARCameraFrameEventArgs args)
        {
            var le = args.lightEstimation;

            if (le.averageIntensityInLumens.HasValue)
                targetIntensity = (le.averageIntensityInLumens.Value / 1000f) * intensityMultiplier;
            else if (le.averageBrightness.HasValue)
                targetIntensity = le.averageBrightness.Value * intensityMultiplier;

            if (le.averageColorTemperature.HasValue)
                targetTemperature = le.averageColorTemperature.Value;

            if (Time.time >= nextLog)
            {
                nextLog = Time.time + 1f;
                Debug.Log($"[M4] Lumens={le.averageIntensityInLumens} | Brightness={le.averageBrightness} | Temp={le.averageColorTemperature} | Soleil={sun.intensity:F2}");
            }
        }

        void Update()
        {
            float t = Time.deltaTime * smoothing;
            sun.intensity = Mathf.Lerp(sun.intensity, Mathf.Max(targetIntensity, 0.2f), t);
            sun.colorTemperature = Mathf.Lerp(sun.colorTemperature, targetTemperature, t);
        }
    }
}