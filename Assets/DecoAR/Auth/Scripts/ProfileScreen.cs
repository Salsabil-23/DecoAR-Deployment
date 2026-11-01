using System;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DecoAR.Auth
{
    // Scene Profil : photo, infos, mot de passe, visage enregistre (reconnaissance faciale), deconnexion
    public class ProfileScreen : MonoBehaviour
    {
        [Header("En-tete")]
        [SerializeField] Button backButton;
        [SerializeField] RawImage photo;
        [SerializeField] TMP_Text initials;
        [SerializeField] Button changePhotoButton;
        [SerializeField] TMP_Text emailLabel;

        [Header("Informations")]
        [SerializeField] AuthField firstName;
        [SerializeField] AuthField lastName;
        [SerializeField] AuthField phone;
        [SerializeField] Button saveButton;
        [SerializeField] TMP_Text infoStatus;

        [Header("Mot de passe")]
        [SerializeField] AuthField currentPassword;
        [SerializeField] AuthField newPassword;
        [SerializeField] AuthField confirmPassword;
        [SerializeField] Button passwordButton;
        [SerializeField] TMP_Text passwordStatus;

        [Header("Visage (reconnaissance faciale)")]
        [SerializeField] TMP_Text faceInfo;
        [SerializeField] Button faceButton;
        [SerializeField] TMP_Text faceButtonLabel;
        [SerializeField] TMP_Text faceStatus;

        [Header("Divers")]
        [SerializeField] Button logoutButton;
        [SerializeField] GameObject cameraPanel;
        [SerializeField] TMP_Text cameraTitle;
        [SerializeField] SelfieCapture photoCapture;   // photo de profil
        [SerializeField] FaceChallenge faceChallenge;  // reenregistrer le visage (defi de mouvements)

        static readonly Color32 ErrorColor = new Color32(0xD6, 0x45, 0x45, 0xFF);
        static readonly Color32 InfoColor = new Color32(0x8A, 0x81, 0x78, 0xFF);
        static readonly Color32 SuccessColor = new Color32(0x3E, 0x9B, 0x6A, 0xFF);

        string Token => AuthSession.Token;
        Texture2D photoTexture;

        void Start()
        {
            if (!AuthSession.IsLoggedIn)
            {
                SceneManager.LoadScene(AuthSettings.Get().authScene);
                return;
            }

            backButton.onClick.AddListener(ProfileNavigation.Back);
            changePhotoButton.onClick.AddListener(() => OpenCamera(forFace: false));
            faceButton.onClick.AddListener(() => OpenCamera(forFace: true));
            saveButton.onClick.AddListener(SaveInfo);
            passwordButton.onClick.AddListener(ChangePassword);
            logoutButton.onClick.AddListener(Logout);
            photoCapture.Confirmed += UploadPhoto;
            photoCapture.Cancelled += () => cameraPanel.SetActive(false);
            faceChallenge.Purpose = "enroll";
            faceChallenge.StartText = "Commencer le défi";
            faceChallenge.Completed += UpdateFace;
            faceChallenge.Cancelled += () => cameraPanel.SetActive(false);
            cameraPanel.SetActive(false);

            ShowUser(AuthSession.User);
            // Donnees fraiches depuis le serveur
            StartCoroutine(AuthApi.Me(Token, r =>
            {
                if (r.Ok) { AuthSession.UpdateUser(r.Data.user); ShowUser(r.Data.user); }
                else if (r.Status == 401) Logout();
            }));
            LoadPhoto();
        }

        void OnDestroy()
        {
            if (photoTexture != null) Destroy(photoTexture);
        }

        void ShowUser(UserDto user)
        {
            firstName.Input.text = user.firstName;
            lastName.Input.text = user.lastName;
            phone.Input.text = user.phone;
            emailLabel.text = user.email + (user.emailVerified ? "  <color=#3E9B6A>(vérifié)</color>" : "");
            initials.text = $"{Initial(user.firstName)}{Initial(user.lastName)}";
            faceInfo.text = user.faceEnrolled
                ? $"Visage enregistré le {FormatDate(user.faceEnrolledAt)} : tu peux te connecter en regardant la caméra."
                : "Aucun visage enregistré : enregistre-le pour te connecter sans mot de passe.";
            faceButtonLabel.text = user.faceEnrolled ? "Réenregistrer mon visage" : "Enregistrer mon visage";
        }

        static string Initial(string s) => string.IsNullOrEmpty(s) ? "" : s.Substring(0, 1).ToUpperInvariant();

        static string FormatDate(string iso) =>
            DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
                ? date.ToLocalTime().ToString("dd/MM/yyyy")
                : "?";

        // ------------------------------------------------------------ camera (visage ou photo)

        // Un seul des deux blocs camera est actif a la fois (chacun ouvre la camera avec lui)
        void OpenCamera(bool forFace)
        {
            cameraTitle.text = forFace ? "Enregistre ton visage" : "Nouvelle photo";
            photoCapture.gameObject.SetActive(!forFace);
            faceChallenge.gameObject.SetActive(forFace);
            cameraPanel.SetActive(true);
        }

        void UpdateFace(FaceProof proof)
        {
            faceChallenge.SetMessage("Enregistrement du visage…");
            StartCoroutine(AuthApi.UpdateFace(Token, proof, r =>
            {
                if (!r.Ok)
                {
                    faceChallenge.Fail(r.Error); // on reste sur la camera pour recommencer
                    return;
                }
                cameraPanel.SetActive(false);
                AuthSession.UpdateUser(r.Data.user);
                ShowUser(r.Data.user);
                SetStatus(faceStatus, "Visage enregistré", SuccessColor);
            }));
        }

        // ------------------------------------------------------------ photo

        void LoadPhoto()
        {
            if (!AuthSession.User.hasPhoto) { SetPhoto(null); return; }
            StartCoroutine(AuthApi.DownloadPhoto(Token, SetPhoto));
        }

        void SetPhoto(Texture2D tex)
        {
            if (photoTexture != null && photoTexture != tex) Destroy(photoTexture);
            photoTexture = tex;
            photo.texture = tex;
            photo.enabled = tex != null;
            initials.enabled = tex == null;
        }

        void UploadPhoto(byte[] jpeg)
        {
            cameraPanel.SetActive(false);
            SetStatus(infoStatus, "Envoi de la photo…", InfoColor);
            StartCoroutine(AuthApi.UploadPhoto(Token, jpeg, r =>
            {
                if (!r.Ok) { SetStatus(infoStatus, r.Error, ErrorColor); return; }
                AuthSession.UpdateUser(r.Data.user);
                var tex = new Texture2D(2, 2);
                tex.LoadImage(jpeg);
                SetPhoto(tex);
                SetStatus(infoStatus, "Photo mise à jour", SuccessColor);
            }));
        }

        // ------------------------------------------------------------ informations

        void SaveInfo()
        {
            bool ok = true;
            ok &= Check(firstName, firstName.Value.Length >= 2);
            ok &= Check(lastName, lastName.Value.Length >= 2);
            ok &= Check(phone, phone.Value.Length == 0 || System.Text.RegularExpressions.Regex.IsMatch(phone.Value, @"^\+?[0-9 ]{8,15}$"));
            if (!ok) { SetStatus(infoStatus, "Vérifie les champs en rouge", ErrorColor); return; }

            saveButton.interactable = false;
            StartCoroutine(AuthApi.UpdateProfile(Token, firstName.Value, lastName.Value, phone.Value, r =>
            {
                saveButton.interactable = true;
                if (!r.Ok)
                {
                    FieldFor(r.Field)?.SetError(true);
                    SetStatus(infoStatus, r.Error, ErrorColor);
                    return;
                }
                AuthSession.UpdateUser(r.Data.user);
                ShowUser(r.Data.user);
                SetStatus(infoStatus, "Modifications enregistrées", SuccessColor);
            }));
        }

        AuthField FieldFor(string field) => field switch
        {
            "firstName" => firstName,
            "lastName" => lastName,
            "phone" => phone,
            "currentPassword" => currentPassword,
            "newPassword" => newPassword,
            _ => null,
        };

        static bool Check(AuthField field, bool ok)
        {
            field.SetError(!ok);
            return ok;
        }

        // ------------------------------------------------------------ mot de passe

        void ChangePassword()
        {
            string next = newPassword.RawValue;
            bool strong = next.Length >= 8 && next.Any(char.IsLetter) && next.Any(char.IsDigit);
            bool ok = Check(currentPassword, currentPassword.RawValue.Length > 0)
                      & Check(newPassword, strong)
                      & Check(confirmPassword, confirmPassword.RawValue == next);
            if (!ok)
            {
                SetStatus(passwordStatus, !strong ? "Nouveau mot de passe : 8 caractères avec lettres et chiffres" : "Vérifie les champs en rouge", ErrorColor);
                return;
            }

            passwordButton.interactable = false;
            StartCoroutine(AuthApi.ChangePassword(Token, currentPassword.RawValue, next, r =>
            {
                passwordButton.interactable = true;
                if (!r.Ok)
                {
                    FieldFor(r.Field)?.SetError(true);
                    SetStatus(passwordStatus, r.Error, ErrorColor);
                    return;
                }
                currentPassword.Clear();
                newPassword.Clear();
                confirmPassword.Clear();
                SetStatus(passwordStatus, "Mot de passe modifié", SuccessColor);
            }));
        }

        // ------------------------------------------------------------ deconnexion

        void Logout()
        {
            AuthSession.Logout();
            SceneManager.LoadScene(AuthSettings.Get().authScene);
        }

        static void SetStatus(TMP_Text label, string text, Color color)
        {
            label.text = text;
            label.color = color;
        }
    }
}
