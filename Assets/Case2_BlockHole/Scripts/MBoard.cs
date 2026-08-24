using DG.Tweening;
using UnityEngine;

namespace Case2_BlockHole.Scripts
{
    public class MBoard : MonoBehaviour
    {
        public static MBoard Instance;

        [Header("Board Shake Settings")]
        [Tooltip("Küp çıkışlarında tahtanın aşağı doğru esneme (yaylanma) miktarı")]
        [SerializeField] private float punchStrength = 0.15f;
        [Tooltip("Esnemenin süresi (0.1 ila 0.2 arası idealdir)")]
        [SerializeField] private float punchDuration = 0.15f;

        private Vector3 _originalPos;
        private Tween _punchTween;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                _originalPos = transform.position;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PunchBoard()
        {
            if (punchStrength <= 0f) return;

            // Önceki punch henüz bitmediyse iptal edip tahtayı düzeltiyoruz ki 
            // üst üste binip pozisyonun kaymasına (offset) yol açmasın.
            if (_punchTween != null && _punchTween.IsActive())
            {
                _punchTween.Kill();
                transform.position = _originalPos;
            }

            // Aşağı doğru (-Y ekseninde) tok bir esneme vuruyoruz
            _punchTween = transform.DOPunchPosition(Vector3.down * punchStrength, punchDuration, 1, 0f)
                .OnComplete(() => transform.position = _originalPos); // Ne olursa olsun orijinal pozisyonu garantiye al
        }
    }
}
