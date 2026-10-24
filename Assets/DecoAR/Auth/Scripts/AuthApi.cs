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
        public bool faceIdEnabled;
    }

    [Serializable] public class AuthResponse { public string token; public UserDto user; }
    [Serializable] public class EnrollResponse { public string biometricToken; public UserDto user; }
    [Serializable] class ErrorResponse { public string message; public string field; }

    [Serializable] class RegisterRequest { public string firstName, lastName, email, phone, password; }
    [Serializable] class LoginRequest { public string email, password; }
    [Serializable] class EnrollRequest { public string deviceId, deviceName; }
    [Serializable] class BiometricLoginRequest { public string email, deviceId, biometricToken; }

    // Resultat d'un appel : soit Data, soit Error (+ Field = champ du formulaire en cause)
    public class ApiResult<T>
    {
        public T Data;
        public string Error;
        public string Field;
        public long Status;
        public bool Ok => Error == null;
    }

    // Client HTTP du backend (Backend/ : Node + MongoDB). A lancer avec StartCoroutine.
    public static class AuthApi
    {
        // Editeur : localhost. Telephone : IP du PC sur le meme Wi-Fi, ex. http://192.168.1.20:3000
        public static string BaseUrl = "http://localhost:3000";
        public static int TimeoutSeconds = 10;

        public static IEnumerator Register(string firstName, string lastName, string email, string phone, string password, Action<ApiResult<AuthResponse>> done) =>
            Send("POST", "/api/auth/register", new RegisterRequest { firstName = firstName, lastName = lastName, email = email, phone = phone, password = password }, null, done);

        public static IEnumerator Login(string email, string password, Action<ApiResult<AuthResponse>> done) =>
            Send("POST", "/api/auth/login", new LoginRequest { email = email, password = password }, null, done);

        public static IEnumerator EnrollBiometric(string token, string deviceId, string deviceName, Action<ApiResult<EnrollResponse>> done) =>
            Send("POST", "/api/auth/biometric/enroll", new EnrollRequest { deviceId = deviceId, deviceName = deviceName }, token, done);

        public static IEnumerator BiometricLogin(string email, string deviceId, string biometricToken, Action<ApiResult<AuthResponse>> done) =>
            Send("POST", "/api/auth/biometric/login", new BiometricLoginRequest { email = email, deviceId = deviceId, biometricToken = biometricToken }, null, done);

        static IEnumerator Send<T>(string method, string path, object body, string token, Action<ApiResult<T>> done)
        {
            using var req = new UnityWebRequest(BaseUrl.TrimEnd('/') + path, method);
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(token)) req.SetRequestHeader("Authorization", "Bearer " + token);
            req.timeout = TimeoutSeconds;

            yield return req.SendWebRequest();

            var result = new ApiResult<T> { Status = req.responseCode };
            string json = req.downloadHandler.text;
            if (req.result == UnityWebRequest.Result.ConnectionError)
            {
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
