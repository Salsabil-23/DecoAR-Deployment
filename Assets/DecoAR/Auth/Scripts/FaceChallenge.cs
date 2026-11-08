using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DecoAR.Auth
{
    // Defi de mouvements (detection du vivant) : le serveur tire au sort des mouvements
    // ("regarde la camera", "tourne la tete a gauche"...). Pour chaque etape, l'app envoie une image
    // toutes les ~0,35 s a /face/pose ; des que le mouvement est detecte, l'image est gardee et on passe
    // a la suite. A la fin, Completed renvoie le jeton du defi + une image par etape : le serveur
    // revérifie tout (mouvements, meme personne) avant d'inscrire ou de connecter.
    public class FaceChallenge : MonoBehaviour
    {
        const float SendEvery = 0.35f;
        const float StepTimeout = 12f;
        const int FrameSize = 480;

        [SerializeField] FaceCamera faceCamera;
        [SerializeField] TMP_Text instruction;
        [SerializeField] TMP_Text stepLabel;
        [SerializeField] RectTransform arrow;
        [SerializeField] Image ring; // contour du cercle : vert quand l'etape est validee
        [SerializeField] Button startButton;
        [SerializeField] TMP_Text startLabel;
        [SerializeField] Button cancelButton;

        // "enroll" (inscription : 2 mouvements) ou "login" (connexion / deverrouillage : 1 mouvement)
        public string Purpose { get; set; } = "login";
        public string StartText { get; set; } = "Commencer";

        // Texte du lien d'annulation (ex. "Retour", "Utiliser mon mot de passe", "Annuler")
        public string CancelText
        {
            get => cancelButton.GetComponentInChildren<TMP_Text>()?.text ?? "";
            set { var label = cancelButton.GetComponentInChildren<TMP_Text>(); if (label != null) label.text = value; }
        }

        public event Action<FaceProof> Completed;
        public event Action Cancelled;

        static readonly Color32 RingIdle = new Color32(0xC8, 0x73, 0x4A, 0xFF);
        static readonly Color32 RingOk = new Color32(0x3E, 0x9B, 0x6A, 0xFF);

        bool running;

        void Awake()
        {
            startButton.onClick.AddListener(() => { if (!running) StartCoroutine(Run()); });
            cancelButton.onClick.AddListener(() => { StopAllCoroutines(); running = false; Cancelled?.Invoke(); });
        }

        void OnEnable() => Reset("Place ton visage dans le cercle, puis appuie sur « " + StartText + " »");

        void OnDisable()
        {
            StopAllCoroutines();
            running = false;
        }

        void Update()
        {
            if (!running) startButton.interactable = faceCamera.Ready;
            if (faceCamera.Error != null) instruction.text = faceCamera.Error;
        }

        // Le serveur a refuse le resultat (visage inconnu, mouvement douteux...) : on recommence
        public void Fail(string message) => Reset(message, error: true);

        // Message pendant l'envoi final ("Création du compte…")
        public void SetMessage(string message) => instruction.text = message;

        void Reset(string message, bool error = false)
        {
            running = false;
            startButton.gameObject.SetActive(true);
            startLabel.text = error ? "Recommencer" : StartText;
            instruction.text = error ? $"<color=#D64545>{message}</color>" : message;
            stepLabel.text = "";
            arrow.gameObject.SetActive(false);
            ring.color = RingIdle;
        }

        IEnumerator Run()
        {
            running = true;
            startButton.gameObject.SetActive(false);
            instruction.text = "Préparation du défi…";

            ApiResult<ChallengeResponse> challenge = null;
            yield return AuthApi.FaceChallenge(Purpose, r => challenge = r);
            if (!challenge.Ok) { Fail(challenge.Error); yield break; }

            var c = challenge.Data;
            var frames = new string[c.steps.Length];
            float baseYaw = 0, basePitch = 0;

            for (int i = 0; i < c.steps.Length; i++)
            {
                string step = c.steps[i];
                stepLabel.text = $"Étape {i + 1}/{c.steps.Length}";
                instruction.text = Instruction(step);
                ShowArrow(step);
                ring.color = RingIdle;

                float deadline = Time.realtimeSinceStartup + StepTimeout;
                bool done = false;
                while (!done)
                {
                    if (Time.realtimeSinceStartup > deadline)
                    {
                        Fail($"Mouvement non détecté à temps : {Instruction(step).ToLowerInvariant()}");
                        yield break;
                    }
                    yield return new WaitForSecondsRealtime(SendEvery);

                    byte[] jpg = faceCamera.GrabJpeg(FrameSize);
                    if (jpg == null) continue;
                    ApiResult<PoseResponse> pose = null;
                    yield return AuthApi.FacePose(jpg, r => pose = r);
                    if (!pose.Ok) { Fail(pose.Error); yield break; }
                    if (!pose.Data.ok)
                    {
                        instruction.text = $"{Instruction(step)}\n<size=75%><color=#D64545>{pose.Data.error}</color></size>";
                        continue;
                    }
                    instruction.text = Instruction(step);

                    var p = pose.Data;
                    if (i == 0) { baseYaw = p.yaw; basePitch = p.pitch; }
                    done = step switch
                    {
                        "left" => p.yaw - baseYaw <= -c.yawDelta,
                        "right" => p.yaw - baseYaw >= c.yawDelta,
                        "up" => p.pitch - basePitch <= -c.pitchDelta,
                        "down" => p.pitch - basePitch >= c.pitchDelta,
                        _ => Mathf.Abs(p.yaw) <= c.centerMaxYaw,
                    };
                    if (done) frames[i] = Convert.ToBase64String(jpg);
                }

                // Etape validee : contour vert un court instant
                ring.color = RingOk;
                arrow.gameObject.SetActive(false);
                instruction.text = "Parfait !";
                yield return new WaitForSecondsRealtime(0.35f);
            }

            stepLabel.text = "";
            instruction.text = "Vérification…";
            Completed?.Invoke(new FaceProof { challengeToken = c.challengeToken, frames = frames });
        }

        static string Instruction(string step) => step switch
        {
            "left" => "Tourne la tête à gauche",
            "right" => "Tourne la tête à droite",
            "up" => "Lève la tête",
            "down" => "Baisse la tête",
            _ => "Regarde la caméra, de face",
        };

        // La fleche pointe dans la direction du mouvement (le sprite pointe vers la droite)
        void ShowArrow(string step)
        {
            float angle = step switch { "left" => 180, "up" => 90, "down" => -90, "right" => 0, _ => float.NaN };
            arrow.gameObject.SetActive(!float.IsNaN(angle));
            if (!float.IsNaN(angle)) arrow.localEulerAngles = new Vector3(0, 0, angle);
        }
    }
}
