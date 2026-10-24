using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DecoAR.Auth
{
    // Ecran d'accueil : inscription (formulaire) et connexion (mot de passe ou Face ID)
    public class AuthScreen : MonoBehaviour
    {
        [SerializeField] string apiBaseUrl = "http://localhost:3000";
        [SerializeField] string nextSceneName = "M1_AvatarSelection";

        [Header("Mise en page (paysage / portrait)")]
        [SerializeField] CanvasScaler scaler;
        [SerializeField] RectTransform brandPanel;
        [SerializeField] GameObject brandDetails; // masque en portrait (pas la place)
        [SerializeField] RectTransform cardArea;

        [Header("Inscription")]
        [SerializeField] GameObject registerPanel;
        [SerializeField] AuthField firstName;
        [SerializeField] AuthField lastName;
        [SerializeField] AuthField email;
        [SerializeField] AuthField phone;
        [SerializeField] AuthField password;
        [SerializeField] AuthField confirmPassword;
        [SerializeField] Image[] strengthBars;
        [SerializeField] TMP_Text strengthLabel;
        [SerializeField] GameObject registerFaceIdRow;
        [SerializeField] Toggle registerFaceIdToggle;
        [SerializeField] TMP_Text registerStatus;
        [SerializeField] Button registerButton;
        [SerializeField] Button goToLoginButton;

        [Header("Connexion")]
        [SerializeField] GameObject loginPanel;
        [SerializeField] AuthField loginEmail;
        [SerializeField] AuthField loginPassword;
        [SerializeField] GameObject loginFaceIdRow;
        [SerializeField] Toggle loginFaceIdToggle;
        [SerializeField] TMP_Text loginStatus;
        [SerializeField] Button loginButton;
        [SerializeField] GameObject faceIdLoginGroup;
        [SerializeField] Button faceIdLoginButton;
        [SerializeField] TMP_Text faceIdLoginLabel;
        [SerializeField] Button goToRegisterButton;

        static readonly Regex EmailRe = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$");
        static readonly Regex PhoneRe = new Regex(@"^\+?[0-9 ]{8,15}$");
        static readonly Color32 ErrorColor = new Color32(0xD6, 0x45, 0x45, 0xFF);
        static readonly Color32 InfoColor = new Color32(0x8A, 0x81, 0x78, 0xFF);
        static readonly Color32 SuccessColor = new Color32(0x3E, 0x9B, 0x6A, 0xFF);
        static readonly Color32 BarOff = new Color32(0xE8, 0xE1, 0xD8, 0xFF);
        static readonly Color32[] StrengthColors =
        {
            new Color32(0xD6, 0x45, 0x45, 0xFF), new Color32(0xE8, 0x8B, 0x3C, 0xFF),
            new Color32(0xD9, 0xB4, 0x3A, 0xFF), new Color32(0x3E, 0x9B, 0x6A, 0xFF),
        };
        static readonly string[] StrengthNames = { "Trop faible", "Moyen", "Bon", "Excellent" };

        IBiometricAuthenticator Bio => BiometricAuth.Current;
        bool busy;
        bool? portrait;
        // Texte d'origine des boutons pendant l'etat "chargement"
        readonly Dictionary<Button, string> idleLabels = new Dictionary<Button, string>();

        void Awake()
        {
            AuthApi.BaseUrl = apiBaseUrl;
        }

        void Start()
        {
            registerButton.onClick.AddListener(OnRegister);
            loginButton.onClick.AddListener(OnPasswordLogin);
            faceIdLoginButton.onClick.AddListener(OnFaceIdLogin);
            goToLoginButton.onClick.AddListener(() => ShowLogin(true));
            goToRegisterButton.onClick.AddListener(() => ShowLogin(false));
            password.Input.onValueChanged.AddListener(UpdateStrength);
            UpdateStrength("");

            // Face ID deja active sur ce telephone : on propose directement la connexion
            ShowLogin(Bio.IsAvailable && BiometricVault.HasToken);
        }

        void Update()
        {
            bool p = Screen.height > Screen.width;
            if (portrait != p) ApplyLayout(p);
        }

        // ------------------------------------------------------------ navigation

        void ShowLogin(bool login)
        {
            registerPanel.SetActive(!login);
            loginPanel.SetActive(login);
            SetStatus(registerStatus, "", InfoColor);
            SetStatus(loginStatus, "", InfoColor);
            if (login && string.IsNullOrEmpty(loginEmail.RawValue))
                loginEmail.Input.text = !string.IsNullOrEmpty(BiometricVault.Email) ? BiometricVault.Email : email.Value;
            RefreshFaceId();
        }

        void RefreshFaceId()
        {
            bool available = Bio.IsAvailable;
            registerFaceIdRow.SetActive(available);
            loginFaceIdRow.SetActive(available && !BiometricVault.HasToken);
            faceIdLoginGroup.SetActive(available && BiometricVault.HasToken);
            faceIdLoginLabel.text = $"Se connecter avec {Bio.Label}";
        }

        void ApplyLayout(bool isPortrait)
        {
            portrait = isPortrait;
            scaler.referenceResolution = isPortrait ? new Vector2(1080, 1920) : new Vector2(1920, 1080);
            brandDetails.SetActive(!isPortrait);
            // Le bloc logo + titre (parent des atouts) est reduit pour tenir dans le bandeau du haut
            brandDetails.transform.parent.localScale = Vector3.one * (isPortrait ? 0.75f : 1f);
            if (isPortrait)
            {
                Anchor(brandPanel, new Vector2(0, 0.82f), Vector2.one);
                Anchor(cardArea, Vector2.zero, new Vector2(1, 0.82f));
            }
            else
            {
                Anchor(brandPanel, Vector2.zero, new Vector2(0.44f, 1));
                Anchor(cardArea, new Vector2(0.44f, 0), Vector2.one);
            }
        }

        static void Anchor(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ------------------------------------------------------------ inscription

        void OnRegister()
        {
            if (busy) return;
            if (!ValidateRegister(out string error)) { SetStatus(registerStatus, error, ErrorColor); return; }

            SetBusy(registerButton, true, "Création du compte…");
            StartCoroutine(AuthApi.Register(firstName.Value, lastName.Value, email.Value.ToLowerInvariant(), phone.Value, password.RawValue, r =>
            {
                if (!r.Ok)
                {
                    SetBusy(registerButton, false);
                    FieldFor(r.Field)?.SetError(true);
                    SetStatus(registerStatus, r.Error, ErrorColor);
                    return;
                }
                AuthSession.Set(r.Data.token, r.Data.user);
                AfterLogin(registerStatus, registerFaceIdRow.activeSelf && registerFaceIdToggle.isOn);
            }));
        }

        bool ValidateRegister(out string error)
        {
            var checks = new (AuthField field, bool ok, string message)[]
            {
                (firstName, firstName.Value.Length >= 2, "Indique ton prénom"),
                (lastName, lastName.Value.Length >= 2, "Indique ton nom"),
                (email, EmailRe.IsMatch(email.Value), "Adresse e-mail invalide"),
                (phone, phone.Value.Length == 0 || PhoneRe.IsMatch(phone.Value), "Numéro de téléphone invalide"),
                (password, PasswordScore(password.RawValue) >= 1, "Mot de passe : 8 caractères minimum avec lettres et chiffres"),
                (confirmPassword, confirmPassword.RawValue == password.RawValue, "Les mots de passe ne correspondent pas"),
            };
            foreach (var c in checks) c.field.SetError(!c.ok);
            var first = checks.FirstOrDefault(c => !c.ok);
            error = first.message;
            return first.field == null;
        }

        AuthField FieldFor(string serverField) => serverField switch
        {
            "firstName" => firstName,
            "lastName" => lastName,
            "email" => email,
            "phone" => phone,
            "password" => password,
            _ => null,
        };

        // 0 = refuse par le serveur, 1..4 = moyen a excellent
        static int PasswordScore(string pw)
        {
            if (pw.Length < 8 || !pw.Any(char.IsLetter) || !pw.Any(char.IsDigit)) return 0;
            int score = 1;
            if (pw.Any(char.IsUpper) && pw.Any(char.IsLower)) score++;
            if (pw.Any(ch => !char.IsLetterOrDigit(ch))) score++;
            if (pw.Length >= 12) score++;
            return Mathf.Min(score, 4);
        }

        void UpdateStrength(string pw)
        {
            int score = PasswordScore(pw);
            int lit = pw.Length == 0 ? 0 : Mathf.Max(1, score);
            Color on = StrengthColors[Mathf.Max(0, lit - 1)];
            for (int i = 0; i < strengthBars.Length; i++) strengthBars[i].color = i < lit ? on : (Color)BarOff;
            strengthLabel.text = pw.Length == 0 ? "8 caractères, lettres et chiffres" : StrengthNames[Mathf.Max(0, score - 1)];
            strengthLabel.color = pw.Length == 0 ? (Color)InfoColor : on;
        }

        // ------------------------------------------------------------ connexion

        void OnPasswordLogin()
        {
            if (busy) return;
            bool emailOk = EmailRe.IsMatch(loginEmail.Value);
            bool passwordOk = loginPassword.RawValue.Length > 0;
            loginEmail.SetError(!emailOk);
            loginPassword.SetError(!passwordOk);
            if (!emailOk || !passwordOk)
            {
                SetStatus(loginStatus, emailOk ? "Saisis ton mot de passe" : "Adresse e-mail invalide", ErrorColor);
                return;
            }

            SetBusy(loginButton, true, "Connexion…");
            StartCoroutine(AuthApi.Login(loginEmail.Value.ToLowerInvariant(), loginPassword.RawValue, r =>
            {
                if (!r.Ok)
                {
                    SetBusy(loginButton, false);
                    loginPassword.SetError(true);
                    SetStatus(loginStatus, r.Error, ErrorColor);
                    return;
                }
                AuthSession.Set(r.Data.token, r.Data.user);
                AfterLogin(loginStatus, loginFaceIdRow.activeSelf && loginFaceIdToggle.isOn);
            }));
        }

        void OnFaceIdLogin()
        {
            if (busy) return;
            string savedEmail = BiometricVault.Email;
            SetBusy(faceIdLoginButton, true, $"{Bio.Label}…");

            Bio.Authenticate("Connecte-toi à DecoAR", (ok, err) =>
            {
                if (!ok)
                {
                    SetBusy(faceIdLoginButton, false);
                    SetStatus(loginStatus, err ?? "Visage non reconnu", ErrorColor);
                    return;
                }
                StartCoroutine(AuthApi.BiometricLogin(savedEmail, BiometricAuth.DeviceId, BiometricVault.Token, r =>
                {
                    SetBusy(faceIdLoginButton, false);
                    if (!r.Ok)
                    {
                        // Jeton refuse (Face ID desactive ailleurs) : on repasse au mot de passe
                        if (r.Status == 401) { BiometricVault.Clear(); RefreshFaceId(); }
                        SetStatus(loginStatus, r.Error, ErrorColor);
                        return;
                    }
                    AuthSession.Set(r.Data.token, r.Data.user);
                    Welcome(loginStatus);
                }));
            });
        }

        // Apres inscription / connexion : active Face ID si demande, puis entre dans l'app
        void AfterLogin(TMP_Text status, bool enableFaceId)
        {
            if (!enableFaceId) { Welcome(status); return; }

            SetStatus(status, $"Valide avec {Bio.Label} pour l'activer…", InfoColor);
            Bio.Authenticate($"Active {Bio.Label} pour te connecter plus vite", (ok, err) =>
            {
                if (!ok)
                {
                    // Le compte est cree quand meme : Face ID pourra etre active plus tard
                    Welcome(status, $"{Bio.Label} non activé ({err})");
                    return;
                }
                StartCoroutine(AuthApi.EnrollBiometric(AuthSession.Token, BiometricAuth.DeviceId, BiometricAuth.DeviceName, r =>
                {
                    if (r.Ok)
                    {
                        BiometricVault.Save(AuthSession.User.email, r.Data.biometricToken);
                        AuthSession.UpdateUser(r.Data.user);
                        Welcome(status, $"{Bio.Label} activé");
                    }
                    else Welcome(status, $"{Bio.Label} non activé ({r.Error})");
                }));
            });
        }

        void Welcome(TMP_Text status, string extra = null)
        {
            string text = $"Bienvenue {AuthSession.User.firstName} !";
            if (!string.IsNullOrEmpty(extra)) text += $"\n<size=80%>{extra}</size>";
            SetStatus(status, text, SuccessColor);
            busy = true;
            StartCoroutine(LoadNext());
        }

        IEnumerator LoadNext()
        {
            yield return new WaitForSeconds(1.2f);
            if (Application.CanStreamedLevelBeLoaded(nextSceneName)) SceneManager.LoadScene(nextSceneName);
            else Debug.LogWarning($"[Auth] Scene '{nextSceneName}' absente des Build Settings");
        }

        // ------------------------------------------------------------ utilitaires

        void SetBusy(Button button, bool value, string text = null)
        {
            busy = value;
            var label = button.GetComponentInChildren<TMP_Text>();
            if (value)
            {
                if (!idleLabels.ContainsKey(button)) idleLabels[button] = label.text;
                label.text = text;
            }
            else if (idleLabels.Remove(button, out string original)) label.text = original;
            button.interactable = !value;
        }

        static void SetStatus(TMP_Text label, string text, Color color)
        {
            label.text = text;
            label.color = color;
        }
    }
}
