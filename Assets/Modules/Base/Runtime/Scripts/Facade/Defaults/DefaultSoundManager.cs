using UnityEngine;

namespace Base
{
    public class DefaultSoundManager : MonoBehaviour, ISoundManager
    {
        private AudioSource bgmSource;
        private AudioSource sfxSource;

        private float bgmVolume = 1f;
        private float sfxVolume = 1f;
        private bool isMuted;

        public float BGMVolume
        {
            get => bgmVolume;
            set
            {
                bgmVolume = Mathf.Clamp01(value);
                if (bgmSource != null)
                    bgmSource.volume = isMuted ? 0f : bgmVolume;
            }
        }

        public float SFXVolume
        {
            get => sfxVolume;
            set => sfxVolume = Mathf.Clamp01(value);
        }

        public bool IsMuted
        {
            get => isMuted;
            set
            {
                isMuted = value;
                if (bgmSource != null)
                    bgmSource.volume = isMuted ? 0f : bgmVolume;
            }
        }

        private void Awake()
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        public void PlayBGM(string clipName)
        {
            var clip = Resources.Load<AudioClip>(clipName);
            if (clip == null)
            {
                Facade.Logger?.Log($"[SoundManager] BGM clip '{clipName}' not found", LogLevel.Warning);
                return;
            }

            bgmSource.clip = clip;
            bgmSource.volume = isMuted ? 0f : bgmVolume;
            bgmSource.Play();
        }

        public void StopBGM()
        {
            bgmSource.Stop();
        }

        public void PlaySFX(string clipName)
        {
            var clip = Resources.Load<AudioClip>(clipName);
            if (clip == null)
            {
                Facade.Logger?.Log($"[SoundManager] SFX clip '{clipName}' not found", LogLevel.Warning);
                return;
            }

            sfxSource.PlayOneShot(clip, isMuted ? 0f : sfxVolume);
        }
    }
}
