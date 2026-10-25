using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace DecoAR.M5
{
    /// Paiement Stripe Checkout via la fonction Supabase « stripe-checkout » (la clé secrète Stripe reste sur Supabase).
    /// Ouvre la page de paiement Stripe DANS l'app (M5InAppBrowser) : elle se ferme seule au retour de Stripe,
    /// puis le paiement est vérifié auprès du serveur avant de lancer l'animation de la caisse.
    public class M5StripePayment : MonoBehaviour
    {
        [Tooltip("https://<projet>.supabase.co/functions/v1/stripe-checkout")]
        public string functionUrl = "";
        [Tooltip("Clé « anon public » du projet Supabase (Project Settings > API Keys). Publique : sans danger dans l'app.")]
        public string supabaseAnonKey = "";
        public float pollInterval = 2.5f;
        [Tooltip("Abandon automatique après ce délai (secondes).")]
        public float timeout = 900f;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(functionUrl);
        public bool Busy { get; private set; }

        [Serializable] class Item { public string name; public float price; public int quantity; }
        [Serializable] class CreateRequest { public Item[] items; public string address; }
        [Serializable] class CreateResponse { public string id, url, error; }
        [Serializable] class StatusResponse { public string status, payment_status, error; }

        string checkoutUrl, sessionId;
        bool cancelled, checkNow, closedByUser, webOpen;
        string DonePrefix => functionUrl.Trim().TrimEnd('/') + "?result="; // = success_url / cancel_url du serveur

        /// lines : (nom, prix TND, quantité). onDone(payé, message).
        public void Pay(IList<(string name, float price, int qty)> lines, string address, Action<string> onStatus, Action<bool, string> onDone)
        {
            if (Busy) return;
            StartCoroutine(Routine(lines, address, onStatus, onDone));
        }

        public void Cancel()
        {
            cancelled = true;
            if (webOpen) { M5InAppBrowser.Close(); webOpen = false; }
        }

        public void ReopenPage() { if (!string.IsNullOrEmpty(checkoutUrl)) OpenPage(); }

        void OpenPage()
        {
            closedByUser = false;
            webOpen = M5InAppBrowser.Supported;
            M5InAppBrowser.Open(checkoutUrl, DonePrefix, gameObject.name);
        }

        /// Appelé par le plugin natif : retour Stripe (…?result=success / cancel) ou "closed".
        public void OnWebViewResult(string result)
        {
            webOpen = false;
            if (result == null || !result.Contains("result=success")) closedByUser = true; // dernière vérification puis annulation
            checkNow = true;
        }

        // Retour dans l'app après la page Stripe : vérification immédiate
        void OnApplicationFocus(bool focus) { if (focus && Busy) checkNow = true; }

        IEnumerator Routine(IList<(string name, float price, int qty)> lines, string address, Action<string> onStatus, Action<bool, string> onDone)
        {
            Busy = true; cancelled = closedByUser = checkNow = false; checkoutUrl = sessionId = null;
            onStatus?.Invoke("Connexion à Stripe…");

            var items = new Item[lines.Count];
            for (int i = 0; i < lines.Count; i++) items[i] = new Item { name = lines[i].name, price = lines[i].price, quantity = lines[i].qty };
            string body = JsonUtility.ToJson(new CreateRequest { items = items, address = address });

            CreateResponse created = null; string error = null;
            using (var req = new UnityWebRequest(functionUrl.Trim(), "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                Auth(req);
                req.timeout = 20;
                yield return req.SendWebRequest();
                created = Parse<CreateResponse>(req, out error);
            }
            if (created == null || string.IsNullOrEmpty(created.url)) { Finish(onDone, false, "Paiement indisponible : " + (created?.error ?? error ?? "réponse vide")); yield break; }

            checkoutUrl = created.url; sessionId = created.id;
            OpenPage();
            onStatus?.Invoke(M5InAppBrowser.Supported ? "Paiement en cours…" : "Terminez le paiement dans la page Stripe, puis revenez dans l'application.");

            float elapsed = 0f;
            while (!cancelled && elapsed < timeout)
            {
                for (float w = 0; w < pollInterval && !checkNow && !cancelled; w += Time.unscaledDeltaTime) yield return null;
                elapsed += pollInterval; checkNow = false;
                if (cancelled) break;

                using (var req = UnityWebRequest.Get($"{functionUrl.Trim()}?session_id={UnityWebRequest.EscapeURL(sessionId)}"))
                {
                    Auth(req);
                    req.timeout = 15;
                    yield return req.SendWebRequest();
                    var s = Parse<StatusResponse>(req, out _);
                    if (s == null) { if (closedByUser) break; continue; } // réseau instable : on réessaie
                    if (s.payment_status == "paid")
                    {
                        // Payé : on ferme d'abord la page pour que le joueur voie l'animation
                        if (webOpen) { M5InAppBrowser.Close(); webOpen = false; yield return new WaitForSecondsRealtime(0.4f); }
                        Finish(onDone, true, "Paiement accepté"); yield break;
                    }
                    if (s.status == "expired") { Finish(onDone, false, "La session de paiement a expiré."); yield break; }
                    if (closedByUser) break;
                }
            }
            Finish(onDone, false, cancelled || closedByUser ? "Paiement annulé." : "Délai de paiement dépassé.");
        }

        void Auth(UnityWebRequest req)
        {
            if (string.IsNullOrWhiteSpace(supabaseAnonKey)) return;
            req.SetRequestHeader("apikey", supabaseAnonKey.Trim());
            req.SetRequestHeader("Authorization", "Bearer " + supabaseAnonKey.Trim());
        }

        static T Parse<T>(UnityWebRequest req, out string error) where T : class
        {
            error = null;
            string text = req.downloadHandler != null ? req.downloadHandler.text : null;
            T data = null;
            if (!string.IsNullOrEmpty(text)) { try { data = JsonUtility.FromJson<T>(text); } catch (Exception) { } }
            if (req.result != UnityWebRequest.Result.Success)
            {
                error = req.error;
                Debug.LogWarning($"[M5] Stripe/Supabase : {req.responseCode} {req.error} {text}");
            }
            return req.result == UnityWebRequest.Result.Success || data != null ? data : null;
        }

        void Finish(Action<bool, string> onDone, bool paid, string message)
        {
            if (webOpen) { M5InAppBrowser.Close(); webOpen = false; }
            Busy = false; checkoutUrl = null;
            onDone?.Invoke(paid, message);
        }
    }
}
