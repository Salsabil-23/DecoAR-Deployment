using System;
using UnityEngine;

namespace DecoAR.Auth
{
    // Utilisateur connecte, accessible par tous les modules (ex. M5 pour lier une commande au compte)
    public static class AuthSession
    {
        const string KeyToken = "DecoAR.Auth.Token";
        const string KeyUser = "DecoAR.Auth.User";
        const string KeyLastEmail = "DecoAR.Auth.LastEmail";

        public static string Token { get; private set; }
        public static UserDto User { get; private set; }
        public static bool IsLoggedIn => !string.IsNullOrEmpty(Token);
        // Dernier compte utilise sur ce telephone (prerempli pour la connexion par le visage)
        public static string LastEmail => PlayerPrefs.GetString(KeyLastEmail, "");

        // Compte administrateur (role donne cote serveur) ; l'espace admin s'ouvre en plus avec le visage
        public static bool IsAdmin => IsLoggedIn && User?.role == "admin";
        // Jeton admin obtenu apres la confirmation par le visage : en memoire seulement, 30 min
        public static string AdminToken { get; private set; }
        static float adminTokenUntil;
        public static bool HasAdminAccess => IsAdmin && !string.IsNullOrEmpty(AdminToken) && Time.realtimeSinceStartup < adminTokenUntil;

        public static void SetAdminToken(string token, int expiresInSeconds)
        {
            AdminToken = token;
            adminTokenUntil = Time.realtimeSinceStartup + Mathf.Max(0, expiresInSeconds - 30); // marge avant l'expiration serveur
        }

        public static void ClearAdminToken() => AdminToken = null;

        public static event Action<UserDto> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Load()
        {
            Token = PlayerPrefs.GetString(KeyToken, null);
            string json = PlayerPrefs.GetString(KeyUser, null);
            User = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<UserDto>(json);
            if (User == null) Token = null;
        }

        public static void Set(string token, UserDto user)
        {
            Token = token;
            User = user;
            PlayerPrefs.SetString(KeyToken, token);
            PlayerPrefs.SetString(KeyUser, JsonUtility.ToJson(user));
            PlayerPrefs.SetString(KeyLastEmail, user.email);
            PlayerPrefs.Save();
            Changed?.Invoke(user);
        }

        public static void UpdateUser(UserDto user)
        {
            if (user == null || !IsLoggedIn) return;
            Set(Token, user);
        }

        public static void Logout()
        {
            Token = null;
            User = null;
            AdminToken = null;
            PlayerPrefs.DeleteKey(KeyToken);
            PlayerPrefs.DeleteKey(KeyUser);
            PlayerPrefs.Save();
            Changed?.Invoke(null);
        }
    }
}
