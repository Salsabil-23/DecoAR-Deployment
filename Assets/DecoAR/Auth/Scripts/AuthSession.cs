using System;
using UnityEngine;

namespace DecoAR.Auth
{
    // Utilisateur connecte, accessible par tous les modules (ex. M5 pour lier une commande au compte)
    public static class AuthSession
    {
        const string KeyToken = "DecoAR.Auth.Token";
        const string KeyUser = "DecoAR.Auth.User";

        public static string Token { get; private set; }
        public static UserDto User { get; private set; }
        public static bool IsLoggedIn => !string.IsNullOrEmpty(Token);

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
            PlayerPrefs.DeleteKey(KeyToken);
            PlayerPrefs.DeleteKey(KeyUser);
            PlayerPrefs.Save();
            Changed?.Invoke(null);
        }
    }
}
