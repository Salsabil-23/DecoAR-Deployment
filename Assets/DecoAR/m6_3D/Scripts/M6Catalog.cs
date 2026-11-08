using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DecoAR.Auth;
using UnityEngine;
using UnityEngine.Networking;

namespace DecoAR.M6
{
    // Catalogue de l'administration : les modeles 3D generes et valides par l'admin.
    // Stocke sur le serveur (decoar-api, MongoDB) : partage entre tous les telephones.
    //   GET /api/catalog            liste (tout compte connecte)
    //   GET /api/catalog/:id/model  fichier .glb (garde en cache dans persistentDataPath/m6_catalogue)
    //   POST / DELETE               espace admin seulement (en-tete X-Admin-Token)
    // A lancer avec StartCoroutine ; le resultat arrive dans le callback (erreur = null si tout va bien).
    public static class M6Catalog
    {
        [Serializable]
        public class Item
        {
            public string id;
            public string name;
            public string category;
            public long modelSize;
            public string createdAt; // date ISO renvoyee par le serveur
            public string Path => System.IO.Path.Combine(Folder, id + ".glb");
            public DateTime Created => DateTime.TryParse(createdAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d)
                ? d.ToLocalTime() : DateTime.MinValue;
        }

        [Serializable] class ListResponse { public List<Item> products = new List<Item>(); }
        [Serializable] class ItemResponse { public Item product; }
        [Serializable] class ErrorResponse { public string message; public string error; }

        // Memes categories que le serveur (Backend/src/models/Product.js)
        public static readonly string[] Categories = { "Salon", "Chambre", "Cuisine", "Décoration", "Luminaires", "Tableaux" };

        static string Folder => System.IO.Path.Combine(Application.persistentDataPath, "m6_catalogue");
        static string Api => AuthApi.BaseUrl.TrimEnd('/') + "/api/catalog";

        public static IEnumerator All(Action<List<Item>, string> done)
        {
            using var req = UnityWebRequest.Get(Api);
            AuthApi.Authorize(req);
            req.timeout = AuthApi.TimeoutSeconds;
            yield return req.SendWebRequest();
            if (Failed(req, out string error)) { done(new List<Item>(), error); yield break; }
            var list = JsonUtility.FromJson<ListResponse>(req.downloadHandler.text)?.products ?? new List<Item>();
            done(list, null);
        }

        /// Envoie le GLB au serveur et l'enregistre dans le catalogue (espace admin).
        public static IEnumerator Add(string sourceGlb, string name, string category, Action<Item, string> done)
        {
            byte[] data;
            try { data = File.ReadAllBytes(sourceGlb); }
            catch (Exception e) { done(null, "Modèle introuvable : " + e.Message); yield break; }

            string url = $"{Api}?name={UnityWebRequest.EscapeURL(name.Trim())}&category={UnityWebRequest.EscapeURL(category)}";
            using var req = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(data),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 120,
            };
            req.SetRequestHeader("Content-Type", "application/octet-stream");
            AuthApi.Authorize(req, admin: true);
            yield return req.SendWebRequest();
            if (Failed(req, out string error)) { done(null, error); yield break; }

            var item = JsonUtility.FromJson<ItemResponse>(req.downloadHandler.text)?.product;
            if (item != null) Cache(item, data); // deja sur le telephone : pas besoin de le retelecharger
            done(item, item == null ? "Réponse du serveur illisible" : null);
        }

        public static IEnumerator Remove(Item item, Action<string> done)
        {
            using var req = UnityWebRequest.Delete($"{Api}/{item.id}");
            req.downloadHandler = new DownloadHandlerBuffer();
            AuthApi.Authorize(req, admin: true);
            req.timeout = AuthApi.TimeoutSeconds;
            yield return req.SendWebRequest();
            if (Failed(req, out string error)) { done(error); yield break; }
            try { if (File.Exists(item.Path)) File.Delete(item.Path); } catch (Exception) { }
            done(null);
        }

        /// Chemin local du GLB (telecharge depuis le serveur la premiere fois).
        public static IEnumerator LoadModel(Item item, Action<string, string> done)
        {
            if (File.Exists(item.Path)) { done(item.Path, null); yield break; }
            using var req = UnityWebRequest.Get($"{Api}/{item.id}/model");
            AuthApi.Authorize(req);
            req.timeout = 120;
            yield return req.SendWebRequest();
            if (Failed(req, out string error)) { done(null, error); yield break; }
            try { Cache(item, req.downloadHandler.data); }
            catch (Exception e) { done(null, "Erreur fichier : " + e.Message); yield break; }
            done(item.Path, null);
        }

        static void Cache(Item item, byte[] data)
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(item.Path, data);
        }

        // Message lisible : celui du serveur ({ message } ou { error }), sinon l'erreur reseau
        static bool Failed(UnityWebRequest req, out string error)
        {
            error = null;
            if (req.result == UnityWebRequest.Result.Success) return false;
            if (req.result == UnityWebRequest.Result.ConnectionError) { error = "Serveur injoignable, vérifie ta connexion"; return true; }
            ErrorResponse body = null;
            try { body = JsonUtility.FromJson<ErrorResponse>(req.downloadHandler?.text ?? ""); } catch (Exception) { }
            error = !string.IsNullOrEmpty(body?.message) ? body.message : !string.IsNullOrEmpty(body?.error) ? body.error : $"Erreur {req.responseCode}";
            if (req.responseCode == 403) AuthSession.ClearAdminToken(); // espace admin expire : il faudra reconfirmer
            return true;
        }
    }
}
