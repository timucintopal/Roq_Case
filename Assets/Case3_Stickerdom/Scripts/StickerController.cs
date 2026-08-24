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
        [Tooltip("Kıvrılan uca takılacak Particle System veya Trail (uçuş sırasındaki iz vs.)")]
        public Transform tipEffectTransform;
        [Tooltip("Sticker tam yere yapışıp düzleştiği an (şlaap) Instantiate edilecek Particle Prefab'ı")]
        public ParticleSystem impactParticlePrefab;
        [Tooltip("Impact efektinin yaratılacağı nokta (Boş bırakılırsa kıvrılan uç noktası kullanılır)")]
        public Transform impactTarget;

        [Header("Settings")]
        public float maxPeelAmount = 0.776f;
        [Tooltip("Soyulma/Kıvrılma yönünü belirler. (-1, 1) sol üste doğru, (1, 1) sağ üste doğru vs.")]
        public Vector2 fixedPeelDirection = new Vector2(-1f, 1f);

        [Header("Timing Settings")]
        [Tooltip("İlk tıklamada sticker'ın kıvrılma (sökülme) süresi.")]
        public float peelDuration = 0.3f;
        [Tooltip("Sticker'ın havada uçup hedefe gitme süresi.")]
        public float flightDuration = 0.8f;
        [Tooltip("Hedefe ulaştıktan sonra düzleşip yapışma süresi.")]
        public float stickDuration = 0.2f;

        private Material stickerMaterial;
        private int peelAmountPropId;
        private int peelDirPropId;
        private int shineLocationPropId;

        private bool isPlaced = false;

        private Renderer activeRenderer;
        private Vector3 initialTipLocalPos;

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
            
            CalculateInitialTipPosition();
        }

        private void CalculateInitialTipPosition()
        {
            Vector2 peelDir = fixedPeelDirection.normalized;
            if (peelDir == Vector2.zero) peelDir = new Vector2(-1, 1).normalized;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                Vector3 boundsMax = sr.sprite.bounds.max;
                Vector3 boundsMin = sr.sprite.bounds.min;
                
                Vector2[] corners = new Vector2[] {
                    new Vector2(boundsMin.x, boundsMin.y),
                    new Vector2(boundsMin.x, boundsMax.y),
                    new Vector2(boundsMax.x, boundsMin.y),
                    new Vector2(boundsMax.x, boundsMax.y)
                };

                float maxD = float.MinValue;
                Vector2 bestCorner = Vector2.zero;
                
                foreach (var corner in corners)
                {
                    float dot = Vector2.Dot(corner, peelDir);
                    if (dot > maxD)
                    {
                        maxD = dot;
                        bestCorner = corner;
                    }
                }
                
                initialTipLocalPos = new Vector3(bestCorner.x, bestCorner.y, 0);
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

        void Update()
        {
            // Eğer bir particle/trail tanımlıysa, shader matematiğini C# tarafında simüle et
            if (tipEffectTransform != null && stickerMaterial != null)
            {
                float currentPeel = stickerMaterial.GetFloat(peelAmountPropId);
                float curlRad = stickerMaterial.HasProperty("_CurlRadius") ? stickerMaterial.GetFloat("_CurlRadius") : 0.3f;
                float sSize = stickerMaterial.HasProperty("_SpriteSize") ? stickerMaterial.GetFloat("_SpriteSize") : 5.0f;
                
                Vector2 peelDir = fixedPeelDirection.normalized;
                if (peelDir == Vector2.zero) peelDir = new Vector2(-1, 1).normalized;

                float d = Vector2.Dot((Vector2)initialTipLocalPos, peelDir);
                float peelLine = sSize - (currentPeel * sSize * 2.0f);
                float distToLine = d - peelLine;

                Vector3 currentTipLocalPos = initialTipLocalPos;

                if (distToLine > 0f)
                {
                    float theta = distToLine / curlRad;
                    
                    if (theta > Mathf.PI)
                    {
                        float flatDist = distToLine - (Mathf.PI * curlRad);
                        currentTipLocalPos.x -= peelDir.x * (distToLine + flatDist);
                        currentTipLocalPos.y -= peelDir.y * (distToLine + flatDist);
                        currentTipLocalPos.z = -(curlRad * 2.0f);
                    }
                    else
                    {
                        float x_prime = Mathf.Sin(theta) * curlRad;
                        float z_prime = (1.0f - Mathf.Cos(theta)) * curlRad;
                        currentTipLocalPos.x += peelDir.x * (-distToLine + x_prime);
                        currentTipLocalPos.y += peelDir.y * (-distToLine + x_prime);
                        currentTipLocalPos.z = -z_prime;
                    }
                }
                
                tipEffectTransform.position = transform.TransformPoint(currentTipLocalPos);
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
                
                // Taşınma (Uçuş) başlarken Trail/Particle'ı durdur
                flySeq.AppendCallback(() => {
                    if (tipEffectTransform != null)
                    {
                        ParticleSystem ps = tipEffectTransform.GetComponent<ParticleSystem>();
                        if (ps != null) ps.Stop();
                        
                        TrailRenderer tr = tipEffectTransform.GetComponent<TrailRenderer>();
                        if (tr != null) tr.emitting = false;
                    }
                });
                
                // Zıplayarak hedefe git (DOJump parabolik, tatmin edici bir uçuş sağlar)
                flySeq.Append(transform.DOJump(
                    new Vector3(targetSlot.transform.position.x, targetSlot.transform.position.y, targetZ),
                    jumpPower: 1.5f,
                    numJumps: 1,
                    duration: flightDuration
                ));

                // Uçarken eş zamanlı olarak Scale'i 1 yap ve Rotasyonu Sıfırla (dik konuma getir)
                flySeq.Join(transform.DOScale(Vector3.one, flightDuration));
                flySeq.Join(transform.DORotate(Vector3.zero, flightDuration));

                // Hedefe vardığında 2 aşamalı tepki
                flySeq.OnComplete(() => {
                    
                    // 1. Hedefe vardığı an (uçuş bittiğinde) yapışma başlarken Trail/Particle'ı tekrar başlat:
                    if (tipEffectTransform != null)
                    {
                        ParticleSystem ps = tipEffectTransform.GetComponent<ParticleSystem>();
                        if (ps != null) ps.Play();
                        
                        TrailRenderer tr = tipEffectTransform.GetComponent<TrailRenderer>();
                        if (tr != null)
                        {
                            tr.Clear(); // Önceki pozisyondan çizgi çekmesini engellemek için temizle
                            tr.emitting = true;
                        }
                    }

                    if (stickerMaterial != null)
                    {
                        // Uç kıvrımının düzleşme (yapışma) animasyonu
                        stickerMaterial.DOFloat(0f, "_PeelAmount", stickDuration).OnComplete(() => {
                            // 2. Düzleşme bittiği an (tam yapıştığında) IMPACT particle'ı Instantiate et:
                            if (impactParticlePrefab != null)
                            {
                                Vector3 spawnPos = impactTarget != null ? impactTarget.position : transform.TransformPoint(initialTipLocalPos);
                                ParticleSystem ps = Instantiate(impactParticlePrefab, spawnPos, Quaternion.identity);
                                ps.Play();
                                
                                // Particle ömrü dolunca objeyi sahneden temizle
                                float destroyTime = ps.main.duration + ps.main.startLifetime.constantMax;
                                Destroy(ps.gameObject, destroyTime);
                            }
                        });
                        stickerMaterial.SetFloat(shineLocationPropId, -1f);
                    }
                    
                    // --- 1. AŞAMA (VURUŞ) ---
                    transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.3f, 5, 0.5f);
                    
                    // Mikro Kamera Sarsıntısı (Vuruş / Impact hissi)
                    if (Camera.main != null)
                    {
                        Camera.main.transform.DOShakePosition(0.15f, strength: 0.1f, vibrato: 10, randomness: 90f, snapping: false, fadeOut: true);
                    }
                    
                    if (shadowTransform != null)
                    {
                        shadowTransform.DOLocalMove(Vector3.zero, stickDuration);
                        SpriteRenderer shadowSR = shadowTransform.GetComponent<SpriteRenderer>();
                        if (shadowSR != null) shadowSR.DOFade(0f, stickDuration);
                    }

                    // --- 2. AŞAMA (SİHİR/ÖDÜL) ---
                    DOVirtual.DelayedCall(0.4f, () => {
                        if (stickerMaterial != null)
                        {
                            stickerMaterial.DOFloat(3f, "_ShineLocation", 0.6f).SetEase(Ease.InOutSine);
                        }
                        // Hare geçerken çok yumuşak bir kalp atışı (nefes alma) efekti
                        transform.DOScale(Vector3.one * 1.06f, 0.3f).SetEase(Ease.OutQuad).OnComplete(() => {
                            transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InQuad);
                        });
                    });
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
