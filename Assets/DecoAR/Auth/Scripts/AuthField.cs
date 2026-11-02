using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DecoAR.Auth
{
    // Champ du formulaire : bordure qui change de couleur (repos / focus / erreur)
    // et bouton optionnel pour afficher le mot de passe
    [RequireComponent(typeof(TMP_InputField))]
    public class AuthField : MonoBehaviour
    {
        [SerializeField] Image border;
        [SerializeField] Button revealButton;
        [SerializeField] TMP_Text revealLabel;

        [SerializeField] Color normalColor = new Color32(0xE2, 0xDA, 0xCF, 0xFF);
        [SerializeField] Color focusColor = new Color32(0xC8, 0x73, 0x4A, 0xFF);
        [SerializeField] Color errorColor = new Color32(0xD6, 0x45, 0x45, 0xFF);

        TMP_InputField input;
        bool focused;
        bool hasError;

        public TMP_InputField Input => input != null ? input : input = GetComponent<TMP_InputField>();
        public string Value => Input.text.Trim();
        public string RawValue => Input.text;

        void Awake()
        {
            Input.onSelect.AddListener(_ => { focused = true; Refresh(); });
            Input.onDeselect.AddListener(_ => { focused = false; Refresh(); });
            Input.onValueChanged.AddListener(_ => SetError(false));
            if (revealButton != null) revealButton.onClick.AddListener(ToggleReveal);
            Refresh();
        }

        public void SetError(bool error)
        {
            if (hasError == error) return;
            hasError = error;
            Refresh();
        }

        public void Clear()
        {
            Input.text = "";
            SetError(false);
        }

        void ToggleReveal()
        {
            bool hidden = Input.contentType == TMP_InputField.ContentType.Password;
            Input.contentType = hidden ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
            Input.ForceLabelUpdate();
            if (revealLabel != null) revealLabel.text = hidden ? "Masquer" : "Voir";
        }

        void Refresh()
        {
            if (border != null) border.color = hasError ? errorColor : focused ? focusColor : normalColor;
        }
    }
}
