using DecoAR.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DecoAR.Player
{
    // Ecran de choix (F1.1) et de personnalisation (F1.2) de l'avatar
    public class AvatarSelectionMenu : MonoBehaviour
    {
        [SerializeField] AvatarCatalog catalog;
        [SerializeField] Transform previewRoot;
        [SerializeField] float previewSpin = 25f;

        [Header("UI")]
        [SerializeField] TMP_Text nameText;
        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;
        [SerializeField] Transform colorButtonsParent;
        [SerializeField] Button colorButtonTemplate;
        [SerializeField] Button confirmButton;

        [SerializeField] string shopSceneName = "M1_Test";

        int index;
        Color color;
        GameObject preview;

        void Start()
        {
            var gm = GameManager.Instance;
            index = gm.AvatarIndex;
            color = gm.OutfitColor;

            previousButton.onClick.AddListener(() => Select(index - 1));
            nextButton.onClick.AddListener(() => Select(index + 1));
            confirmButton.onClick.AddListener(Confirm);

            colorButtonTemplate.gameObject.SetActive(false);
            foreach (var c in catalog.outfitColors)
            {
                var b = Instantiate(colorButtonTemplate, colorButtonsParent);
                b.gameObject.SetActive(true);
                b.image.color = c;
                Color captured = c;
                b.onClick.AddListener(() => SetColor(captured));
            }

            Select(index);
        }

        void Update()
        {
            if (previewRoot != null) previewRoot.Rotate(0f, previewSpin * Time.deltaTime, 0f);
        }

        void Select(int i)
        {
            int count = catalog.avatars.Length;
            if (count == 0) return;
            index = (i % count + count) % count;

            if (preview != null) Destroy(preview);
            previewRoot.localRotation = Quaternion.identity;
            var anim = AvatarAppearance.Spawn(catalog, index, color, previewRoot);
            preview = anim != null ? anim.gameObject : null;

            var option = catalog.Get(index);
            nameText.text = option.prefab != null ? option.displayName : option.displayName + " (modele manquant)";
        }

        void SetColor(Color c)
        {
            color = c;
            if (preview != null) AvatarAppearance.ApplyOutfitColor(preview, c);
        }

        void Confirm()
        {
            var gm = GameManager.Instance;
            gm.SetAvatar(index, color);
            gm.ClearShopPose(); // nouvelle partie : on entre par la porte
            SceneManager.LoadScene(shopSceneName);
        }
    }
}
