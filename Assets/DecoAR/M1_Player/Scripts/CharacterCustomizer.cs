using System.Collections.Generic;
using DecoAR.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DecoAR.Player
{
    // Ecran de personnalisation : peau, cheveux, t-shirt, pantalon, chaussures (F1.1, F1.2)
    // et scan facial a la camera.
    public class CharacterCustomizer : MonoBehaviour
    {
        [SerializeField] CharacterProfile profile;
        [SerializeField] Transform previewRoot;

        [Header("Camera de previsualisation")]
        [SerializeField] Camera previewCamera;
        [SerializeField] Transform bodyView;
        [SerializeField] Transform faceView;
        [SerializeField] Transform bodyViewPortrait;
        [SerializeField] Transform faceViewPortrait;
        [SerializeField] float cameraSmooth = 6f;

        [Header("Onglets (dans l'ordre de LookSlot)")]
        [SerializeField] Button[] tabButtons;
        [SerializeField] Color tabActive = new Color(0.78f, 0.40f, 0.29f);
        [SerializeField] Color tabIdle = new Color(1f, 1f, 1f, 0f);
        [SerializeField] Color tabActiveText = Color.white;
        [SerializeField] Color tabIdleText = new Color(0.23f, 0.18f, 0.16f);

        [Header("Couleurs")]
        [SerializeField] TMP_Text sectionTitle;
        [SerializeField] TMP_Text sectionHint;
        [SerializeField] Transform swatchGrid;
        [SerializeField] Button swatchTemplate;   // enfant "Selection" = anneau de selection

        [Header("Actions")]
        [SerializeField] Button scanButton;
        [SerializeField] Button randomButton;
        [SerializeField] Button resetButton;
        [SerializeField] Button confirmButton;
        [SerializeField] FaceScanner scanner;

        [SerializeField] string shopSceneName = "M1_Test";
        [SerializeField] string faceScanSceneName = "M1_ScanVisage";

        // Le scan 3D renvoie ici quand le telephone ne gere pas le suivi du visage : on ouvre le scan photo
        public static bool OpenPhotoScanOnStart;

        static readonly string[] Titles = { "Couleur de peau", "Cheveux", "T-shirt", "Pantalon", "Chaussures" };
        static readonly string[] Hints =
        {
            "Le premier rond garde la peau d'origine. Le scan facial peut la choisir pour toi.",
            "Le premier rond garde la couleur d'origine.",
            "Le premier rond garde le t-shirt d'origine.",
            "Le premier rond garde le pantalon d'origine.",
            "Le premier rond garde les chaussures d'origine.",
        };

        AvatarLook look;
        LookSlot slot;
        GameObject preview;
        readonly List<(Color color, GameObject selection)> swatches = new List<(Color, GameObject)>();
        readonly Dictionary<LookSlot, Color> scanned = new Dictionary<LookSlot, Color>();

        void Start()
        {
            look = GameManager.Instance.Look;

            var anim = CharacterAppearance.Spawn(profile, look, previewRoot);
            preview = anim != null ? anim.gameObject : null;
            if (preview == null) Debug.LogWarning("[M1] Aucun personnage dans le profil");

            for (int i = 0; i < tabButtons.Length; i++)
            {
                var s = (LookSlot)i;
                tabButtons[i].onClick.AddListener(() => SelectSlot(s));
            }
            scanButton.onClick.AddListener(OpenFaceScan);
            randomButton.onClick.AddListener(Randomize);
            resetButton.onClick.AddListener(ResetLook);
            confirmButton.onClick.AddListener(Confirm);
            scanner.Applied += OnScanApplied;
            CharacterProfile.Changed += OnProfileChanged;

            swatchTemplate.gameObject.SetActive(false);
            scanner.gameObject.SetActive(false);
            // Avatar deja realiste (MetaPerson...) : la carte de scan n'a pas d'objet
            scanButton.transform.parent.gameObject.SetActive(profile.useFaceScan);
            RefreshScanLabel();
            SelectSlot(LookSlot.Peau);

            if (OpenPhotoScanOnStart)
            {
                OpenPhotoScanOnStart = false;
                scanner.Open();
            }

            var v = CurrentView();
            previewCamera.transform.SetPositionAndRotation(v.position, v.rotation);
        }

        void OnDestroy()
        {
            if (scanner != null) scanner.Applied -= OnScanApplied;
            CharacterProfile.Changed -= OnProfileChanged;
        }

        // Gros plan sur le visage pour la peau et les cheveux, plan large pour les vetements
        Transform CurrentView()
        {
            bool face = slot == LookSlot.Peau || slot == LookSlot.Cheveux;
            if (ResponsiveLayout.IsPortrait && bodyViewPortrait != null) return face ? faceViewPortrait : bodyViewPortrait;
            return face ? faceView : bodyView;
        }

        void Update()
        {
            var v = CurrentView();
            float k = 1f - Mathf.Exp(-cameraSmooth * Time.deltaTime);
            var t = previewCamera.transform;
            t.SetPositionAndRotation(Vector3.Lerp(t.position, v.position, k), Quaternion.Slerp(t.rotation, v.rotation, k));
        }

        void SelectSlot(LookSlot s)
        {
            slot = s;
            sectionTitle.text = Titles[(int)s];
            sectionHint.text = Hints[(int)s];

            for (int i = 0; i < tabButtons.Length; i++)
            {
                bool active = i == (int)s;
                tabButtons[i].image.color = active ? tabActive : tabIdle;
                var label = tabButtons[i].GetComponentInChildren<TMP_Text>();
                if (label != null) label.color = active ? tabActiveText : tabIdleText;
            }

            BuildSwatches();
        }

        void BuildSwatches()
        {
            foreach (var sw in swatches) Destroy(sw.selection.transform.parent.gameObject);
            swatches.Clear();

            var colors = new List<Color>(profile.Palette(slot));
            if (scanned.TryGetValue(slot, out var fromScan)) colors.Add(fromScan);

            foreach (var c in colors)
            {
                var b = Instantiate(swatchTemplate, swatchGrid);
                b.gameObject.SetActive(true);
                b.image.color = c;
                var selection = b.transform.Find("Selection").gameObject;
                Color captured = c;
                b.onClick.AddListener(() => Pick(captured));
                swatches.Add((c, selection));
            }
            RefreshSelection();
        }

        void Pick(Color c)
        {
            look.Set(slot, c);
            if (preview != null) CharacterAppearance.Apply(preview, look, profile);
            RefreshSelection();
        }

        void RefreshSelection()
        {
            Color current = look.Get(slot);
            foreach (var sw in swatches) sw.selection.SetActive(Same(sw.color, current));
        }

        static bool Same(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.02f;

        void Randomize()
        {
            foreach (LookSlot s in System.Enum.GetValues(typeof(LookSlot)))
            {
                var p = profile.Palette(s);
                look.Set(s, p[Random.Range(0, p.Length)]);
            }
            if (preview != null) CharacterAppearance.Apply(preview, look, profile);
            RefreshSelection();
        }

        void ResetLook()
        {
            look = new AvatarLook();
            FaceStore.Delete();
            FaceMaskStore.Delete();
            if (preview != null) CharacterAppearance.Apply(preview, look, profile);
            RefreshSelection();
            RefreshScanLabel();
        }

        void OnScanApplied(Color skin, Color hair, bool hasHair, Texture2D face)
        {
            if (face != null)
            {
                FaceStore.Save(face);
                look.visagePhoto = true;
            }
            look.Set(LookSlot.Peau, skin);
            if (hasHair)
            {
                look.Set(LookSlot.Cheveux, hair);
            }
            if (preview != null) CharacterAppearance.Apply(preview, look, profile);
            SelectSlot(LookSlot.Peau);
            RefreshScanLabel();
        }

        void OnProfileChanged(CharacterProfile p)
        {
            if (p == profile && preview != null) CharacterAppearance.Apply(preview, look, profile);
        }

        // Scan 3D (ARCore / ARKit) dans sa propre scene ; le look en cours est garde
        void OpenFaceScan()
        {
            GameManager.Instance.SetLook(look);
            SceneManager.LoadScene(faceScanSceneName);
        }

        void RefreshScanLabel()
        {
            var label = scanButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = look.visagePhoto ? "Refaire" : "Scanner";
        }

        void Confirm()
        {
            var gm = GameManager.Instance;
            gm.SetLook(look);
            gm.ClearShopPose(); // nouvelle partie : on entre par la porte
            SceneManager.LoadScene(shopSceneName);
        }
    }
}
