using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace DecoAR.Auth
{
    [Serializable] public class UserDto
    {
        public string id;
        public string firstName;
        public string lastName;
        public string email;
        public string phone;
        public bool emailVerified;
        public string role; // "user" ou "admin" (donne avec npm run make-admin)
        public bool faceEnrolled;
        public string faceEnrolledAt;
        public bool hasPhoto;
        public string photoUpdatedAt;
    }

    [Serializable] public class AuthResponse { public string token; public UserDto user; }
    [Serializable] public class AdminResponse { public string adminToken; public int expiresIn; public UserDto user; }
    [Serializable] public class UserResponse { public UserDto user; }
    [Serializable] public class MessageResponse { public string message; }

    // Defi de mouvements : etapes tirees au hasard par le serveur + seuils de detection
    [Serializable] public class ChallengeResponse
    {
        public string challengeToken;
        public string[] steps;
        public float yawDelta, pitchDelta, centerMaxYaw;
    }
    [Serializable] public class PoseResponse { public bool ok; public float yaw, pitch; public string error; }
    // Preuve envoyee a la fin du defi : le jeton + une image (JPEG base64) par etape
    [Serializable] public class FaceProof { public string challengeToken; public string[] frames; }
    [Serializable] class ErrorResponse { public string message; public string field; }

    [Serializable] class RegisterRequest { public string firstName, lastName, email, phone, password; public FaceProof face; }
    [Serializable] class LoginRequest { public string email, password; }
    [Serializable] class EmailRequest { public string email; }
    [Serializable] class CodeRequest { public string code; }
    [Serializable] class ResetRequest { public string email, code, password; }
    [Serializable] class ProfileRequest { public string firstName, lastName, phone; }
    [Serializable] class PasswordRequest { public string currentPassword, newPassword; }
    [Serializable] class PhotoRequest { public string image; }
    [Serializable] class PurposeRequest { public string purpose; }
    [Serializable] class FaceLoginRequest { public string email; public FaceProof face; }
    [Serializable] class FaceRequest { public FaceProof face; }

    // Resultat d'un appel : soit Data, soit Error (+ Field = champ du formulaire en cause)
    public class ApiResult<T>
    {
        public T Data;
        public string Error;
        public string Field;
        public long Status;
        public bool Ok => Error == null;
        public bool IsNetworkError => Status == 0;
    }

    // Client HTTP du backend (Backend/ : Node + MongoDB). A lancer avec StartCoroutine.
    public static class AuthApi
    {
        // Regle dans Resources/DecoAR_AuthSettings (URL Render sur telephone, localhost dans l'editeur)
        public static string BaseUrl => AuthSettings.Get().ApiUrl;
        // Render gratuit "s'endort" : le premier appel peut prendre ~50 s le temps qu'il redemarre
        public static int TimeoutSeconds = 60;

        // ------------------------------------------------------------ compte

        public static IEnumerator Wake() => Send<MessageResponse>("GET", "/api/health", null, null, null);

        // face : resultat du defi de mouvements (FaceChallenge), verifie par OpenCV cote serveur
        public static IEnumerator Register(string firstName, string lastName, string email, string phone, string password, FaceProof face, Action<ApiResult<AuthResponse>> done) =>
            Send("POST", "/api/auth/register", new RegisterRequest
            {
                firstName = firstName, lastName = lastName, email = email, phone = phone, password = password, face = face,
            }, null, done);

        public static IEnumerator Login(string email, string password, Action<ApiResult<AuthResponse>> done) =>
            Send("POST", "/api/auth/login", new LoginRequest { email = email, password = password }, null, done);

        public static IEnumerator Me(string token, Action<ApiResult<UserResponse>> done) =>
            Send("GET", "/api/users/me", null, token, done);

        public static IEnumerator VerifyEmail(string token, string code, Action<ApiResult<UserResponse>> done) =>
            Send("POST", "/api/auth/verify-email", new CodeRequest { code = code }, token, done);

        public static IEnumerator ResendVerification(string token, Action<ApiResult<MessageResponse>> done) =>
            Send("POST", "/api/auth/verify-email/resend", new CodeRequest(), token, done);

        public static IEnumerator ForgotPassword(string email, Action<ApiResult<MessageResponse>> done) =>
            Send("POST", "/api/auth/forgot-password", new EmailRequest { email = email }, null, done);

        public static IEnumerator ResetPassword(string email, string code, string password, Action<ApiResult<AuthResponse>> done) =>
            Send("POST", "/api/auth/reset-password", new ResetRequest { email = email, code = code, password = password }, null, done);

        // ------------------------------------------------------------ profil

        public static IEnumerator UpdateProfile(string token, string firstName, string lastName, string phone, Action<ApiResult<UserResponse>> done) =>
            Send("PATCH", "/api/users/me", new ProfileRequest { firstName = firstName, lastName = lastName, phone = phone }, token, done);

        public static IEnumerator ChangePassword(string token, string currentPassword, string newPassword, Action<ApiResult<MessageResponse>> done) =>
            Send("POST", "/api/users/me/password", new PasswordRequest { currentPassword = currentPassword, newPassword = newPassword }, token, done);

        public static IEnumerator UploadPhoto(string token, byte[] jpeg, Action<ApiResult<UserResponse>> done) =>
            Send("PUT", "/api/users/me/photo", new PhotoRequest { image = Convert.ToBase64String(jpeg) }, token, done);

        // Telecharge la photo de profil (null si l'utilisateur n'en a pas)
        public static IEnumerator DownloadPhoto(string token, Action<Texture2D> done)
        {
            using var req = UnityWebRequestTexture.GetTexture(BaseUrl.TrimEnd('/') + "/api/users/me/photo");
            req.SetRequestHeader("Authorization", "Bearer " + token);
            req.timeout = TimeoutSeconds;
            yield return req.SendWebRequest();
            done?.Invoke(req.result == UnityWebRequest.Result.Success ? DownloadHandlerTexture.GetContent(req) : null);
        }

        // ------------------------------------------------------------ reconnaissance faciale (OpenCV)

        // purpose : "enroll" (inscription, 2 mouvements) ou "login" (connexion / deverrouillage, 1 mouvement)
        public static IEnumerator FaceChallenge(string purpose, Action<ApiResult<ChallengeResponse>> done) =>
            Send("POST", "/api/auth/face/challenge", new PurposeRequest { purpose = purpose }, null, done);

        // Orientation de la tete sur une image (retour en direct pendant le defi)
        public static IEnumerator FacePose(byte[] jpeg, Action<ApiResult<PoseResponse>> done) =>
            Send("POST", "/api/auth/face/pose", new PhotoRequest { image = Convert.ToBase64String(jpeg) }, null, done);

        // email facultatif : avec, on compare au visage de ce compte ; sans, on cherche parmi tous les comptes
        public static IEnumerator FaceLogin(string email, FaceProof face, Action<ApiResult<AuthResponse>> done) =>
            Send("POST", "/api/auth/face/login", new FaceLoginRequest { email = email ?? "", face = face }, null, done);

        // Espace admin : compte admin + defi de mouvements avec son visage -> jeton admin (30 min)
        public static IEnumerator AdminConfirm(string token, FaceProof face, Action<ApiResult<AdminResponse>> done) =>
            Send("POST", "/api/admin/confirm", new FaceRequest { face = face }, token, done);

        // En-tetes d'une requete faite par un autre module (catalogue, M6) : session, et jeton admin si demande
        public static void Authorize(UnityWebRequest req, bool admin = false)
        {
            if (AuthSession.IsLoggedIn) req.SetRequestHeader("Authorization", "Bearer " + AuthSession.Token);
            if (admin && AuthSession.HasAdminAccess) req.SetRequestHeader("X-Admin-Token", AuthSession.AdminToken);
        }

        public static IEnumerator UpdateFace(string token, FaceProof face, Action<ApiResult<UserResponse>> done) =>
            Send("PUT", "/api/users/me/face", new FaceRequest { face = face }, token, done);

        // ------------------------------------------------------------ envoi

        static UnityWebRequest Build(string method, string path, object body, string token)
        {
            var req = new UnityWebRequest(BaseUrl.TrimEnd('/') + path, method);
            if (body != null)
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
                req.SetRequestHeader("Content-Type", "application/json");
            }
            req.downloadHandler = new DownloadHandlerBuffer();
            if (!string.IsNullOrEmpty(token)) req.SetRequestHeader("Authorization", "Bearer " + token);
            req.timeout = TimeoutSeconds;
            return req;
        }

        static IEnumerator Send<T>(string method, string path, object body, string token, Action<ApiResult<T>> done)
        {
            var req = Build(method, path, body, token);
            yield return req.SendWebRequest();

            // Connexion reutilisee deja fermee ("Connection reset by peer") : on reessaie une fois
            if (req.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogWarning($"[Auth] {method} {path} : {req.error}, nouvel essai");
                req.Dispose();
                yield return new WaitForSecondsRealtime(0.3f);
                req = Build(method, path, body, token);
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.ConnectionError)
                    Debug.LogWarning($"[Auth] {method} {BaseUrl}{path} : {req.error}");
            }

            using var _ = req;
            var result = new ApiResult<T> { Status = req.responseCode };
            string json = req.downloadHandler.text;
            if (req.result == UnityWebRequest.Result.ConnectionError)
            {
                result.Status = 0;
                result.Error = "Serveur injoignable. Vérifie que le backend est lancé et l'adresse de l'API.";
            }
            else if (req.result != UnityWebRequest.Result.Success)
            {
                var err = TryParse<ErrorResponse>(json);
                result.Error = !string.IsNullOrEmpty(err?.message) ? err.message : $"Erreur {req.responseCode}";
                result.Field = err?.field;
            }
            else
            {
                result.Data = TryParse<T>(json);
                if (result.Data == null) result.Error = "Réponse du serveur illisible";
            }
            done?.Invoke(result);
        }

        static TResult TryParse<TResult>(string json)
        {
            if (string.IsNullOrEmpty(json)) return default;
            try { return JsonUtility.FromJson<TResult>(json); }
            catch { return default; }
        }
    }
}
