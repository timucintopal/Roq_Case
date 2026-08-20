using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Case1_FitTheShape.Scripts
{
    public class ShapeController : MonoBehaviour
    {
        [SerializeField] ShapeType shapeType;
        [SerializeField] private SegmentController targetSegment;

        private Transform JumpTarget => targetSegment.GetHole();


        [ContextMenu("JumpToTarget")]
        public void JumpToTarget()
        {
            if(JumpTarget != null && !_sequenceIsActive)
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
            float dirX = Mathf.Sign(JumpTarget.position.x - transform.position.x);
            if (Mathf.Abs(JumpTarget.position.x - transform.position.x) < 0.05f) dirX = 1f;

            // 1. Anticipation (Hazırlık) - Sıçramadan önce tatlı bir esneme/güç toplama
            yield return transform.DOPunchScale(Vector3.one * 0.75f, 0.3f, 1, 1).WaitForCompletion();

            // 2. Havalanma ve Spin (Direkt Zıplama)
            Sequence airSeq = DOTween.Sequence();
            
            float jumpDuration = 0.45f; // Zıplama ve takla süresi

            // Hedefe doğrudan kavisli zıplama
            airSeq.Append(transform.DOJump(JumpTarget.position, 1.5f, 1, jumpDuration).SetEase(Ease.InOutSine));
            
            // Zıplarken gideceği yöne doğru 180 derece (yarım takla) spin atar.
            // Böylece inişte objenin Y ekseni tam tersine (hedefin -Y'sine) dönmüş olarak oturur.
            // Eğer 1.5 takla atıp yine ters oturmasını isterseniz 180 yerine 540 yapabilirsiniz.
            airSeq.Join(transform.DORotate(new Vector3(0, 0, -dirX * 180f), jumpDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.OutCubic));

            yield return airSeq.WaitForCompletion();
            
            // 4. İniş (Impact) - Yuvaya girdiğini hissettirecek minik bir bounce/squash
            yield return transform.DOPunchScale(new Vector3(0.3f, -0.3f, 0.3f), 0.2f, 1, 1).WaitForCompletion();

            // 5. Yuvaya oturduğumuzu bildir (Hole kapanacak ve Meksika dalgası başlayacak)
            if (targetSegment != null)
            {
                targetSegment.OnShapeLanded();
            }

            _sequenceIsActive = false;
        }
        
        
    }
}
