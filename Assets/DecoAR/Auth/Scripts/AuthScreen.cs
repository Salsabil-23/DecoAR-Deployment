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
    // Ecran d'accueil : inscription (formulaire + defi de mouvements qui enregistre le visage),
    // verification de l'e-mail, connexion (mot de passe ou visage avec defi), mot de passe oublie,
    // et connexion automatique si la session est encore valide
    public class AuthScreen : MonoBehaviour
    {
        [Header("Mise en page (paysage / portrait)")]
        [SerializeField] CanvasScaler scaler;
        [SerializeField] RectTransform brandPanel;
        [SerializeField] GameObject brandDetails; // masque en portrait (pas la place)
        [SerializeField] RectTransform cardArea;

        [Header("Chargement")]
        [SerializeField] GameObject loadingPanel;
        [SerializeField] TMP_Text loadingLabel;

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
        [SerializeField] TMP_Text registerStatus;
        [SerializeField] Button registerButton;
        [SerializeField] Button goToLoginButton;

        [Header("Enregistrement du visage (inscription)")]
        [SerializeField] GameObject enrollPanel;
        [SerializeField] FaceChallenge enrollChallenge;
        [SerializeField] TMP_Text enrollStatus;

        [Header("Verification de l'e-mail")]
        [SerializeField] GameObject verifyPanel;
        [SerializeField] TMP_Text verifySubtitle;
        [SerializeField] AuthField verifyCode;
        [SerializeField] TMP_Text verifyStatus;
        [SerializeField] Button verifyButton;
        [SerializeField] Button resendButton;
        [SerializeField] Button verifySwitchAccountButton;

        [Header("Connexion")]
        [SerializeField] GameObject loginPanel;
        [SerializeField] AuthField loginEmail;
        [SerializeField] AuthField loginPassword;
        [SerializeField] Button forgotLinkButton;
        [SerializeField] TMP_Text loginStatus;
        [SerializeField] Button loginButton;
        [SerializeField] Button faceLoginButton;
        [SerializeField] Button goToRegisterButton;

        [Header("Connexion par le visage")]
        [SerializeField] GameObject faceLoginPanel;
        [SerializeField] TMP_Text faceLoginSubtitle;
        [SerializeField] FaceChallenge faceLoginChallenge;
        [SerializeField] TMP_Text faceLoginStatus;

        [Header("Mot de passe oublie")]
        [SerializeField] GameObject forgotPanel;
        [SerializeField] AuthField forgotEmail;
        [SerializeField] Button forgotSendButton;
        [SerializeField] GameObject forgotStep2;
        [SerializeField] AuthField forgotCode;
        [SerializeField] AuthField forgotPassword;
        [SerializeField] AuthField forgotConfirm;
        [SerializeField] Button forgotResetButton;
        [SerializeField] TMP_Text forgotStatus;
        [SerializeField] Button forgotBackButton;

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

        GameObject[] panels;
        bool busy;
        bool? portrait;
        // Texte d'origine des boutons pendant l'etat "chargement"
        readonly Dictionary<Button, string> idleLabels = new Dictionary<Button, string>();

        void Start()
        {
            panels = new[] { loadingPanel, registerPanel, enrollPanel, verifyPanel, loginPanel, faceLoginPanel, forgotPanel };

            registerButton.onClick.AddListener(OnRegisterForm);
            goToLoginButton.onClick.AddListener(() => ShowLogin());
            password.Input.onValueChanged.AddListener(UpdateStrength);
            UpdateStrength("");

            enrollChallenge.Purpose = "enroll";
            enrollChallenge.StartText = "Commencer le défi";
            enrollChallenge.Completed += OnFaceEnrolled;
            enrollChallenge.Cancelled += () => Show(registerPanel);

            verifyButton.onClick.AddListener(OnVerify);
            verifyCode.Input.onValueChanged.AddListener(code => { if (code.Length == 6) OnVerify(); });
            resendButton.onClick.AddListener(OnResend);
            verifySwitchAccountButton.onClick.AddListener(() => { AuthSession.Logout(); ShowLogin(); });

            loginButton.onClick.AddListener(OnPasswordLogin);
            faceLoginButton.onClick.AddListener(ShowFaceLogin);
            goToRegisterButton.onClick.AddListener(() => Show(registerPanel));

            faceLoginChallenge.Purpose = "login";
            faceLoginChallenge.StartText = "Me reconnaître";
            faceLoginChallenge.Completed += OnFaceLogin;
            faceLoginChallenge.Cancelled += () => ShowLogin();

            forgotLinkButton.onClick.AddListener(ShowForgot);
            forgotSendButton.onClick.AddListener(OnForgotSend);
            forgotResetButton.onClick.AddListener(OnForgotReset);
            forgotBackButton.onClick.AddListener(() => ShowLogin());

            StartCoroutine(AuthApi.Wake()); // reveille le serveur en ligne pendant que l'utilisateur tape
            AutoLogin();
        }

        void Update()
        {
            bool p = Screen.height > Screen.width;
            if (portrait != p) ApplyLayout(p);
        }

        // ------------------------------------------------------------ navigation

        void Show(GameObject panel)
        {
            foreach (var p in panels) p.SetActive(p == panel);
            foreach (var s in new[] { registerStatus, enrollStatus, verifyStatus, loginStatus, faceLoginStatus, forgotStatus }) SetStatus(s, "", InfoColor);
        }

        void ShowLogin(string message = null)
        {
            Show(loginPanel);
            if (string.IsNullOrEmpty(loginEmail.RawValue))
                loginEmail.Input.text = !string.IsNullOrEmpty(AuthSession.LastEmail) ? AuthSession.LastEmail : email.Value;
            if (message != null) SetStatus(loginStatus, message, ErrorColor);
        }

        void ShowLoading(string text)
        {
            Show(loadingPanel);
            loadingLabel.text = text;
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

        // ------------------------------------------------------------ connexion automatique

        void AutoLogin()
        {
            if (!AuthSession.IsLoggedIn)
            {
                // Deja venu sur ce telephone : connexion ; sinon : inscription
                if (!string.IsNullOrEmpty(AuthSession.LastEmail)) ShowLogin();
                else Show(registerPanel);
                return;
            }

            ShowLoading($"Reconnexion de {AuthSession.User.firstName}…");
            StartCoroutine(AuthApi.Me(AuthSession.Token, r =>
            {
                if (r.Ok)
                {
                    AuthSession.UpdateUser(r.Data.user);
                    ContinueAfterAuth(loadingLabel);
                }
                else if (r.Status == 401)
                {
                    AuthSession.Logout();
                    ShowLogin("Session expirée, reconnecte-toi");
                }
                else ShowLogin(r.Error);
            }));
        }

        // Apres inscription / connexion : e-mail verifie -> boutique
        void ContinueAfterAuth(TMP_Text status)
        {
            if (!AuthSession.User.emailVerified) { ShowVerify(); return; }
            Welcome(status);
        }

        // ------------------------------------------------------------ inscription

        // Etape 1 : le formulaire est valide localement, on passe a la camera
        void OnRegisterForm()
        {
            if (busy) return;
            if (!ValidateRegister(out string error)) { SetStatus(registerStatus, error, ErrorColor); return; }
            Show(enrollPanel);
        }

        // Etape 2 : defi de mouvements reussi -> creation du compte (le serveur revérifie tout avec OpenCV)
        void OnFaceEnrolled(FaceProof proof)
        {
            if (busy) return;
            busy = true;
            enrollChallenge.SetMessage("Création du compte…");
            SetStatus(enrollStatus, "", InfoColor);
            StartCoroutine(AuthApi.Register(firstName.Value, lastName.Value, email.Value.ToLowerInvariant(), phone.Value, password.RawValue, proof, r =>
            {
                busy = false;
                if (!r.Ok)
                {
                    if (r.Field == "face" || r.IsNetworkError)
                    {
                        // Defi refuse (mouvement douteux, visage deja utilise...) : on recommence le defi
                        enrollChallenge.Fail(r.Error);
                    }
                    else
                    {
                        // Probleme dans le formulaire (e-mail deja pris...) : retour au formulaire
                        Show(registerPanel);
                        FieldFor(r.Field)?.SetError(true);
                        SetStatus(registerStatus, r.Error, ErrorColor);
                    }
                    return;
                }
                AuthSession.Set(r.Data.token, r.Data.user);
                ContinueAfterAuth(enrollStatus);
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

        // ------------------------------------------------------------ verification de l'e-mail

        void ShowVerify()
        {
            Show(verifyPanel);
            verifyCode.Clear();
            verifySubtitle.text = $"Saisis le code à 6 chiffres envoyé à <b>{AuthSession.User.email}</b>";
        }

        void OnVerify()
        {
            if (busy) return;
            string code = verifyCode.Value;
            if (code.Length != 6)
            {
                verifyCode.SetError(true);
                SetStatus(verifyStatus, "Le code contient 6 chiffres", ErrorColor);
                return;
            }

            SetBusy(verifyButton, true, "Vérification…");
            StartCoroutine(AuthApi.VerifyEmail(AuthSession.Token, code, r =>
            {
                SetBusy(verifyButton, false);
                if (!r.Ok)
                {
                    verifyCode.SetError(true);
                    SetStatus(verifyStatus, r.Error, ErrorColor);
                    return;
                }
                AuthSession.UpdateUser(r.Data.user);
                ContinueAfterAuth(verifyStatus);
            }));
        }

        void OnResend()
        {
            if (busy) return;
            SetBusy(resendButton, true, "Envoi…");
            StartCoroutine(AuthApi.ResendVerification(AuthSession.Token, r =>
            {
                SetBusy(resendButton, false);
                SetStatus(verifyStatus, r.Ok ? "Nouveau code envoyé, regarde tes e-mails (et les spams)" : r.Error, r.Ok ? SuccessColor : ErrorColor);
            }));
        }

        // ------------------------------------------------------------ connexion par mot de passe

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
                SetBusy(loginButton, false);
                if (!r.Ok)
                {
                    loginPassword.SetError(true);
                    SetStatus(loginStatus, r.Error, ErrorColor);
                    return;
                }
                AuthSession.Set(r.Data.token, r.Data.user);
                ContinueAfterAuth(loginStatus);
            }));
        }

        // ------------------------------------------------------------ connexion par le visage

        // E-mail saisi : on compare a CE compte (plus sur). Sinon : on cherche parmi tous les comptes.
        string FaceLoginEmail => EmailRe.IsMatch(loginEmail.Value) ? loginEmail.Value.ToLowerInvariant() : null;

        void ShowFaceLogin()
        {
            Show(faceLoginPanel);
            faceLoginSubtitle.text = FaceLoginEmail != null
                ? $"Regarde la caméra pour te connecter en tant que <b>{FaceLoginEmail}</b>"
                : "Suis les mouvements demandés : DecoAR va te reconnaître";
        }

        void OnFaceLogin(FaceProof proof)
        {
            if (busy) return;
            busy = true;
            faceLoginChallenge.SetMessage("Reconnaissance en cours…");
            SetStatus(faceLoginStatus, "", InfoColor);
            StartCoroutine(AuthApi.FaceLogin(FaceLoginEmail, proof, r =>
            {
                busy = false;
                if (!r.Ok)
                {
                    faceLoginChallenge.Fail(r.Error);
                    return;
                }
                AuthSession.Set(r.Data.token, r.Data.user);
                ContinueAfterAuth(faceLoginStatus);
            }));
        }

        // ------------------------------------------------------------ mot de passe oublie

        void ShowForgot()
        {
            Show(forgotPanel);
            forgotStep2.SetActive(false);
            forgotEmail.Input.text = loginEmail.Value;
        }

        void OnForgotSend()
        {
            if (busy) return;
            if (!EmailRe.IsMatch(forgotEmail.Value))
            {
                forgotEmail.SetError(true);
                SetStatus(forgotStatus, "Adresse e-mail invalide", ErrorColor);
                return;
            }
            SetBusy(forgotSendButton, true, "Envoi…");
            StartCoroutine(AuthApi.ForgotPassword(forgotEmail.Value.ToLowerInvariant(), r =>
            {
                SetBusy(forgotSendButton, false);
                if (!r.Ok) { SetStatus(forgotStatus, r.Error, ErrorColor); return; }
                forgotStep2.SetActive(true);
                SetStatus(forgotStatus, "Si un compte existe, un code vient d'être envoyé (pense aux spams)", SuccessColor);
            }));
        }

        void OnForgotReset()
        {
            if (busy) return;
            bool codeOk = forgotCode.Value.Length == 6;
            bool pwOk = PasswordScore(forgotPassword.RawValue) >= 1;
            bool sameOk = forgotConfirm.RawValue == forgotPassword.RawValue;
            forgotCode.SetError(!codeOk);
            forgotPassword.SetError(!pwOk);
            forgotConfirm.SetError(!sameOk);
            if (!codeOk || !pwOk || !sameOk)
            {
                SetStatus(forgotStatus, !codeOk ? "Le code contient 6 chiffres"
                    : !pwOk ? "Mot de passe : 8 caractères minimum avec lettres et chiffres"
                    : "Les mots de passe ne correspondent pas", ErrorColor);
                return;
            }

            SetBusy(forgotResetButton, true, "Changement…");
            StartCoroutine(AuthApi.ResetPassword(forgotEmail.Value.ToLowerInvariant(), forgotCode.Value, forgotPassword.RawValue, r =>
            {
                SetBusy(forgotResetButton, false);
                if (!r.Ok)
                {
                    (r.Field == "password" ? forgotPassword : forgotCode).SetError(true);
                    SetStatus(forgotStatus, r.Error, ErrorColor);
                    return;
                }
                AuthSession.Set(r.Data.token, r.Data.user);
                ContinueAfterAuth(forgotStatus);
            }));
        }

        // ------------------------------------------------------------ fin

        void Welcome(TMP_Text status)
        {
            SetStatus(status, $"Bienvenue {AuthSession.User.firstName} !", SuccessColor);
            busy = true;
            StartCoroutine(LoadNext());
        }

        IEnumerator LoadNext()
        {
            yield return new WaitForSeconds(1.2f);
            string next = AuthSettings.Get().firstSceneAfterLogin;
            if (Application.CanStreamedLevelBeLoaded(next)) SceneManager.LoadScene(next);
            else Debug.LogWarning($"[Auth] Scene '{next}' absente des Build Settings");
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
