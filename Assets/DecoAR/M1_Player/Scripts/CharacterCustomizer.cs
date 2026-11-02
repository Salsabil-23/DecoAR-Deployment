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

        // Couleurs des ronds de l'onglet Avatar (un rond numerote par personnage)
        static readonly Color[] AvatarSwatchColors =
        {
            new Color(0.78f, 0.40f, 0.29f), new Color(0.54f, 0.60f, 0.48f), new Color(0.22f, 0.29f, 0.45f),
            new Color(0.85f, 0.65f, 0.28f), new Color(0.45f, 0.32f, 0.24f), new Color(0.82f, 0.56f, 0.60f),
        };

        AvatarLook look;
        LookSlot slot;
        bool onAvatarTab;
        Button avatarTab;
        GameObject preview;
        bool onePieceOutfit;
        readonly List<(Color color, int avatar, GameObject selection)> swatches = new List<(Color, int, GameObject)>();
        readonly Dictionary<LookSlot, Color> scanned = new Dictionary<LookSlot, Color>();

        void Start()
        {
            look = GameManager.Instance.Look;
            if (look.avatar < 0 || look.avatar >= profile.AvatarCount) look.avatar = 0;

            for (int i = 0; i < tabButtons.Length; i++)
            {
                var s = (LookSlot)i;
                tabButtons[i].onClick.AddListener(() => SelectSlot(s));
            }
            // Plusieurs personnages : onglet Avatar en premier, pour choisir avant d'entrer dans la boutique
            if (profile.AvatarCount > 1)
            {
                avatarTab = Instantiate(tabButtons[0], tabButtons[0].transform.parent);
                avatarTab.name = "OngletAvatar";
                avatarTab.onClick.RemoveAllListeners();
                avatarTab.onClick.AddListener(ShowAvatars);
                avatarTab.transform.SetSiblingIndex(tabButtons[0].transform.GetSiblingIndex());
                var avatarLabel = avatarTab.GetComponentInChildren<TMP_Text>();
                if (avatarLabel != null) avatarLabel.text = "Avatar";
            }

            LookSlot first = SpawnPreview();
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
            if (avatarTab != null) ShowAvatars();
            else SelectSlot(first);

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

        // (Re)cree le personnage choisi et n'affiche que les onglets qu'il porte ; renvoie le premier onglet couleur
        LookSlot SpawnPreview()
        {
            foreach (Transform child in previewRoot) Destroy(child.gameObject);
            var anim = CharacterAppearance.Spawn(profile, look, previewRoot);
            preview = anim != null ? anim.gameObject : null;
            if (preview == null) Debug.LogWarning("[M1] Aucun personnage dans le profil");

            LookSlot first = LookSlot.Peau;
            bool found = false;
            for (int i = 0; i < tabButtons.Length; i++)
            {
                var s = (LookSlot)i;
                // Un avatar MetaPerson n'a qu'une tenue d'une piece ; les cheveux viennent du scan
                bool available = preview == null || s == LookSlot.Cheveux || CharacterAppearance.HasSlot(preview, s);
                tabButtons[i].gameObject.SetActive(available);
                if (available && !found) { first = s; found = true; }
            }
            // Haut seul sans pantalon ni chaussures : c'est la tenue complete
            onePieceOutfit = preview != null && !CharacterAppearance.HasSlot(preview, LookSlot.Bas)
                && !CharacterAppearance.HasSlot(preview, LookSlot.Chaussures);
            var label = tabButtons[(int)LookSlot.Haut].GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = onePieceOutfit ? "Tenue" : "T-shirt";
            return first;
        }

        // Gros plan sur le visage pour la peau et les cheveux, plan large pour les vetements et l'avatar
        Transform CurrentView()
        {
            bool face = !onAvatarTab && (slot == LookSlot.Peau || slot == LookSlot.Cheveux);
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
            onAvatarTab = false;
            bool outfit = onePieceOutfit && s == LookSlot.Haut;
            sectionTitle.text = outfit ? "Tenue" : Titles[(int)s];
            sectionHint.text = outfit ? "Le premier rond garde la tenue d'origine." : Hints[(int)s];
            HighlightTabs();
            BuildSwatches();
        }

        void ShowAvatars()
        {
            onAvatarTab = true;
            sectionTitle.text = $"Avatar {look.avatar + 1} / {profile.AvatarCount}";
            sectionHint.text = "Choisis ton personnage avant d'entrer dans la boutique.";
            HighlightTabs();
            BuildSwatches();
        }

        void HighlightTabs()
        {
            for (int i = 0; i < tabButtons.Length; i++)
                Highlight(tabButtons[i], !onAvatarTab && i == (int)slot);
            if (avatarTab != null) Highlight(avatarTab, onAvatarTab);
        }

        void Highlight(Button tab, bool active)
        {
            tab.image.color = active ? tabActive : tabIdle;
            var label = tab.GetComponentInChildren<TMP_Text>();
            if (label != null) label.color = active ? tabActiveText : tabIdleText;
        }

        void BuildSwatches()
        {
            foreach (var sw in swatches) Destroy(sw.selection.transform.parent.gameObject);
            swatches.Clear();

            if (onAvatarTab)
            {
                for (int i = 0; i < profile.AvatarCount; i++)
                {
                    var b = AddSwatch(AvatarSwatchColors[i % AvatarSwatchColors.Length], i);
                    // numero du personnage dans le rond
                    var number = Instantiate(sectionHint, b.transform);
                    foreach (var fitter in number.GetComponents<ContentSizeFitter>()) Destroy(fitter);
                    var element = number.GetComponent<LayoutElement>();
                    if (element == null) element = number.gameObject.AddComponent<LayoutElement>();
                    element.ignoreLayout = true;
                    number.text = (i + 1).ToString();
                    number.fontSize = 30;
                    number.fontStyle = FontStyles.Bold;
                    number.color = Color.white;
                    number.alignment = TextAlignmentOptions.Center;
                    number.raycastTarget = false;
                    var rt = number.rectTransform;
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = rt.offsetMax = Vector2.zero;
                    int captured = i;
                    b.onClick.AddListener(() => PickAvatar(captured));
                }
                RefreshSelection();
                return;
            }

            var colors = new List<Color>(profile.Palette(slot));
            if (scanned.TryGetValue(slot, out var fromScan)) colors.Add(fromScan);

            foreach (var c in colors)
            {
                var b = AddSwatch(c, -1);
                Color captured = c;
                b.onClick.AddListener(() => Pick(captured));
            }
            RefreshSelection();
        }

        Button AddSwatch(Color c, int avatar)
        {
            var b = Instantiate(swatchTemplate, swatchGrid);
            b.gameObject.SetActive(true);
            b.image.color = c;
            swatches.Add((c, avatar, b.transform.Find("Selection").gameObject));
            return b;
        }

        void PickAvatar(int index)
        {
            if (look.avatar == index) return;
            look.avatar = index;
            SpawnPreview();
            ShowAvatars();
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
            foreach (var sw in swatches)
                sw.selection.SetActive(onAvatarTab ? sw.avatar == look.avatar : Same(sw.color, current));
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
            // Couleurs et scan d'origine ; le personnage choisi reste
            look = new AvatarLook { avatar = look.avatar };
            FaceStore.Delete();
            FaceMaskStore.Delete();
            HeadStore.Delete();
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
