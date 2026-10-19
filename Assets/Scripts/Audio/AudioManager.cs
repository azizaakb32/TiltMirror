using System.Collections.Generic;
using UnityEngine;

namespace TiltMirror
{
    // Plays sound effects and the ambient music loop from Assets/Resources/TiltMirror/Audio.
    // Missing clips are skipped silently, so the game also runs without audio files.
    // Created automatically by GameController (AudioManager.Ensure()).
    public sealed class AudioManager : MonoBehaviour
    {
        const string AudioRoot = "TiltMirror/Audio/";
        const string MutedKey = "tm_muted";

        public static AudioManager Instance { get; private set; }

        [SerializeField, Range(0f, 1f)] float sfxVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.3f;

        AudioSource sfx;
        AudioSource music;
        bool muted;
        readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        public static AudioManager Ensure()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("Audio");
                go.AddComponent<AudioManager>();   // Awake() sets Instance
            }
            return Instance;
        }

        public bool Muted
        {
            get { return muted; }
            set
            {
                muted = value;
                PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
                ApplySettings();
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;

            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;

            AudioClip loop = Load("music_ambient_loop");
            if (loop != null)
            {
                music.clip = loop;
                music.Play();
            }
            ApplySettings();
        }

        void ApplySettings()
        {
            if (sfx == null) return;
            sfx.mute = muted;
            music.mute = muted;
            sfx.volume = sfxVolume;
            music.volume = musicVolume;
        }

        AudioClip Load(string clipName)
        {
            AudioClip clip;
            if (cache.TryGetValue(clipName, out clip)) return clip;
            clip = Resources.Load<AudioClip>(AudioRoot + clipName);
            cache[clipName] = clip;
            return clip;
        }

        public void Play(string clipName, float pitch = 1f)
        {
            AudioClip clip = Load(clipName);
            if (clip == null) return;
            sfx.pitch = pitch;
            sfx.PlayOneShot(clip);
        }
    }
}
