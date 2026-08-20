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
            float speed = 0.04f; // Dalganın yayılma hızı
            float waveAnimDuration = 0.2f; // Segmentin esneme süresi

            // Dalganın kenarlara çarpıp yansıma sürelerinin matematiği
            float outwardEdgeTime = maxDistance * speed; 
            float inwardStartTime = outwardEdgeTime + waveAnimDuration; 
            float inwardCenterTime = inwardStartTime + outwardEdgeTime;
            float secondOutwardStartTime = inwardCenterTime + waveAnimDuration;

            foreach (var segment in segments)
            {
                if (segment == null) continue;

                float dist = Vector3.Distance(centerSegment.transform.position, segment.transform.position);
                
                if (dist <= maxDistance)
                {
                    Sequence seq = DOTween.Sequence();

                    // 1. Dalga Gidişi (Merkezden dışa) - 3x Etki (0.3f büyüklük)
                    float t1 = dist * speed;
                    seq.Insert(t1, segment.transform.DOPunchScale(Vector3.one * 0.3f, waveAnimDuration, 1, 1));

                    // 2. Dalga Gelişi (Kenardan merkeze yansıma) - 2x Etki (0.2f büyüklük)
                    float t2 = inwardStartTime + ((maxDistance - dist) * speed);
                    seq.Insert(t2, segment.transform.DOPunchScale(Vector3.one * 0.2f, waveAnimDuration, 1, 1));

                    // 3. Son Gidiş (Merkezden dışa sönümlenme) - 1x Etki (0.1f büyüklük)
                    float t3 = secondOutwardStartTime + (dist * speed);
                    seq.Insert(t3, segment.transform.DOPunchScale(Vector3.one * 0.1f, waveAnimDuration, 1, 1));
                }
            }
        }
    }
}
