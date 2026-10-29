using System.Collections.Generic;
using UnityEngine;

namespace DonerMaster
{
    public enum Sfx { Slice, Pop, Paper, Coin, Ding, Bell, Buzz, Tick, Whoosh, Mmm, Grumble }

    /// <summary>
    /// Pooled one-shot player with per-shot pitch, plus two loops (sizzle + street ambience).
    /// To use recorded sounds later, assign clips to <see cref="Overrides"/> in the inspector.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        [System.Serializable]
        public class ClipOverride
        {
            public Sfx Id;
            public AudioClip Clip;
        }

        public List<ClipOverride> Overrides = new List<ClipOverride>();
        public AudioClip SizzleOverride;
        public AudioClip AmbienceOverride;

        public static bool Muted
        {
            get => AudioListener.volume < 0.01f;
            set => AudioListener.volume = value ? 0f : 1f;
        }

        readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();
        readonly List<AudioSource> _pool = new List<AudioSource>();
        AudioSource _sizzle;
        AudioSource _ambience;
        int _next;
        readonly Dictionary<Sfx, float> _lastPlayed = new Dictionary<Sfx, float>();

        void Awake()
        {
            I = this;
            _clips[Sfx.Slice] = ProceduralAudio.Slice();
            _clips[Sfx.Pop] = ProceduralAudio.Pop();
            _clips[Sfx.Paper] = ProceduralAudio.Paper();
            _clips[Sfx.Coin] = ProceduralAudio.Coin();
            _clips[Sfx.Ding] = ProceduralAudio.Ding();
            _clips[Sfx.Bell] = ProceduralAudio.Bell();
            _clips[Sfx.Buzz] = ProceduralAudio.Buzz();
            _clips[Sfx.Tick] = ProceduralAudio.Tick();
            _clips[Sfx.Whoosh] = ProceduralAudio.Whoosh();
            _clips[Sfx.Mmm] = ProceduralAudio.Mmm();
            _clips[Sfx.Grumble] = ProceduralAudio.Grumble();
            foreach (var o in Overrides)
                if (o.Clip != null) _clips[o.Id] = o.Clip;

            for (int i = 0; i < 12; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                _pool.Add(s);
            }

            _sizzle = gameObject.AddComponent<AudioSource>();
            _sizzle.clip = SizzleOverride != null ? SizzleOverride : ProceduralAudio.Sizzle();
            _sizzle.loop = true;
            _sizzle.volume = 0f;
            _sizzle.Play();

            _ambience = gameObject.AddComponent<AudioSource>();
            _ambience.clip = AmbienceOverride != null ? AmbienceOverride : ProceduralAudio.StreetAmbience();
            _ambience.loop = true;
            _ambience.volume = 0.25f;
            _ambience.Play();
        }

        /// <summary>Play a sound. Small random pitch variation keeps repeats from sounding robotic.</summary>
        public static void Play(Sfx id, float volume = 1f, float pitch = 1f, float randomPitch = 0.06f, float minInterval = 0.02f)
        {
            if (I == null) return;
            I.PlayInternal(id, volume, pitch, randomPitch, minInterval);
        }

        void PlayInternal(Sfx id, float volume, float pitch, float randomPitch, float minInterval)
        {
            if (!_clips.TryGetValue(id, out var clip) || clip == null) return;
            if (_lastPlayed.TryGetValue(id, out var last) && Time.unscaledTime - last < minInterval) return;
            _lastPlayed[id] = Time.unscaledTime;

            var src = _pool[_next];
            _next = (_next + 1) % _pool.Count;
            src.clip = clip;
            src.volume = volume;
            src.pitch = pitch * (1f + Random.Range(-randomPitch, randomPitch));
            src.Play();
        }

        /// <summary>Semitone helper for rising combo notes.</summary>
        public static float Semitones(float n) => Mathf.Pow(2f, n / 12f);

        public static void SetSizzle(float volume, float pitch)
        {
            if (I == null || I._sizzle == null) return;
            I._sizzle.volume = Mathf.MoveTowards(I._sizzle.volume, volume, Time.deltaTime * 1.5f);
            I._sizzle.pitch = Mathf.MoveTowards(I._sizzle.pitch, pitch, Time.deltaTime);
        }
    }
}
