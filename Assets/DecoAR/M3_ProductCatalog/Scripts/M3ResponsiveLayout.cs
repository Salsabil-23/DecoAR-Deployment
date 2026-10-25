using System;
using UnityEngine;
using UnityEngine.UI;

namespace DecoAR.M3
{
    // Adapte le rayon M3 a l'orientation du telephone :
    // paysage = disposition de la scene (etagere a gauche, fiche a droite),
    // portrait = barre sur deux lignes, etagere au centre, fiche produit en bas.
    public class M3ResponsiveLayout : MonoBehaviour
    {
        [Serializable]
        public class Slot
        {
            public RectTransform target;
            public Vector2 anchorMin, anchorMax, position, size;
            [NonSerialized] public Vector2 lAnchorMin, lAnchorMax, lPosition, lSize;

            public void Capture()
            {
                lAnchorMin = target.anchorMin; lAnchorMax = target.anchorMax;
                lPosition = target.anchoredPosition; lSize = target.sizeDelta;
            }

            public void Apply(bool portrait)
            {
                target.anchorMin = portrait ? anchorMin : lAnchorMin;
                target.anchorMax = portrait ? anchorMax : lAnchorMax;
                target.anchoredPosition = portrait ? position : lPosition;
                target.sizeDelta = portrait ? size : lSize;
            }
        }

        [SerializeField] CanvasScaler scaler;
        [SerializeField] Slot[] slots;

        [Header("Camera en portrait")]
        [SerializeField] Camera viewCamera;
        [SerializeField] Vector3 portraitCameraPosition = new Vector3(0f, 0.95f, -3.4f);
        [SerializeField] Vector3 portraitLookAt = new Vector3(0f, 0.58f, 0.75f);

        [Header("Point d'inspection en portrait")]
        [SerializeField] Transform inspectPoint;
        [SerializeField] Vector3 portraitInspectPosition = new Vector3(0f, 0.08f, 0.85f);
        [SerializeField] M3Inspector inspector;
        [SerializeField] float portraitInspectSize = 0.25f;

        Vector2 landscapeReference;
        float landscapeMatch;
        Vector3 lCamPos, lInspectPos;
        Quaternion lCamRot;
        float lInspectSize;
        bool? portrait;

        void Awake()
        {
            foreach (var s in slots) if (s.target != null) s.Capture();
            if (scaler != null) { landscapeReference = scaler.referenceResolution; landscapeMatch = scaler.matchWidthOrHeight; }
            if (viewCamera != null) { lCamPos = viewCamera.transform.position; lCamRot = viewCamera.transform.rotation; }
            if (inspectPoint != null) lInspectPos = inspectPoint.localPosition;
            if (inspector != null) lInspectSize = inspector.InspectSize;
            Refresh();
        }

        void Update() => Refresh();

        void Refresh()
        {
            bool now = Screen.height > Screen.width;
            if (portrait == now) return;
            portrait = now;

            if (scaler != null)
            {
                // Portrait : on cale sur la largeur (1080) pour que rien ne depasse a droite, quelle que soit la hauteur de l'ecran
                scaler.referenceResolution = now ? new Vector2(landscapeReference.y, landscapeReference.x) : landscapeReference;
                scaler.matchWidthOrHeight = now ? 0f : landscapeMatch;
            }
            foreach (var s in slots) if (s.target != null) s.Apply(now);

            if (viewCamera != null)
            {
                if (now)
                {
                    viewCamera.transform.position = portraitCameraPosition;
                    viewCamera.transform.LookAt(portraitLookAt);
                }
                else viewCamera.transform.SetPositionAndRotation(lCamPos, lCamRot);
            }
            if (inspectPoint != null) inspectPoint.localPosition = now ? portraitInspectPosition : lInspectPos;
            if (inspector != null) inspector.InspectSize = now ? portraitInspectSize : lInspectSize;

            Debug.Log($"[M3] Disposition {(now ? "portrait" : "paysage")}");
        }
    }
}
