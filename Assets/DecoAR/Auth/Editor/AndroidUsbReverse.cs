using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using Debug = UnityEngine.Debug;

namespace DecoAR.Auth.EditorTools
{
    // Telephone branche en USB : "localhost:3000" sur le telephone = backend du PC (adb reverse).
    // Unity relance adb a chaque Build And Run, ce qui efface le renvoi : on le refait toutes les 20 s
    // tant que la cible est Android et que l'API du telephone pointe sur localhost.
    [InitializeOnLoad]
    static class AndroidUsbReverse
    {
        const double IntervalSeconds = 20;
        static double next;

        static AndroidUsbReverse()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) return;
            if (EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + IntervalSeconds;
            Run(quiet: true);
        }

        [MenuItem("DecoAR/Auth/Relier le telephone en USB (adb reverse)")]
        static void RunFromMenu() => Run(quiet: false);

        static void Run(bool quiet)
        {
            if (!TryLocalPort(AuthSettings.Get().deviceApiUrl, out int port))
            {
                if (!quiet) Debug.Log("[Auth] Device Api Url ne pointe pas sur localhost : pas besoin du renvoi USB");
                return;
            }

            string adb = Path.Combine(AndroidExternalToolsSettings.sdkRootPath ?? "", "platform-tools", "adb.exe");
            if (!File.Exists(adb))
            {
                if (!quiet) Debug.LogWarning($"[Auth] adb introuvable : {adb}");
                return;
            }

            try
            {
                var process = Process.Start(new ProcessStartInfo(adb, $"reverse tcp:{port} tcp:{port}")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
                if (quiet || process == null) return; // pas d'attente : l'editeur ne doit pas se figer
                process.WaitForExit(5000);
                string error = process.StandardError.ReadToEnd().Trim();
                if (process.ExitCode == 0) Debug.Log($"[Auth] Telephone relie : localhost:{port} -> backend du PC");
                else Debug.LogWarning($"[Auth] adb reverse a echoue (telephone branche et debogage USB autorise ?) {error}");
            }
            catch (Exception e)
            {
                if (!quiet) Debug.LogWarning($"[Auth] adb reverse : {e.Message}");
            }
        }

        static bool TryLocalPort(string url, out int port)
        {
            port = 0;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            if (uri.Host != "localhost" && uri.Host != "127.0.0.1") return false;
            port = uri.Port;
            return true;
        }
    }
}
