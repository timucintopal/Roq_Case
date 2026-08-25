using UnityEngine;

namespace Case1_FitTheShape.Scripts
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        [Tooltip("Sırasıyla çalınacak sesler (0. index = 1. eşleşme sesi, 1. index = 2. eşleşme sesi vb.)")]
        [SerializeField] private AudioClip[] matchSounds;
        
        private AudioSource _audioSource;
        private int _matchCount = 0; // Kaçıncı eşleşmede olduğumuzu tutan sayaç

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            // Sisteme abone ol (Hiçbir oyun kodunu bozmadan sadece dinliyoruz)
            GameEvents.OnSegmentFilled += HandleMatch;
        }

        private void OnDisable()
        {
            GameEvents.OnSegmentFilled -= HandleMatch;
        }

        private void HandleMatch(SegmentController segment, ShapeType type)
        {
            if (matchSounds == null || matchSounds.Length == 0) return;

            // Sıradaki sesin indexini bul (Eğer 5 ses varsa ve 6. eşleşme olursa % operatörü sayesinde tekrar 0. sese döner)
            int soundIndex = _matchCount % matchSounds.Length;
            
            // Sesi çal
            _audioSource.PlayOneShot(matchSounds[soundIndex]);
            
            // Sayacı bir sonraki eşleşme için artır
            _matchCount++;
        }
    }
}
