using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace DecoAR.M5
{
    /// Position GPS du téléphone → adresse postale (géocodage inverse OpenStreetMap / Nominatim).
    public class M5LocationService : MonoBehaviour
    {
        [Tooltip("Position utilisée dans l'éditeur (pas de GPS sur PC) : ESPRIT, Ariana.")]
        public Vector2 editorLatLon = new Vector2(36.8992f, 10.1894f);
        public float gpsTimeout = 20f;
        public string language = "fr";

        public bool Busy { get; private set; }

        [Serializable] class NominatimAddress
        {
            public string house_number, road, pedestrian, neighbourhood, suburb, village, town, city, municipality, county, state, postcode, country;
        }
        [Serializable] class NominatimResult { public string display_name; public NominatimAddress address; }

        /// onStatus : messages de progression, onDone : adresse (ou null + message d'erreur).
        public void FindAddress(Action<string> onStatus, Action<string, string> onDone)
        {
            if (Busy) return;
            StartCoroutine(Routine(onStatus, onDone));
        }

        IEnumerator Routine(Action<string> onStatus, Action<string, string> onDone)
        {
            Busy = true;
            double lat = 0, lon = 0;
            string error = null;

            if (Application.isEditor)
            {
                onStatus?.Invoke("Éditeur : position de test utilisée");
                lat = editorLatLon.x; lon = editorLatLon.y;
            }
            else
            {
#if UNITY_ANDROID
                if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                {
                    bool? granted = null;
                    var cb = new PermissionCallbacks();
                    cb.PermissionGranted += _ => granted = true;
                    cb.PermissionDenied += _ => granted = false;
                    Permission.RequestUserPermission(Permission.FineLocation, cb);
                    onStatus?.Invoke("Autorise l'accès à ta position…");
                    while (granted == null) yield return null;
                    if (granted == false) { Finish(onDone, null, "Accès à la position refusé."); yield break; }
                }
                if (!Input.location.isEnabledByUser) { Finish(onDone, null, "Activez la localisation (GPS) du téléphone."); yield break; }
#endif
                onStatus?.Invoke("Recherche de ta position…");
                Input.location.Start(10f, 5f);
                float t = 0;
                while (Input.location.status == LocationServiceStatus.Initializing && t < gpsTimeout) { t += Time.unscaledDeltaTime; yield return null; }
                if (Input.location.status != LocationServiceStatus.Running)
                {
                    error = Input.location.status == LocationServiceStatus.Failed ? "Accès à la position refusé ou GPS indisponible." : "Position introuvable, réessayez près d'une fenêtre.";
                    Input.location.Stop();
                    Finish(onDone, null, error); yield break;
                }
                var d = Input.location.lastData;
                lat = d.latitude; lon = d.longitude;
                Input.location.Stop();
            }

            onStatus?.Invoke("Recherche de l'adresse…");
            var ci = CultureInfo.InvariantCulture;
            string url = $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&addressdetails=1&zoom=18&accept-language={language}" +
                         $"&lat={lat.ToString("0.000000", ci)}&lon={lon.ToString("0.000000", ci)}";
            using (var req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("User-Agent", "DecoAR/1.0 (projet etudiant ESPRIT)");
                req.timeout = 10;
                yield return req.SendWebRequest();
                string address = null;
                if (req.result == UnityWebRequest.Result.Success)
                {
                    try { address = Format(JsonUtility.FromJson<NominatimResult>(req.downloadHandler.text)); }
                    catch (Exception e) { Debug.LogWarning("[M5] Réponse d'adresse illisible : " + e.Message); }
                }
                else Debug.LogWarning("[M5] Géocodage impossible : " + req.error);

                // Sans réseau on garde au moins les coordonnées GPS
                if (string.IsNullOrEmpty(address))
                    Finish(onDone, $"Position GPS : {lat.ToString("0.00000", ci)}, {lon.ToString("0.00000", ci)}", "Adresse exacte introuvable (pas de réseau ?) : coordonnées GPS ajoutées.");
                else Finish(onDone, address, null);
            }
        }

        void Finish(Action<string, string> onDone, string address, string error)
        {
            Busy = false;
            onDone?.Invoke(address, error);
        }

        static string Format(NominatimResult r)
        {
            if (r == null) return null;
            var a = r.address;
            if (a == null) return r.display_name;
            var parts = new List<string>();
            string street = First(a.road, a.pedestrian);
            if (!string.IsNullOrEmpty(street)) parts.Add(string.IsNullOrEmpty(a.house_number) ? street : $"{a.house_number} {street}");
            string area = First(a.neighbourhood, a.suburb);
            if (!string.IsNullOrEmpty(area)) parts.Add(area);
            string city = First(a.city, a.town, a.village, a.municipality, a.county, a.state);
            string cityLine = string.Join(" ", new[] { a.postcode, city }).Trim();
            if (!string.IsNullOrEmpty(cityLine)) parts.Add(cityLine);
            if (!string.IsNullOrEmpty(a.country)) parts.Add(a.country);
            return parts.Count > 0 ? string.Join(", ", parts) : r.display_name;
        }

        static string First(params string[] v)
        {
            foreach (var s in v) if (!string.IsNullOrEmpty(s)) return s;
            return null;
        }
    }
}
