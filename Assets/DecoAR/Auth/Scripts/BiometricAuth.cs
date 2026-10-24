using System;
using UnityEngine;

namespace DecoAR.Auth
{
    // Point d'entree du capteur (Face ID / empreinte). Le visage est verifie par le telephone :
    // on ne recoit que "reussi / echoue", rien n'est envoye au serveur a part le jeton d'appareil.
    public interface IBiometricAuthenticator
    {
        bool IsAvailable { get; }
        string Label { get; } // "Face ID", "Touch ID", "Empreinte"...

        // done(true, null) si l'utilisateur est reconnu, sinon done(false, message).
        // A appeler sur le thread principal (les callbacks natifs arrivent souvent sur un autre thread).
        void Authenticate(string reason, Action<bool, string> done);
    }

    public static class BiometricAuth
    {
        // A remplacer au demarrage par l'implementation native, ex. :
        //   iOS     : LocalAuthentication (LAContext.evaluatePolicy) via un plugin .mm
        //   Android : androidx.biometric.BiometricPrompt via un plugin .aar / AndroidJavaObject
        public static IBiometricAuthenticator Current { get; set; } = CreateDefault();

        public static string DeviceId => SystemInfo.deviceUniqueIdentifier;
        public static string DeviceName => SystemInfo.deviceModel;

        static IBiometricAuthenticator CreateDefault()
        {
#if UNITY_EDITOR
            return new EditorBiometricSimulator();
#else
            return new UnavailableBiometric();
#endif
        }
    }

    // Editeur : simule un visage reconnu pour tester tout le parcours sans telephone
    public class EditorBiometricSimulator : IBiometricAuthenticator
    {
        public static bool NextResult = true;

        public bool IsAvailable => true;
        public string Label => "Face ID";

        public void Authenticate(string reason, Action<bool, string> done)
        {
            Debug.Log($"[Auth] Face ID simule ({(NextResult ? "reconnu" : "refuse")}) : {reason}");
            done(NextResult, NextResult ? null : "Visage non reconnu");
        }
    }

    // Tant que le plugin natif n'est pas branche, le bouton Face ID reste masque sur telephone
    public class UnavailableBiometric : IBiometricAuthenticator
    {
        public bool IsAvailable => false;
        public string Label => "Face ID";
        public void Authenticate(string reason, Action<bool, string> done) => done(false, "Face ID indisponible sur cet appareil");
    }

    // Jeton d'appareil renvoye par /api/auth/biometric/enroll.
    // TODO capteur : sur telephone, le ranger dans le Keychain (iOS, kSecAccessControlBiometryCurrentSet)
    // ou le Keystore (Android) plutot que dans PlayerPrefs.
    public static class BiometricVault
    {
        const string KeyEmail = "DecoAR.Auth.BiometricEmail";
        const string KeyToken = "DecoAR.Auth.BiometricToken";

        public static string Email => PlayerPrefs.GetString(KeyEmail, "");
        public static bool HasToken => !string.IsNullOrEmpty(PlayerPrefs.GetString(KeyToken, ""));

        public static void Save(string email, string token)
        {
            PlayerPrefs.SetString(KeyEmail, email);
            PlayerPrefs.SetString(KeyToken, token);
            PlayerPrefs.Save();
        }

        public static string Token => PlayerPrefs.GetString(KeyToken, "");

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(KeyEmail);
            PlayerPrefs.DeleteKey(KeyToken);
            PlayerPrefs.Save();
        }
    }
}
