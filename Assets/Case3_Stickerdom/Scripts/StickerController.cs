using DG.Tweening;
using UnityEngine;

namespace Case3_Stickerdom.Scripts
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class StickerController : MonoBehaviour
    {
        public StickerType stickerType;

        [Space, Header("References")]
        public Transform shadowTransform;

        [Header("Settings")]
        public float maxPeelAmount = 0.776f;
        public float peelDuration = 0.3f;
        [Tooltip("Soyulma/Kıvrılma yönünü belirler. (-1, 1) sol üste doğru, (1, 1) sağ üste doğru vs.")]
        public Vector2 fixedPeelDirection = new Vector2(-1f, 1f);

        private Material stickerMaterial;
        private int peelAmountPropId;
        private int peelDirPropId;
        private int shineLocationPropId;

        private bool isPlaced = false;

        private Renderer activeRenderer;

        public bool IsPlaced => isPlaced;
        public int SortingOrder => activeRenderer != null ? activeRenderer.sortingOrder : 0;

        void Start()
        {
            peelAmountPropId = Shader.PropertyToID("_PeelAmount");
            peelDirPropId = Shader.PropertyToID("_PeelDirection");
            shineLocationPropId = Shader.PropertyToID("_ShineLocation");

            if (activeRenderer == null)
            {
                Renderer childRenderer = GetComponentInChildren<MeshRenderer>();
                if (childRenderer != null)
                {
                    SetActiveRenderer(childRenderer);
                }
                else
                {
                    SetActiveRenderer(GetComponent<SpriteRenderer>());
                }
            }
        }

        public void SetActiveRenderer(Renderer rend)
        {
            if (rend == null) return;
        
            activeRenderer = rend;
        
            // Editör modundayken .material çağrısı yaparsak hafıza sızıntısı (leak) uyarısı verir.
            // Bu yüzden sadece oyun oynanırken (Play mode) instance alıyoruz.
            if (Application.isPlaying && activeRenderer.sharedMaterial != null)
            {
                stickerMaterial = new Material(activeRenderer.sharedMaterial);
                
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null && sr.sprite.texture != null)
                {
                    stickerMaterial.SetTexture("_MainTex", sr.sprite.texture);
                    stickerMaterial.SetColor("_Color", sr.color);
                }

                activeRenderer.material = stickerMaterial;
            
                if (peelAmountPropId != 0) 
                {
                    stickerMaterial.SetFloat(peelAmountPropId, 0f);
                }
                
                if (shineLocationPropId != 0)
                {
                    stickerMaterial.SetFloat(shineLocationPropId, -1f);
                }
            }
        }

        public void OnTap(Vector3 clickWorldPos)
        {
            if (isPlaced) return;
            isPlaced = true; // İlk tıklamada hemen kilitliyoruz ki uçarken art arda tıklanmasın

            Debug.Log("Sticker'a Tıklandı! (Tap) Obje: " + gameObject.name);
            
            // 1. Sabit soyulma yönünü kullan
            Vector2 peelDir = fixedPeelDirection.normalized;
            if (peelDir == Vector2.zero) peelDir = new Vector2(-1, 1).normalized; 

            if (stickerMaterial != null)
            {
                stickerMaterial.SetVector(peelDirPropId, peelDir);
                DOTween.Kill(stickerMaterial);
                // X sn duration ile katlanıyor (peelDuration)
                stickerMaterial.DOFloat(maxPeelAmount, "_PeelAmount", peelDuration);
            }

            // Gölge varsa hafifçe belli et
            if (shadowTransform != null)
            {
                shadowTransform.DOLocalMove(new Vector3(0.15f, -0.15f, 0.5f), peelDuration);
                SpriteRenderer shadowSR = shadowTransform.GetComponent<SpriteRenderer>();
                if (shadowSR != null) shadowSR.DOFade(0.4f, peelDuration);
            }

            // 2. MSticker'dan uygun slot var mı bak
            StickerSlot targetSlot = null;
            if (MSticker.Instance != null)
            {
                targetSlot = MSticker.Instance.GetAvailableSlot(stickerType);
            }

            if (targetSlot != null)
            {
                targetSlot.isFilled = true; // Yuvayı başkası kapmasın
                
                // Havalanma hissi için Z'yi öne al
                float targetZ = targetSlot.transform.position.z - 0.1f;
                
                Sequence flySeq = DOTween.Sequence();
                
                // Kıvrılma animasyonu bittikten hemen sonra uçmaya başla
                flySeq.AppendInterval(peelDuration);
                
                // Zıplayarak hedefe git (DOJump parabolik, tatmin edici bir uçuş sağlar)
                flySeq.Append(transform.DOJump(
                    new Vector3(targetSlot.transform.position.x, targetSlot.transform.position.y, targetZ),
                    jumpPower: 1.5f,
                    numJumps: 1,
                    duration: 0.8f
                ));

                // Uçarken eş zamanlı olarak Scale'i 1 yap
                flySeq.Join(transform.DOScale(Vector3.one, 0.8f));

                // Hedefe vardığında Foil değerini sıfırla, jöle efekti ver ve Parlama (Şimşek/Shine) geçişi yap
                flySeq.OnComplete(() => {
                    if (stickerMaterial != null)
                    {
                        stickerMaterial.DOFloat(0f, "_PeelAmount", 0.2f);
                        stickerMaterial.SetFloat(shineLocationPropId, -1f);
                        stickerMaterial.DOFloat(3f, "_ShineLocation", 0.5f).SetEase(Ease.InOutSine);
                    }
                    transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.3f, 5, 0.5f);
                    
                    if (shadowTransform != null)
                    {
                        shadowTransform.DOLocalMove(Vector3.zero, 0.2f);
                        SpriteRenderer shadowSR = shadowTransform.GetComponent<SpriteRenderer>();
                        if (shadowSR != null) shadowSR.DOFade(0f, 0.2f);
                    }
                });
            }
            else
            {
                // Slot yoksa veya doluysa, sadece katlanıp geri açılsın (hata/reddedilme efekti)
                if (stickerMaterial != null)
                {
                    stickerMaterial.DOFloat(0f, "_PeelAmount", peelDuration).SetDelay(peelDuration + 0.1f);
                }
                
                if (shadowTransform != null)
                {
                    shadowTransform.DOLocalMove(Vector3.zero, peelDuration).SetDelay(peelDuration + 0.1f);
                    SpriteRenderer shadowSR = shadowTransform.GetComponent<SpriteRenderer>();
                    if (shadowSR != null) shadowSR.DOFade(0f, peelDuration).SetDelay(peelDuration + 0.1f);
                }

                // Geri açıldıktan sonra tekrar tıklanabilsin diye kiliti kaldırıyoruz
                DOVirtual.DelayedCall(peelDuration * 2f + 0.2f, () => {
                    isPlaced = false;
                });
            }
        }

        void OnDestroy()
        {
            // Bahsettiğimiz Memory Leak (Hafıza Kaçağı) çözümüdür.
            if (stickerMaterial != null)
            {
                Destroy(stickerMaterial);
            }
        }
    }
}
