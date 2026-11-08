using System;
using UnityEngine;
using UnityEngine.UI;

namespace DecoAR.Player
{
    [Serializable]
    public struct RectLayout
    {
        public Vector2 anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta;

        public static RectLayout Of(RectTransform rt) => new RectLayout
        {
            anchorMin = rt.anchorMin,
            anchorMax = rt.anchorMax,
            pivot = rt.pivot,
            anchoredPosition = rt.anchoredPosition,
            sizeDelta = rt.sizeDelta,
        };

        public void ApplyTo(RectTransform rt)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
        }
    }

    [Serializable]
    public class ResponsiveRect
    {
        public RectTransform target;
        public RectLayout landscape;
        public RectLayout portrait;
    }

    // L'ecran suit la rotation du telephone ; l'UI change de disposition en portrait / paysage
    public class ResponsiveLayout : MonoBehaviour
    {
        public CanvasScaler scaler;
        public ResponsiveRect[] rects = new ResponsiveRect[0];
        public GameObject[] landscapeOnly = new GameObject[0];
        public GameObject[] portraitOnly = new GameObject[0];

        [Header("Camera 3D (optionnel) : zone d'ecran en portrait")]
        public Camera worldCamera;
        public Rect portraitViewport = new Rect(0f, 0.56f, 1f, 0.44f);

        public static bool IsPortrait => Screen.height > Screen.width;

        bool applied;
        bool appliedPortrait;

        void OnEnable()
        {
            Screen.autorotateToPortrait = true;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Apply();
        }

        void Update()
        {
            if (!applied || appliedPortrait != IsPortrait) Apply();
        }

        void Apply()
        {
            bool p = IsPortrait;
            applied = true;
            appliedPortrait = p;

            if (scaler != null) scaler.referenceResolution = p ? new Vector2(1080, 1920) : new Vector2(1920, 1080);
            foreach (var r in rects)
                if (r.target != null) (p ? r.portrait : r.landscape).ApplyTo(r.target);
            foreach (var go in landscapeOnly) if (go != null) go.SetActive(!p);
            foreach (var go in portraitOnly) if (go != null) go.SetActive(p);
            if (worldCamera != null) worldCamera.rect = p ? portraitViewport : new Rect(0f, 0f, 1f, 1f);
        }
    }
}
