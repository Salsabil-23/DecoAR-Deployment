using UnityEngine;

namespace DecoAR.Design
{
    /// Animation du logo 3D « Maison AR » : l'anneau tourne en orbite autour de la maison, le socle lumineux
    /// respire, les coins de visée scintillent doucement et l'ensemble se balance légèrement.
    public class DecoLogo3D : MonoBehaviour
    {
        public Transform ring;          // pivot de l'anneau (incliné)
        public Transform ringSpin;      // l'anneau lui-même, qui tourne dans son plan
        public Renderer glow;           // ruban lumineux du socle
        public Renderer[] corners;      // coins de visée
        public Transform chair;
        public float orbitSpeed = 22f;  // degrés par seconde
        public float sway = 10f;        // balancement de l'ensemble (degrés)
        public float swayPeriod = 9f;

        static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock block;
        Color glowBase, cornerBase;
        Quaternion baseRotation;
        Vector3 chairBase;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            if (glow) glowBase = glow.sharedMaterial.GetColor(Emission);
            if (corners != null && corners.Length > 0) cornerBase = corners[0].sharedMaterial.GetColor(Emission);
            baseRotation = transform.localRotation;
            if (chair) chairBase = chair.localPosition;
        }

        void Update()
        {
            float t = Time.time;
            if (ringSpin) ringSpin.localRotation = Quaternion.Euler(0f, -t * orbitSpeed, 0f);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, Mathf.Sin(t * Mathf.PI * 2f / swayPeriod) * sway, 0f);
            if (chair) chair.localPosition = chairBase + Vector3.up * (0.004f * Mathf.Sin(t * 1.6f));

            if (glow)
            {
                block.SetColor(Emission, glowBase * (0.8f + 0.35f * Mathf.Sin(t * 2.2f)));
                glow.SetPropertyBlock(block);
            }
            if (corners != null)
                for (int i = 0; i < corners.Length; i++)
                {
                    block.SetColor(Emission, cornerBase * (0.85f + 0.25f * Mathf.Sin(t * 3f + i * 1.3f)));
                    corners[i].SetPropertyBlock(block);
                }
        }
    }
}
