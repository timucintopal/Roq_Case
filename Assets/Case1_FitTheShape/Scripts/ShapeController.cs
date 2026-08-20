using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Case1_FitTheShape.Scripts
{
    public class ShapeController : MonoBehaviour
    {
        [SerializeField] ShapeType shapeType;
        [SerializeField] private Transform jumpTarget;

        [ContextMenu("JumpToTarget")]
        public void JumpToTarget()
        {
            if(jumpTarget != null && !_sequenceIsActive)
                StartCoroutine(MoveSequence());
        }

        bool _sequenceIsActive = false;
        
        IEnumerator MoveSequence()
        {
            _sequenceIsActive = true;
            
            // yield return transform.DOPunchScale(Vector3.one * 0.75f, .45f, 1, 1).WaitForCompletion();
            // yield return transform.DOLocalMoveY(2, .2f).SetDelay(.1f).SetEase(Ease.OutBack).SetRelative().WaitForCompletion();
            // yield return transform.DOJump(jumpTarget.position, 1,1,.25f).SetEase(Ease.InOutSine).WaitForCompletion();

            // Gideceği yönü belirliyoruz (X eksenine göre)
            float dirX = Mathf.Sign(jumpTarget.position.x - transform.position.x);
            if (Mathf.Abs(jumpTarget.position.x - transform.position.x) < 0.05f) dirX = 1f;

            // 1. Anticipation (Hazırlık) - Sıçramadan önce tatlı bir esneme/güç toplama
            yield return transform.DOPunchScale(Vector3.one * 0.75f, 0.3f, 1, 1).WaitForCompletion();

            // 2. Havalanma ve Spin (Juice!)
            Sequence airSeq = DOTween.Sequence();
            
            // Yuvanın Y pozisyonuna offset ekleyerek havalanma
            float offsetY = 2f;
            airSeq.Append(transform.DOMoveY(jumpTarget.position.y + offsetY, 0.35f).SetEase(Ease.OutBack));
            
            // Havalanırken gideceği yöne doğru 360 derece estetik bir spin (takla)
            airSeq.Join(transform.DORotate(new Vector3(0, 0, -dirX * 360f), 0.35f, RotateMode.FastBeyond360)
                .SetRelative()
                .SetEase(Ease.OutCubic));

            yield return airSeq.WaitForCompletion();

            // 3. Hedefe (yuvaya) tam oturuş - Havadayken yuvaya doğru bir kavis
            yield return transform.DOJump(jumpTarget.position, 0.5f, 1, 0.25f).SetEase(Ease.InOutSine).WaitForCompletion();
            
            // 4. İniş (Impact) - Yuvaya girdiğini hissettirecek minik bir bounce/squash
            yield return transform.DOPunchScale(new Vector3(0.3f, -0.3f, 0.3f), 0.2f, 1, 1).WaitForCompletion();

            _sequenceIsActive = false;
        }
        
        
    }
}
