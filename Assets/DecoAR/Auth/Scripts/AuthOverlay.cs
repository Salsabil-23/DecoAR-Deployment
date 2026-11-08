using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DecoAR.Auth
{
    // Couche persistante au-dessus de toutes les scenes (prefab Resources/DecoAR_AuthOverlay) :
    //  - bouton rond "profil" en haut a gauche (hors scenes Auth / Profil)
    //  - verrouillage automatique : app en arriere-plan > lockAfterSeconds -> reconnaissance faciale pour revenir
    //  - confirmation de l'espace admin (ConfirmAdmin) : meme ecran, meme defi, jeton admin en retour
    public class AuthOverlay : MonoBehaviour
    {
        public static AuthOverlay Instance { get; private set; }

        [Header("Bouton profil")]
        [SerializeField] GameObject profileBubble;
        [SerializeField] Button profileButton;
        [SerializeField] RawImage profilePhoto;
        [SerializeField] TMP_Text profileInitials;

        [Header("Verrouillage")]
        [SerializeField] GameObject lockPanel;
        [SerializeField] TMP_Text lockSubtitle;
        [SerializeField] GameObject lockCameraGroup;
        [SerializeField] FaceChallenge lockChallenge;
        [SerializeField] Button passwordButton;
        [SerializeField] TMP_Text lockStatus;

        DateTime? backgroundSince;
        bool unlocking;
        // Mode "espace admin" : non null tant qu'on attend la confirmation par le visage
        Action<bool> adminDone;
        TMP_Text lockTitle, passwordLabel;
        string lockTitleText, passwordText, cancelText;
        Texture2D photo;
        string photoVersion;

        public bool IsLocked => lockPanel.activeSelf;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var prefab = Resources.Load<AuthOverlay>("DecoAR_AuthOverlay");
            if (prefab == null)
            {
                Debug.LogWarning("[Auth] Prefab DecoAR_AuthOverlay absent : menu DecoAR > Auth > Tout generer");
                return;
            }
            Instantiate(prefab).name = "AuthOverlay";
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            profileButton.onClick.AddListener(ProfileNavigation.Open);
            passwordButton.onClick.AddListener(() => { if (adminDone != null) EndAdmin(false); else UsePassword(); });
            lockChallenge.Purpose = "login";
            lockChallenge.StartText = "Déverrouiller";
            lockChallenge.Completed += proof => { if (adminDone != null) ConfirmAdminFace(proof); else Unlock(proof); };
            lockChallenge.Cancelled += () => { if (adminDone != null) EndAdmin(false); else UsePassword(); };
            lockPanel.SetActive(false);

            // Textes de l'ecran, changes en mode admin puis remis (le titre n'est pas relie dans le prefab)
            lockTitle = lockPanel.transform.Find("Contenu/Titre")?.GetComponentInChildren<TMP_Text>();
            passwordLabel = passwordButton.GetComponentInChildren<TMP_Text>();
            lockTitleText = lockTitle != null ? lockTitle.text : "";
            passwordText = passwordLabel != null ? passwordLabel.text : "";
            cancelText = lockChallenge.CancelText;

            SceneManager.sceneLoaded += (_, __) => Refresh();
            AuthSession.Changed += _ => Refresh();
            Refresh();
        }

        // ------------------------------------------------------------ bouton profil

        void Refresh()
        {
            var settings = AuthSettings.Get();
            string scene = SceneManager.GetActiveScene().name;
            // Pas de bulle dans la boutique M5 (sa carte profil en haut à gauche ouvre le même profil)
            // ni dans le catalogue M3 (elle cachait son bouton « ‹ Boutique », le profil reste à un toucher dans la boutique)
            bool show = AuthSession.IsLoggedIn && scene != settings.authScene && scene != settings.profileScene
                && scene != DecoAR.Core.SceneNames.Shop && scene != DecoAR.Core.SceneNames.Catalog;
            profileBubble.SetActive(show);
            if (!show) return;

            var user = AuthSession.User;
            profileInitials.text = $"{Initial(user.firstName)}{Initial(user.lastName)}";
            // Photo retelechargee seulement si elle a change
            if (user.hasPhoto && user.photoUpdatedAt != photoVersion)
            {
                photoVersion = user.photoUpdatedAt;
                StartCoroutine(AuthApi.DownloadPhoto(AuthSession.Token, SetPhoto));
            }
            else if (!user.hasPhoto) SetPhoto(null);
        }

        void SetPhoto(Texture2D tex)
        {
            if (photo != null && photo != tex) Destroy(photo);
            photo = tex;
            profilePhoto.texture = tex;
            profilePhoto.enabled = tex != null;
            profileInitials.enabled = tex == null;
        }

        static string Initial(string s) => string.IsNullOrEmpty(s) ? "" : s.Substring(0, 1).ToUpperInvariant();

        // ------------------------------------------------------------ verrouillage

        void OnApplicationPause(bool paused)
        {
            if (paused) backgroundSince = DateTime.UtcNow;
            else CheckLock();
        }

        void CheckLock()
        {
            if (backgroundSince == null) return;
            double away = (DateTime.UtcNow - backgroundSince.Value).TotalSeconds;
            backgroundSince = null;
            if (away >= AuthSettings.Get().lockAfterSeconds) Lock();
        }

        // Public pour les tests : menu DecoAR > Auth > Verrouiller maintenant
        public void Lock()
        {
            if (!AuthSession.IsLoggedIn || SceneManager.GetActiveScene().name == AuthSettings.Get().authScene) return;
            if (adminDone != null) EndAdmin(false);
            SetTexts(lockTitleText, passwordText, cancelText, "Déverrouiller");
            AuthSession.ClearAdminToken(); // apres une absence, l'espace admin se reconfirme

            bool canUseFace = AuthSession.User.faceEnrolled;
            lockStatus.text = "";
            lockSubtitle.text = canUseFace
                ? $"Bon retour {AuthSession.User.firstName} ! Suis les mouvements pour continuer."
                : "Pour ta sécurité, reconnecte-toi.";
            lockCameraGroup.SetActive(canUseFace); // la camera s'ouvre avec son groupe (lien "mot de passe" inclus)
            passwordButton.gameObject.SetActive(!canUseFace);
            lockPanel.SetActive(true);
        }

        // Le visage doit correspondre au compte connecte (comparaison 1:1 cote serveur)
        void Unlock(FaceProof proof)
        {
            if (unlocking) return;
            unlocking = true;
            lockChallenge.SetMessage("Reconnaissance en cours…");
            lockStatus.text = "";
            StartCoroutine(AuthApi.FaceLogin(AuthSession.User.email, proof, r =>
            {
                unlocking = false;
                if (!r.Ok)
                {
                    lockChallenge.Fail(r.Error);
                    return;
                }
                AuthSession.Set(r.Data.token, r.Data.user); // jeton renouvele
                lockCameraGroup.SetActive(false);
                lockPanel.SetActive(false);
            }));
        }

        // ------------------------------------------------------------ espace admin

        /// Ouvre l'espace admin : compte admin + defi de mouvements avec son visage (verifie par le serveur).
        /// done(true) quand le jeton admin est obtenu (ou deja valide), done(false) si annule ou refuse.
        public void ConfirmAdmin(Action<bool> done)
        {
            if (!AuthSession.IsAdmin) { done?.Invoke(false); return; }
            if (AuthSession.HasAdminAccess) { done?.Invoke(true); return; }

            adminDone = done;
            bool canUseFace = AuthSession.User.faceEnrolled;
            SetTexts("Espace administration", "Fermer", "<color=#F3E9DD>Annuler</color>", "Confirmer mon identité");
            lockStatus.text = "";
            lockSubtitle.text = canUseFace
                ? $"{AuthSession.User.firstName}, confirme que c'est bien toi : suis les mouvements avec ton visage."
                : "Enregistre d'abord ton visage dans ton profil (Mon visage) pour ouvrir l'espace admin.";
            lockCameraGroup.SetActive(canUseFace);
            passwordButton.gameObject.SetActive(!canUseFace);
            lockPanel.SetActive(true);
        }

        void ConfirmAdminFace(FaceProof proof)
        {
            if (unlocking) return;
            unlocking = true;
            lockChallenge.SetMessage("Vérification…");
            lockStatus.text = "";
            StartCoroutine(AuthApi.AdminConfirm(AuthSession.Token, proof, r =>
            {
                unlocking = false;
                if (!r.Ok)
                {
                    lockChallenge.Fail(r.Error);
                    return;
                }
                AuthSession.UpdateUser(r.Data.user);
                AuthSession.SetAdminToken(r.Data.adminToken, r.Data.expiresIn);
                EndAdmin(true);
            }));
        }

        void EndAdmin(bool ok)
        {
            var done = adminDone;
            adminDone = null;
            lockCameraGroup.SetActive(false);
            lockPanel.SetActive(false);
            SetTexts(lockTitleText, passwordText, cancelText, "Déverrouiller");
            done?.Invoke(ok);
        }

        void SetTexts(string title, string password, string cancel, string start)
        {
            if (lockTitle != null) lockTitle.text = title;
            if (passwordLabel != null) passwordLabel.text = password;
            lockChallenge.CancelText = cancel;
            lockChallenge.StartText = start;
        }

        void UsePassword()
        {
            lockCameraGroup.SetActive(false);
            lockPanel.SetActive(false);
            AuthSession.Logout();
            SceneManager.LoadScene(AuthSettings.Get().authScene);
        }
    }

    // Aller au profil depuis n'importe quelle scene, puis revenir au meme endroit
    public static class ProfileNavigation
    {
        public static string ReturnScene { get; private set; }

        public static void Open()
        {
            ReturnScene = SceneManager.GetActiveScene().name;
            // Boutique M1 : on retient la position de l'avatar pour le remettre au meme endroit
            if (DecoAR.Player.PlayerController.Instance != null) DecoAR.Player.PlayerController.Instance.SaveShopPose();
            // Boutique de la caisse (M5) : son joueur reprend la position gardee par le GameManager a son retour
            else if (SceneManager.GetActiveScene().name == DecoAR.Core.SceneNames.Shop && DecoAR.Core.GameManager.Instance != null)
            {
                var player = UnityEngine.Object.FindAnyObjectByType<CharacterController>();
                if (player != null) DecoAR.Core.GameManager.Instance.SaveShopPose(player.transform.position, player.transform.rotation);
            }
            SceneManager.LoadScene(AuthSettings.Get().profileScene);
        }

        public static void Back()
        {
            string target = string.IsNullOrEmpty(ReturnScene) ? AuthSettings.Get().firstSceneAfterLogin : ReturnScene;
            SceneManager.LoadScene(target);
        }
    }
}
