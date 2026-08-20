using UnityEngine;
using DG.Tweening;

namespace Case1_FitTheShape.Scripts
{
    public class SegmentController : MonoBehaviour
    {
        [SerializeField] private Shape shape;
        [SerializeField] private Transform hole;
        [SerializeField] private Transform approachPoint;

        [ContextMenu("FillHole")]
        public void SetHole()
        {
            hole = transform.GetChild(0);
            approachPoint = transform.GetChild(9);
        }

        public Transform GetHole()
        {
            return hole;
        }

        // Objelerin yuvaya girmeden hemen önce havada hizalanacağı nokta (9. Child)
        public Transform GetApproachPoint()
        {
            return approachPoint; // Eğer 9. child yoksa güvenlik olarak asıl deliği döndür
        }

        public bool IsFilled { get; private set; } = false;

        public bool SegmentCheck(ShapeType type)
        {
            return shape.Type == type;
        }

        // Şekil eşleşip havalandığı an, yuvayı başka bir şekil kapmasın diye kilitler
        public void LockSegment()
        {
            IsFilled = true;
        }

        // Şekil yuvaya değmeden hemen önce deliği kapatma efekti (Snap-Fit hissiyatı)
        public void CloseHoleEarly()
        {
            if (hole != null)
            {
                // Deliği küçülterek yok et
                hole.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).OnComplete(() => hole.gameObject.SetActive(false));
            }
        }

        public void OnShapeLanded()
        {
            // MDrum'ı bul ve meksika dalgasını başlat
            MDrum drum = GetComponentInParent<MDrum>();
            if (drum != null)
            {
                // Dalganın etki edeceği alan (reach) artık Inspector'dan MDrum üzerinden ayarlanıyor
                drum.PlayWaveEffect(this);
            }
        }
    }
}