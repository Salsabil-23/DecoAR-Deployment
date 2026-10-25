using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DecoAR.M3
{
    // F3.7 : recherche vocale au micro.
    // iOS : framework Speech via Plugins/iOS/DecoARM3Speech.mm (reconnaissance en francais).
    // Autres plateformes / editeur : indisponible, la recherche texte (F3.9) prend le relais.
    // Le plugin natif rappelle ce composant par UnitySendMessage : le GameObject doit garder son nom.
    public class M3VoiceSearch : MonoBehaviour
    {
        [SerializeField] float maxListenSeconds = 6f;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void _DecoARM3_StartSpeech(string gameObjectName, string locale);
        [DllImport("__Internal")] static extern void _DecoARM3_StopSpeech();
#endif

        public static bool IsAvailable =>
#if UNITY_IOS && !UNITY_EDITOR
            true;
#else
            false;
#endif

        public bool IsListening { get; private set; }

        public event Action<string> Partial;     // texte compris en cours d'ecoute
        public event Action<string> Result;      // texte final
        public event Action<string> Failed;      // message d'erreur en francais
        public event Action<bool> ListeningChanged;

        float stopAt;
        string lastPartial = "";

        public void StartListening()
        {
            if (IsListening) return;
            if (!IsAvailable)
            {
                Failed?.Invoke("Recherche vocale indisponible sur cet appareil : utilisez la recherche texte.");
                return;
            }
            lastPartial = "";
            SetListening(true);
            stopAt = Time.unscaledTime + maxListenSeconds;
#if UNITY_IOS && !UNITY_EDITOR
            _DecoARM3_StartSpeech(gameObject.name, "fr-FR");
#endif
        }

        public void StopListening()
        {
            if (!IsListening) return;
#if UNITY_IOS && !UNITY_EDITOR
            _DecoARM3_StopSpeech();
#endif
            // Le natif renvoie le resultat final ; s'il ne vient pas, le dernier partiel fait foi
            Invoke(nameof(FinishWithPartial), 1.5f);
        }

        void Update()
        {
            if (IsListening && Time.unscaledTime > stopAt) StopListening();
        }

        void FinishWithPartial()
        {
            if (!IsListening) return;
            SetListening(false);
            if (!string.IsNullOrWhiteSpace(lastPartial)) Result?.Invoke(lastPartial);
            else Failed?.Invoke("Je n'ai rien compris, réessayez ou utilisez la recherche texte.");
        }

        void SetListening(bool on)
        {
            IsListening = on;
            ListeningChanged?.Invoke(on);
        }

        // ------------------------------------------------ rappels du plugin natif

        void OnNativeSpeechPartial(string text)
        {
            lastPartial = text ?? "";
            Partial?.Invoke(lastPartial);
        }

        void OnNativeSpeechResult(string text)
        {
            CancelInvoke(nameof(FinishWithPartial));
            SetListening(false);
            if (string.IsNullOrWhiteSpace(text)) text = lastPartial;
            if (string.IsNullOrWhiteSpace(text)) Failed?.Invoke("Je n'ai rien compris, réessayez ou utilisez la recherche texte.");
            else Result?.Invoke(text);
        }

        void OnNativeSpeechError(string code)
        {
            CancelInvoke(nameof(FinishWithPartial));
            SetListening(false);
            Failed?.Invoke(code == "denied"
                ? "Micro ou reconnaissance vocale refusés : autorisez-les dans Réglages, ou utilisez la recherche texte."
                : "La reconnaissance vocale a échoué, utilisez la recherche texte.");
        }
    }
}
