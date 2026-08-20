using UnityEngine;
using DG.Tweening;

namespace Case1_FitTheShape.Scripts
{
    public class SegmentController : MonoBehaviour
    {
        [SerializeField] private Shape shape;
        [SerializeField] private Transform hole;

        [ContextMenu("FillHole")]
        public void SetHole()
        {
            hole = transform.GetChild(0);
        }

        public Transform GetHole()
        {
            return hole;
        }

        public bool SegmentCheck(ShapeType type)
        {
            return shape.Type == type;
        }

        public void OnShapeLanded()
        {
            if (hole != null)
            {
                // Deliği küçülterek yok et (Juice!)
                hole.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => hole.gameObject.SetActive(false));
            }

            // MDrum'ı bul ve meksika dalgasını başlat
            MDrum drum = GetComponentInParent<MDrum>();
            if (drum != null)
            {
                // Dalganın etki edeceği maksimum fiziksel mesafe (örneğin 6 birim)
                drum.PlayWaveEffect(this, 6f);
            }
        }
    }
}