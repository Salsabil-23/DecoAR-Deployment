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
            passwordButton.onClick.AddListener(UsePassword);
            lockChallenge.Purpose = "login";
            lockChallenge.StartText = "Déverrouiller";
            lockChallenge.Completed += Unlock;
            lockChallenge.Cancelled += UsePassword;
            lockPanel.SetActive(false);

            SceneManager.sceneLoaded += (_, __) => Refresh();
            AuthSession.Changed += _ => Refresh();
            Refresh();
        }

        // ------------------------------------------------------------ bouton profil

        void Refresh()
        {
            var settings = AuthSettings.Get();
            string scene = SceneManager.GetActiveScene().name;
            bool show = AuthSession.IsLoggedIn && scene != settings.authScene && scene != settings.profileScene;
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
