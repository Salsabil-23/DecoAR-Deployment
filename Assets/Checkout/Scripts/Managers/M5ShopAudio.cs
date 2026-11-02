using System.Collections;
using UnityEngine;

namespace DecoAR.M5
{
    /// Sons de la boutique : musique d'ambiance en boucle (désactivable, mémorisée) + effets de la caisse.
    /// Appels statiques sans dépendance de scène : M5ShopAudio.Item(), Register(), Whoosh(), Applause().
    public class M5ShopAudio : MonoBehaviour
    {
        public static M5ShopAudio Instance { get; private set; }

        public AudioClip music, item, register, whoosh, applause;
        [Range(0, 1)] public float musicVolume = 0.3f;
        [Range(0, 1)] public float sfxVolume = 0.9f;

        const string MusicKey = "M5_Musique";
        AudioSource musicSrc;
        readonly AudioSource[] pool = new AudioSource[5];
        int next;
        float duckUntil;

        public bool MusicOn
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set => PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
        }

        void Awake()
        {
            Instance = this;
            musicSrc = gameObject.AddComponent<AudioSource>();
            musicSrc.clip = music; musicSrc.loop = true; musicSrc.playOnAwake = false;
            musicSrc.spatialBlend = 0f; musicSrc.priority = 0; musicSrc.volume = 0f;
            if (music) musicSrc.Play();
            for (int i = 0; i < pool.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false; s.spatialBlend = 0f;
                pool[i] = s;
            }
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            // Fondu d'entrée, et la musique baisse pendant la caisse et les applaudissements
            float target = MusicOn ? musicVolume * (Time.time < duckUntil ? 0.35f : 1f) : 0f;
            musicSrc.volume = Mathf.MoveTowards(musicSrc.volume, target, Time.unscaledDeltaTime * 0.35f);
        }

        void Play(AudioClip clip, float volume, float delay, float pitchJitter, float duck)
        {
            if (!clip) return;
            if (duck > 0f) duckUntil = Mathf.Max(duckUntil, Time.time + delay + duck);
            if (delay > 0f) StartCoroutine(PlayLater(clip, volume, delay, pitchJitter));
            else Fire(clip, volume, pitchJitter);
        }

        IEnumerator PlayLater(AudioClip clip, float volume, float delay, float pitchJitter)
        {
            yield return new WaitForSeconds(delay);
            Fire(clip, volume, pitchJitter);
        }

        void Fire(AudioClip clip, float volume, float pitchJitter)
        {
            var s = pool[next]; next = (next + 1) % pool.Length;
            s.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            s.PlayOneShot(clip, volume * sfxVolume);
        }

        /// Article qui tombe dans le panier (calé sur la chute du panier 3D).
        public static void Item(float delay = 0.38f) { if (Instance) Instance.Play(Instance.item, 0.8f, delay, 0.08f, 0f); }
        public static void Register() { if (Instance) Instance.Play(Instance.register, 1f, 0f, 0f, 2.5f); }
        public static void Whoosh() { if (Instance) Instance.Play(Instance.whoosh, 0.8f, 0f, 0.05f, 0f); }
        /// La musique reste baissée pendant toute la durée des applaudissements.
        public static void Applause() { if (Instance && Instance.applause) Instance.Play(Instance.applause, 1f, 0f, 0f, Instance.applause.length); }
    }
}
