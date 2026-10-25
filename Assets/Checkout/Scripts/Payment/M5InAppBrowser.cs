using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace DecoAR.M5
{
    /// Page web plein écran dans l'application (plugins Plugins/Android/M5WebView.java et Plugins/iOS/M5WebView.mm).
    /// Dans l'éditeur : navigateur externe.
    public static class M5InAppBrowser
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void _M5WebView_Open(string url, string doneUrl, string gameObject);
        [DllImport("__Internal")] static extern void _M5WebView_Close();
#endif

        public static bool Supported => !Application.isEditor &&
            (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer);

        /// Ouvre url ; quand la page atteint doneUrlPrefix, la fenêtre se ferme et gameObject.OnWebViewResult(url) est appelé
        /// ("closed" si l'utilisateur ferme).
        public static void Open(string url, string doneUrlPrefix, string gameObject)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var c = new AndroidJavaClass("com.decoar.m5.M5WebView")) c.CallStatic("open", url, doneUrlPrefix, gameObject);
#elif UNITY_IOS && !UNITY_EDITOR
            _M5WebView_Open(url, doneUrlPrefix, gameObject);
#else
            Application.OpenURL(url);
#endif
        }

        public static void Close()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var c = new AndroidJavaClass("com.decoar.m5.M5WebView")) c.CallStatic("close");
#elif UNITY_IOS && !UNITY_EDITOR
            _M5WebView_Close();
#endif
        }
    }
}
