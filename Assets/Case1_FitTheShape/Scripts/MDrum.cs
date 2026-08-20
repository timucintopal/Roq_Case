using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;

using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;
using NUnit.Framework;

namespace Case1_FitTheShape.Scripts
{
    // Inspector'da 2 boyutlu (Sütun ve Satır) yapı oluşturabilmek için bir sınıf
    [System.Serializable]
    public class DrumColumn
    {
        [Tooltip("Bu sütuna ait segmentleri sırayla (örneğin yukarıdan aşağıya doğru) atayın.")]
        public List<SegmentController> rowSegments = new List<SegmentController>();
    }

    public class MDrum : MonoBehaviour
    {
        public static MDrum Instance { get; private set; }

        [Tooltip("5x15 matris için buraya 5 adet eleman (sütun) ekleyip, her birine o sütunun 15 segmentini atayın.")]
        [SerializeField] private List<DrumColumn> columns = new List<DrumColumn>();

        [Header("Active State")]
        [Tooltip("O an kameraya dönük (oynanabilir) olan segmentleri tutan liste. Davul döndükçe bunu güncelleyin.")]
        public List<SegmentController> activeSegments = new List<SegmentController>();

        [Header("Wave Settings")]
        [Tooltip("Dalganın merkezden dışarıya doğru toplam kaç segmente ulaşacağını belirler. (Dairesel etki alanı)")]
        [SerializeField] private int waveReachCount = 15;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // Aktif segmentler arasından tipi eşleşen, içi boş olan ve fırlatılan objeye YATAYDA (X ekseninde) EN YAKIN hedefi döndürür
        public SegmentController GetMatchingActiveSegment(ShapeType type)
        {
            SegmentController bestMatch = null;

            foreach (var segment in activeSegments)
            {
                if (segment != null && !segment.IsFilled && segment.SegmentCheck(type))
                {
                    return segment;
                }
            }
            return bestMatch; // Eşleşen hedef yoksa null döner
        }

        // Grid (2D) ve Silindir (Wrap) matematiği ile kusursuz dairesel mesafe hesaplama
        private float CalculateGridDistance(SegmentController center, SegmentController target)
        {
            int cX = -1, cY = -1;
            int tX = -1, tY = -1;

            // Her iki objenin de (Center ve Target) Sütun (X) ve Satır (Y) koordinatlarını buluyoruz
            for (int i = 0; i < columns.Count; i++)
            {
                int cIndex = columns[i].rowSegments.IndexOf(center);
                if (cIndex != -1) { cX = i; cY = cIndex; }

                int tIndex = columns[i].rowSegments.IndexOf(target);
                if (tIndex != -1) { tX = i; tY = tIndex; }
            }

            // Objelerden biri listelerde yoksa (atanmamışsa) çok uzak say
            if (cX == -1 || tX == -1) return 999f; 

            int numCols = columns.Count;
            // İlk kolonun eleman sayısını silindirin çevresi (satır sayısı) kabul ediyoruz
            int numRows = columns[0].rowSegments.Count; 

            // X ekseninde normal mesafe (Sütunlar arası)
            float diffX = Mathf.Abs(cX - tX);
            
            // Y ekseninde silindir etrafında döndüğü için "başa sarma (wrap-around)" mesafesi
            // Örneğin 0. satır ile 14. satır birbirine komşudur.
            float diffY = Mathf.Min(Mathf.Abs(cY - tY), numRows - Mathf.Abs(cY - tY));

            // Pisagor teoremi ile 2D Grid üzerindeki dairesel kuş uçuşu mesafesi
            return Mathf.Sqrt(diffX * diffX + diffY * diffY);
        }

        public void PlayWaveEffect(SegmentController centerSegment)
        {
            float speed = 0.05f; // Grid mesafesi baz alındığı için dalga gecikme çarpanı
            float waveAnimDuration = 0.2f; // Segmentin esneme süresi

            // Tüm kolonlardaki tüm segmentleri tek bir düz listeye (flat list) çevir
            var allSegments = columns.SelectMany(c => c.rowSegments).ToList();

            // Segmentleri artık fiziksel 3D mesafeye göre değil, Sizin Inspector'dan atadığınız
            // Sanal 2D Silindir Grid mesafesine göre sıralıyoruz!
            var orderedSegments = allSegments
                .Where(s => s != null)
                .Select(s => new { Seg = s, Dist = CalculateGridDistance(centerSegment, s) })
                .OrderBy(x => x.Dist)
                .Take(waveReachCount)
                .ToList();

            if (orderedSegments.Count == 0) return;

            // Etki alanındaki en uzak segmentin mesafesi, dalganın geri sekme sınırıdır (edge).
            float actualMaxDistance = orderedSegments[orderedSegments.Count - 1].Dist;

            // Dalganın kenarlara çarpıp yansıma sürelerinin matematiği
            float outwardEdgeTime = actualMaxDistance * speed; 
            float inwardStartTime = outwardEdgeTime + waveAnimDuration; 
            float inwardCenterTime = inwardStartTime + outwardEdgeTime;
            float secondOutwardStartTime = inwardCenterTime + waveAnimDuration;

            foreach (var item in orderedSegments)
            {
                float dist = item.Dist;
                SegmentController segment = item.Seg;

                Sequence seq = DOTween.Sequence();

                // 1. Dalga Gidişi (Merkezden dışa) - 3x Etki 
                seq.Insert(dist * speed, segment.transform.DOPunchScale(Vector3.one * 0.3f, waveAnimDuration, 1, 1));

                // 2. Dalga Gelişi (Kenardan merkeze yansıma) - 2x Etki 
                seq.Insert(inwardStartTime + ((actualMaxDistance - dist) * speed), segment.transform.DOPunchScale(Vector3.one * 0.2f, waveAnimDuration, 1, 1));

                // 3. Son Gidiş (Merkezden dışa sönümlenme) - 1x Etki 
                seq.Insert(secondOutwardStartTime + (dist * speed), segment.transform.DOPunchScale(Vector3.one * 0.1f, waveAnimDuration, 1, 1));
            }
        }
    }
}
