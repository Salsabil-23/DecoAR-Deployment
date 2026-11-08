using UnityEngine;

namespace DecoAR.Auth
{
    // Reglages du module Auth : Assets/DecoAR/Auth/Resources/DecoAR_AuthSettings.asset
    // (cree par le menu DecoAR > Auth). Un seul endroit pour l'adresse de l'API.
    public class AuthSettings : ScriptableObject
    {
        [Tooltip("Adresse de l'API sur telephone : URL Render (https://decoar-api.onrender.com) ou IP du PC")]
        public string deviceApiUrl = "http://localhost:3000";

        [Tooltip("Adresse de l'API dans l'editeur Unity")]
        public string editorApiUrl = "http://localhost:3000";

        [Tooltip("Verrouillage si l'app reste en arriere-plan plus longtemps (secondes)")]
        public float lockAfterSeconds = 300;

        public string authScene = "Auth";
        public string profileScene = "Profile";
        public string firstSceneAfterLogin = "M1_Personnalisation";   // apres connexion : creer son personnage

        static AuthSettings instance;

        public static AuthSettings Get()
        {
            if (instance == null) instance = Resources.Load<AuthSettings>("DecoAR_AuthSettings");
            if (instance == null) instance = CreateInstance<AuthSettings>();
            return instance;
        }

        public string ApiUrl => Application.isEditor ? editorApiUrl : deviceApiUrl;
    }
}
