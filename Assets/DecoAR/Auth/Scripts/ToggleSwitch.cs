using UnityEngine;
using UnityEngine.UI;

namespace DecoAR.Auth
{
    // Interrupteur style iOS par-dessus un Toggle : la pastille glisse, le fond change de couleur
    [RequireComponent(typeof(Toggle))]
    public class ToggleSwitch : MonoBehaviour
    {
        [SerializeField] Image track;
        [SerializeField] RectTransform knob;
        [SerializeField] float travel = 26f;
        [SerializeField] Color offColor = new Color32(0xD5, 0xCD, 0xC2, 0xFF);
        [SerializeField] Color onColor = new Color32(0xC8, 0x73, 0x4A, 0xFF);

        Toggle toggle;
        float t;

        void Awake()
        {
            toggle = GetComponent<Toggle>();
            t = toggle.isOn ? 1f : 0f;
            Apply();
        }

        void Update()
        {
            float target = toggle.isOn ? 1f : 0f;
            if (Mathf.Approximately(t, target)) return;
            t = Mathf.MoveTowards(t, target, Time.unscaledDeltaTime * 8f);
            Apply();
        }

        void Apply()
        {
            if (knob != null) knob.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, t), 0f);
            if (track != null) track.color = Color.Lerp(offColor, onColor, t);
        }
    }
}
