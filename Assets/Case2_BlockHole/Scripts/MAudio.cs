using UnityEngine;

namespace Case2_BlockHole.Scripts
{
    [RequireComponent(typeof(AudioSource))]
    public class MAudio : MonoBehaviour
    {
        public static MAudio Instance;

        private AudioSource _audioSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                _audioSource = GetComponent<AudioSource>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip == null || _audioSource == null) return;
            
            _audioSource.pitch = 1f;
            _audioSource.PlayOneShot(clip, volume);
        }

        public void PlaySoundWithPitch(AudioClip clip, float pitch, float volume = 1f)
        {
            if (clip == null || _audioSource == null) return;
            
            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(clip, volume);
        }
    }
}
