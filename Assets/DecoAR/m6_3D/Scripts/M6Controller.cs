
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class M6Controller : MonoBehaviour
{
    [Header("Backend M6 (Render : decoar-m6, ou IP du PC en local)")]
    public string serverUrl = "https://decoar-m6.onrender.com";

    [Header("Interface")]
    public RawImage photoPreview;
    public TMP_Text statusText;
    public Button generateButton;
    public M6ModelViewer modelViewer;

    private byte[] selectedImage;
    private bool busy;

    private void Start()
    {
        SetStatus("Choisis une photo pour commencer");

        if (photoPreview != null)
            photoPreview.gameObject.SetActive(false);

        // Render (offre gratuite) endort le serveur : on le réveille dès l'ouverture de l'écran
        StartCoroutine(WakeServer(false));
    }

    bool awake;

    /// GET /health jusqu'à ce que le serveur réponde (réveil Render : jusqu'à ~1 min).
    private IEnumerator WakeServer(bool showStatus)
    {
        if (awake) yield break;
        float deadline = Time.realtimeSinceStartup + 90f;
        while (Time.realtimeSinceStartup < deadline)
        {
            using (UnityWebRequest ping = UnityWebRequest.Get(serverUrl.TrimEnd('/') + "/health"))
            {
                ping.timeout = 60;
                yield return ping.SendWebRequest();
                if (ping.result == UnityWebRequest.Result.Success) { awake = true; yield break; }
            }
            if (showStatus) SetStatus("Réveil du serveur… (jusqu'à 1 min)");
            yield return new WaitForSecondsRealtime(3f);
        }
    }

    public void OpenGallery()
    {
#if UNITY_ANDROID || UNITY_IOS
        NativeGallery.GetImageFromGallery(
            path =>
            {
                if (!string.IsNullOrEmpty(path))
                    SelectImage(path);
            },
            "Choisir une photo",
            "image/*"
        );
#else
        SetStatus("Galerie disponible sur téléphone.");
#endif
    }

    public void TakePhoto()
    {
#if UNITY_ANDROID || UNITY_IOS
        NativeCamera.TakePicture(
            path =>
            {
                if (!string.IsNullOrEmpty(path))
                    SelectImage(path);
            },
            2048
        );
#else
        SetStatus("Caméra disponible sur téléphone.");
#endif
    }

    private void SelectImage(string path)
    {
        try
        {
            Texture2D source =
                NativeGallery.LoadImageAtPath(
                    path,
                    1600,
                    false
                );

            if (source == null)
                throw new Exception("Impossible de lire la photo");

            selectedImage = source.EncodeToJPG(85);

            if (photoPreview != null)
            {
                photoPreview.texture = source;
                photoPreview.gameObject.SetActive(true);
            }

            SetStatus("Photo prête pour génération");
        }
        catch (Exception ex)
        {
            SetStatus("Erreur photo : " + ex.Message);
        }
    }

    public void Generate3D()
    {
        if (busy)
            return;

        if (selectedImage == null || selectedImage.Length == 0)
        {
            SetStatus("Choisis d'abord une photo");
            return;
        }

        StartCoroutine(GenerateRoutine());
    }

    public void TestGLB()
    {
        if (!busy)
        {
            StartCoroutine(
                DownloadModel(
                    serverUrl.TrimEnd('/') + "/test-model"
                )
            );
        }
    }

    private IEnumerator GenerateRoutine()
    {
        busy = true;

        if (generateButton != null)
            generateButton.interactable = false;

        if (!awake)
        {
            SetStatus("Réveil du serveur… (jusqu'à 1 min)");
            yield return WakeServer(true);
            if (!awake)
            {
                SetStatus("Le serveur ne répond pas. Réessaie dans un instant.");
                EndBusy();
                yield break;
            }
        }

        SetStatus("Envoi de la photo…");

        WWWForm form = new WWWForm();

        form.AddBinaryData(
            "image",
            selectedImage,
            "photo.jpg",
            "image/jpeg"
        );

        using (UnityWebRequest request =
            UnityWebRequest.Post(
                serverUrl.TrimEnd('/') + "/generate",
                form
            ))
        {
            request.timeout = 120;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                SetStatus("Erreur envoi : " + request.error);
                EndBusy();
                yield break;
            }

            JobResponse response =
                JsonUtility.FromJson<JobResponse>(
                    request.downloadHandler.text
                );

            if (response == null ||
                string.IsNullOrEmpty(response.job_id))
            {
                SetStatus("Réponse serveur invalide");
                EndBusy();
                yield break;
            }

            string jobId = response.job_id;

            SetStatus("Génération 3D en cours… (environ 1 min)");

            float deadline =
                Time.realtimeSinceStartup + 900f;
            int failures = 0;

            while (Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForSecondsRealtime(5f);

                using (UnityWebRequest check =
                    UnityWebRequest.Get(
                        serverUrl.TrimEnd('/') +
                        "/status/" + jobId
                    ))
                {
                    check.timeout = 30;

                    yield return check.SendWebRequest();

                    if (check.result !=
                        UnityWebRequest.Result.Success)
                    {
                        // un raté réseau isolé ne doit pas arrêter une génération en cours
                        if (++failures < 3) continue;
                        SetStatus("Pas de connexion. Vérifie ton réseau puis réessaie.");
                        EndBusy();
                        yield break;
                    }
                    failures = 0;

                    JobResponse state =
                        JsonUtility.FromJson<JobResponse>(
                            check.downloadHandler.text
                        );

                    if (state.status == "failed")
                    {
                        SetStatus("Échec : " + state.error);
                        EndBusy();
                        yield break;
                    }

                    if (state.status == "completed")
                    {
                        yield return DownloadModel(
                            serverUrl.TrimEnd('/') +
                            "/model/" + jobId
                        );

                        EndBusy();
                        yield break;
                    }
                }
            }

            SetStatus("Le serveur ne répond pas. Réessaie dans un instant.");
        }

        EndBusy();
    }

    private IEnumerator DownloadModel(string url)
    {
        SetStatus("Téléchargement du modèle…");

        using (UnityWebRequest request =
            UnityWebRequest.Get(url))
        {
            request.timeout = 120;

            yield return request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                SetStatus("Erreur GLB : " + request.error);
                yield break;
            }

            string path = Path.Combine(
                Application.persistentDataPath,
                "m6_current.glb"
            );

            try
            {
                File.WriteAllBytes(
                    path,
                    request.downloadHandler.data
                );
            }
            catch (Exception ex)
            {
                SetStatus("Erreur fichier : " + ex.Message);
                yield break;
            }

            SetStatus("Affichage du modèle 3D…");

            if (modelViewer == null)
            {
                SetStatus("M6ModelViewer non assigné");
                yield break;
            }

            Task<bool> task = modelViewer.LoadModel(path);

            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
            {
                SetStatus(
                    "Erreur import : " +
                    task.Exception?.GetBaseException().Message
                );
            }
            else
            {
                SetStatus(
                    task.Result
                    ? "Modèle 3D affiché !"
                    : "Échec affichage GLB"
                );
            }
        }
    }

    private void EndBusy()
    {
        busy = false;

        if (generateButton != null)
            generateButton.interactable = true;
    }

    private void SetStatus(string message)
    {
        Debug.Log("[M6] " + message);

        if (statusText != null)
            statusText.text = message;
    }

    [Serializable]
    private class JobResponse
    {
        public string job_id;
        public string status;
        public string error;
    }
}
