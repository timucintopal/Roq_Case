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
        private Transform ApproachTarget => targetSegment.GetApproachPoint();


        [ContextMenu("JumpToTarget")]
        public void JumpToTarget()
        {
            if (_sequenceIsActive) return;

            // MDrum'daki aktif segmentler arasından şekil tipine uygun, bize EN YAKIN (kendi hizamızdaki) boş hedefi ara
            SegmentController match = MDrum.Instance.GetMatchingActiveSegment(shapeType);
            
            
            if (match != null)
            {
                // Eşleşme bulundu, hedefi anında kilitle ki havadayken başka şekil aynı yuvayı kapmasın
                match.LockSegment();
                targetSegment = match;
                StartCoroutine(MoveSequence());
            }
            else
            {
                // Eşleşme bulunamadı! Daha belirgin bir "hayır" titremesi ve jöle (wobble) tepkisi
                transform.DOComplete(); // Varsa önceki animasyonu durdur
                transform.DOShakeRotation(0.3f, new Vector3(0, 0, 30f), 15, 90, false);
                transform.DOPunchScale(new Vector3(0.4f, -0.2f, 0.4f), 0.3f, 5, 1);
            }
        }



        bool _sequenceIsActive = false;
        
        IEnumerator MoveSequence()
        {
            _sequenceIsActive = true;
            
            // yield return transform.DOPunchScale(Vector3.one * 0.75f, .45f, 1, 1).WaitForCompletion();
            // yield return transform.DOLocalMoveY(2, .2f).SetDelay(.1f).SetEase(Ease.OutBack).SetRelative().WaitForCompletion();
            // yield return transform.DOJump(jumpTarget.position, 1,1,.25f).SetEase(Ease.InOutSine).WaitForCompletion();

            // Gideceği yönü belirliyoruz (X eksenine göre)
            float dirX = Mathf.Sign(ApproachTarget.position.x - transform.position.x);
            if (Mathf.Abs(ApproachTarget.position.x - transform.position.x) < 0.05f) dirX = 1f;

            // 1. Anticipation (Hazırlık) - Sıçramadan önce tatlı bir esneme/güç toplama
            // Squash & Stretch: Önce yassılaşıp güç topluyor
            yield return transform.DOScale(new Vector3(1.4f, 0.6f, 1.4f), 0.12f).SetEase(Ease.OutQuad).WaitForCompletion();
            // Havalanırken eski formuna (hatta hafif ince uzun forma) dönerek zıplıyor
            transform.DOScale(new Vector3(0.9f, 1.2f, 0.9f), 0.15f).SetEase(Ease.OutBack)
                     .OnComplete(() => transform.DOScale(Vector3.one, 0.1f));

            // 2. Havalanma ve Saplanma (İki Aşamalı Zıplama)
            Sequence airSeq = DOTween.Sequence();
            
            float totalDuration = 0.45f; // Zıplama ve girme toplam süresi

            // Toplam hızı (hissiyatı) sabit tutmak için süreyi mesafeye oranlıyoruz
            float distToApproach = Vector3.Distance(transform.position, ApproachTarget.position);
            float distToHole = Vector3.Distance(ApproachTarget.position, JumpTarget.position);
            float totalDist = distToApproach + distToHole;
            if (totalDist == 0) totalDist = 0.1f; // Matematiksel Güvenlik

            float arcDuration = totalDuration * (distToApproach / totalDist);
            float dropDuration = totalDuration * (distToHole / totalDist);

            // 1. Aşama: Havadaki hizaya (Approach Point) kavisli zıpla
            airSeq.Append(transform.DOJump(ApproachTarget.position, 1.5f, 1, arcDuration).SetEase(Ease.InOutSine));
            
            // 2. Aşama: Havadaki hizadan asıl deliğe dikine (linear) gir
            // Append olduğu için 1. hareket bittiği milisaniyede hız kesmeden bu başlar. Ease.InSine ivmeyi giderek artırır (saplanma hissi).
            airSeq.Append(transform.DOMove(JumpTarget.position, dropDuration).SetEase(Ease.InSine));
            
            // YENİ: Yuvaya girerken şeklin kendisi de küçülüp sıfırlansın (yok olsun)
            airSeq.Join(transform.DOScale(Vector3.zero, dropDuration).SetEase(Ease.InSine));
            
            // YENİ DÖNÜŞ (ROTATION) MANTIĞI:
            // Objenin Y ekseni (Up), ApproachTarget'ın -Y eksenine (-Up) bakacak şekilde hedef rotasyon hesaplıyoruz.
            // Objenin Z eksenini (Forward) hedefin Z'si ile aynı tutuyoruz ki sağa sola sapmasın.
            Quaternion targetRotation = Quaternion.LookRotation(ApproachTarget.forward, -ApproachTarget.up);

            // Kullanıcı isteği: Dönme işlemi objenin approach noktasına varmasından önce tamamlansın.
            // Bu yüzden süreyi arcDuration'ın %65'i kadar yapıyoruz (Havadayken erkenden yüzünü döner).
            airSeq.Insert(0, transform.DORotateQuaternion(targetRotation, arcDuration * 0.65f).SetEase(Ease.InOutSine));

            // YENİ: Cuk diye oturma (Snap-Fit) hissiyatı! 
            // Toplam sürenin dolmasına 0.15 saniye kala hedefteki deliği kapatmaya başla.
            airSeq.InsertCallback(totalDuration - 0.15f, () => 
            {
                if (targetSegment != null)
                {
                    targetSegment.CloseHoleEarly();
                }
            });

            yield return airSeq.WaitForCompletion();
            // 4. İniş (Impact) - Yuvaya girdiğini hissettirecek minik bir bounce/squash kaldırıldı
            // Artık scale animasyonu oynatmadan doğrudan oturacak.

            // 5. Yuvaya oturduğumuzu bildir (Hole kapanacak ve Meksika dalgası başlayacak)
            if (targetSegment != null)
            {
                targetSegment.OnShapeLanded();
            }

            _sequenceIsActive = false;
        }
        
        
    }
}
