using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace Case1_FitTheShape.Scripts
{
    public class MDrum : MonoBehaviour
    {
        [SerializeField] private List<SegmentController> segments = new List<SegmentController>();
        
        [SerializeField] private List<SegmentController> selectedSegments = new List<SegmentController>();

        private void Awake()
        {
            GatherSegments();
        }

        // Unity editöründe scriptin yanındaki 3 noktaya (...) basıp "Gather Segments" diyerek
        // manuel olarak da listeyi doldurabilirsiniz.
        [ContextMenu("Gather Segments")]
        public void GatherSegments()
        {
            // Tüm alt objelerdeki SegmentController'ları bulup listeye çeviriyoruz.
            segments = new List<SegmentController>(GetComponentsInChildren<SegmentController>());
        }

        public void PlayWaveEffect(SegmentController centerSegment, float maxDistance)
        {
            foreach (var segment in segments)
            {
                if (segment == centerSegment || segment == null) continue;

                // İki segment arasındaki fiziksel mesafeyi (kuş uçuşu) hesaplıyoruz
                float dist = Vector3.Distance(centerSegment.transform.position, segment.transform.position);
                
                // Eğerk, belirlenen dalga sınırları (maxDistance) içindeyse dalgaya dahil et
                if (dist <= maxDistance)
                {
                    // Mesafeye göre gecikme (delay) hesapla. Uzaktakiler dalgayı daha geç hissedecek.
                    float delay = dist * 0.04f; // Dalga yayılma hızı

                    // Minik bir büyüme/esneme dalgası
                    segment.transform.DOPunchScale(Vector3.one * 0.15f, 0.35f, 1, 1).SetDelay(delay);
                }
            }
        }
    }
}
