using UnityEngine;
using System.Collections.Generic;

namespace Case4_Buca.Scripts
{
    public class MAudio : MonoBehaviour
    {
        public static MAudio Instance { get; private set; }

        [Header("Audio Settings")]
        [Tooltip("Aynı anda çalabilecek maksimum ses sayısı (Pooling)")]
        public int poolSize = 10;
        
        [Tooltip("Seslerin makine tüfeği gibi aynı tonda çıkmasını önlemek için rastgele pitch aralığı.")]
        public Vector2 pitchRandomRange = new Vector2(0.9f, 1.1f);

        private List<AudioSource> audioPool;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializePool();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializePool()
        {
            audioPool = new List<AudioSource>();
            GameObject poolContainer = new GameObject("AudioPool");
            poolContainer.transform.SetParent(this.transform);

            for (int i = 0; i < poolSize; i++)
            {
                GameObject go = new GameObject($"AudioSource_{i}");
                go.transform.SetParent(poolContainer.transform);
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f; // 3D Sound
                source.minDistance = 2f;
                source.maxDistance = 20f;
                audioPool.Add(source);
            }
        }

        public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;

            // Havuzda boşta olan (çalmayan) bir AudioSource bul
            AudioSource availableSource = GetAvailableSource();
            
            if (availableSource != null)
            {
                availableSource.transform.position = position;
                availableSource.clip = clip;
                availableSource.volume = volume;
                availableSource.pitch = Random.Range(pitchRandomRange.x, pitchRandomRange.y);
                availableSource.Play();
            }
        }

        private AudioSource GetAvailableSource()
        {
            foreach (var source in audioPool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }
            return null; // Havuz tamamen doluysa ve hepsi çalıyorsa atla (Performance over fidelity)
        }
    }
}
