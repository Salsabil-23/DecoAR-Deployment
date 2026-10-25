using UnityEngine;
using UnityEngine.Rendering;

namespace DecoAR.M5
{
    /// Rendu propre à la caisse : active le pipeline M5 (lumières d'appoint par pixel, HDR pour la lueur de la télé)
    /// le temps de la scène, puis remet celui du projet (les modules AR gardent leurs réglages).
    [DefaultExecutionOrder(-1000)]
    public class M5Ambiance : MonoBehaviour
    {
        public RenderPipelineAsset pipeline;
        RenderPipelineAsset previous;
        bool swapped;

        void Awake()
        {
            if (!pipeline) return;
            previous = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = pipeline;
            swapped = true;
        }

        void OnDestroy()
        {
            if (swapped) QualitySettings.renderPipeline = previous;
        }
    }
}
