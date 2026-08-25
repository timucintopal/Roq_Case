using UnityEngine;
using DG.Tweening;

namespace Case1_FitTheShape.Scripts
{
    public class SegmentController : MonoBehaviour
    {
        public Vector2Int GridPos { get; set; } // MDrum tarafından atanacak matematiksel grid koordinatı

        [SerializeField] private Shape shape;
        [SerializeField] private Transform hole;
        [SerializeField] private Transform approachPoint;

        [ContextMenu("FillHole")]
        public void SetHole()
        {
            if (transform.childCount > 0) hole = transform.GetChild(0);
            if (transform.childCount > 9) approachPoint = transform.GetChild(9);
        }

        public Transform GetHole()
        {
            return hole;
        }

        // Objelerin yuvaya girmeden hemen önce havada hizalanacağı nokta
        public Transform GetApproachPoint()
        {
            return approachPoint != null ? approachPoint : hole; 
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
            // Bağımlılık kalktı: Yuvaya oturduğumuzu global event ile duyuruyoruz.
            // Dinleyen kim varsa (MDrum) gerekli dalga ve partikül efektlerini oynatır.
            GameEvents.OnSegmentFilled?.Invoke(this, shape.Type);
        }
    }
}