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

        [Header("Audio")]
        [Tooltip("Sticker hedefe tam yapıştığında çalınacak ses (İsteğe bağlı)")]
        public AudioClip stickSound;

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

        // Caching
        private Material _stickerMaterial;
        private Renderer _activeRenderer;
        private ParticleSystem _tipParticle;
        private TrailRenderer _tipTrail;
        private SpriteRenderer _shadowRenderer;

        // Shader properties
        private int _peelAmountPropId;
        private int _peelDirPropId;
        private int _shineLocationPropId;

        // State
        private bool _isPlaced = false;
        private Vector3 _initialTipLocalPos;

        public bool IsPlaced => _isPlaced;
        public int SortingOrder => _activeRenderer != null ? _activeRenderer.sortingOrder : 0;

        private void Awake()
        {
            _peelAmountPropId = Shader.PropertyToID("_PeelAmount");
            _peelDirPropId = Shader.PropertyToID("_PeelDirection");
            _shineLocationPropId = Shader.PropertyToID("_ShineLocation");

            if (tipEffectTransform != null)
            {
                _tipParticle = tipEffectTransform.GetComponent<ParticleSystem>();
                _tipTrail = tipEffectTransform.GetComponent<TrailRenderer>();
            }

            if (shadowTransform != null)
            {
                _shadowRenderer = shadowTransform.GetComponent<SpriteRenderer>();
            }
        }

        private void Start()
        {
            if (_activeRenderer == null)
            {
                Renderer childRenderer = GetComponentInChildren<MeshRenderer>();
                SetActiveRenderer(childRenderer != null ? childRenderer : GetComponent<SpriteRenderer>());
            }
            
            CalculateInitialTipPosition();
        }

        public void SetActiveRenderer(Renderer rend)
        {
            if (rend == null) return;
        
            _activeRenderer = rend;
        
            if (Application.isPlaying && _activeRenderer.sharedMaterial != null)
            {
                _stickerMaterial = new Material(_activeRenderer.sharedMaterial);
                
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null && sr.sprite.texture != null)
                {
                    _stickerMaterial.SetTexture("_MainTex", sr.sprite.texture);
                    _stickerMaterial.SetColor("_Color", sr.color);
                }

                _activeRenderer.material = _stickerMaterial;
            
                if (_peelAmountPropId != 0) _stickerMaterial.SetFloat(_peelAmountPropId, 0f);
                if (_shineLocationPropId != 0) _stickerMaterial.SetFloat(_shineLocationPropId, -1f);
            }
        }

        private void Update()
        {
            UpdateTipPosition();
        }

        public void OnTap(Vector3 clickWorldPos)
        {
            if (_isPlaced) return;
            _isPlaced = true; 

            StartPeelAnimation();

            StickerSlot targetSlot = StickerSlotManager.Instance != null ? StickerSlotManager.Instance.GetAvailableSlot(stickerType) : null;

            if (targetSlot != null)
            {
                targetSlot.isFilled = true;
                FlyToTarget(targetSlot);
            }
            else
            {
                HandleRejection();
            }
        }

        // ==========================
        // ANIMATION & LOGIC METHODS
        // ==========================

        private void StartPeelAnimation()
        {
            Vector2 peelDir = fixedPeelDirection.normalized;
            if (peelDir == Vector2.zero) peelDir = new Vector2(-1, 1).normalized; 

            if (_stickerMaterial != null)
            {
                _stickerMaterial.SetVector(_peelDirPropId, peelDir);
                DOTween.Kill(_stickerMaterial);
                _stickerMaterial.DOFloat(maxPeelAmount, "_PeelAmount", peelDuration);
            }

            ShowShadow();
        }

        private void FlyToTarget(StickerSlot targetSlot)
        {
            float targetZ = targetSlot.transform.position.z - 0.1f;
            
            Sequence flySeq = DOTween.Sequence();
            flySeq.AppendInterval(peelDuration);
            
            flySeq.AppendCallback(StopTipEffects);
            
            flySeq.Append(transform.DOJump(
                new Vector3(targetSlot.transform.position.x, targetSlot.transform.position.y, targetZ),
                jumpPower: 1.5f,
                numJumps: 1,
                duration: flightDuration
            ));

            flySeq.Join(transform.DOScale(Vector3.one, flightDuration));
            flySeq.Join(transform.DORotate(Vector3.zero, flightDuration));

            flySeq.OnComplete(HandleSuccessfulPlacement);
        }

        private void HandleSuccessfulPlacement()
        {
            PlayTipEffects();

            if (_stickerMaterial != null)
            {
                _stickerMaterial.DOFloat(0f, "_PeelAmount", stickDuration).OnComplete(PlayImpactEffect);
                _stickerMaterial.SetFloat(_shineLocationPropId, -1f);
            }
            
            transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.3f, 5, 0.5f);
            
            if (Camera.main != null)
            {
                Camera.main.transform.DOShakePosition(0.15f, strength: 0.1f, vibrato: 10, randomness: 90f, snapping: false, fadeOut: true);
                
                if (stickSound != null)
                {
                    AudioSource.PlayClipAtPoint(stickSound, Camera.main.transform.position);
                }
            }
            
            HideShadow(stickDuration);
            PlayMagicRewardEffect();
        }

        private void HandleRejection()
        {
            if (_stickerMaterial != null)
            {
                _stickerMaterial.DOFloat(0f, "_PeelAmount", peelDuration).SetDelay(peelDuration + 0.1f);
            }
            
            HideShadow(peelDuration, peelDuration + 0.1f);

            DOVirtual.DelayedCall(peelDuration * 2f + 0.2f, () => {
                _isPlaced = false;
            });
        }

        // ==========================
        // VFX & SHADOW HELPERS
        // ==========================

        private void StopTipEffects()
        {
            if (_tipParticle != null) _tipParticle.Stop();
            if (_tipTrail != null) _tipTrail.emitting = false;
        }

        private void PlayTipEffects()
        {
            if (_tipParticle != null) _tipParticle.Play();
            if (_tipTrail != null)
            {
                _tipTrail.Clear();
                _tipTrail.emitting = true;
            }
        }

        private void PlayImpactEffect()
        {
            if (impactParticlePrefab != null)
            {
                Vector3 spawnPos = impactTarget != null ? impactTarget.position : transform.TransformPoint(_initialTipLocalPos);
                ParticleSystem ps = Instantiate(impactParticlePrefab, spawnPos, Quaternion.identity);
                ps.Play();
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
            }
        }

        private void PlayMagicRewardEffect()
        {
            DOVirtual.DelayedCall(0.4f, () => {
                if (_stickerMaterial != null)
                {
                    _stickerMaterial.DOFloat(3f, "_ShineLocation", 0.6f).SetEase(Ease.InOutSine);
                }
                transform.DOScale(Vector3.one * 1.06f, 0.3f).SetEase(Ease.OutQuad).OnComplete(() => {
                    transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InQuad);
                });
            });
        }

        private void ShowShadow()
        {
            if (shadowTransform != null)
            {
                shadowTransform.DOLocalMove(new Vector3(0.15f, -0.15f, 0.5f), peelDuration);
                if (_shadowRenderer != null) _shadowRenderer.DOFade(0.4f, peelDuration);
            }
        }

        private void HideShadow(float duration, float delay = 0f)
        {
            if (shadowTransform != null)
            {
                shadowTransform.DOLocalMove(Vector3.zero, duration).SetDelay(delay);
                if (_shadowRenderer != null) _shadowRenderer.DOFade(0f, duration).SetDelay(delay);
            }
        }

        // ==========================
        // SHADER SIMULATION
        // ==========================

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
                
                _initialTipLocalPos = new Vector3(bestCorner.x, bestCorner.y, 0);
            }
        }

        private void UpdateTipPosition()
        {
            if (tipEffectTransform == null || _stickerMaterial == null) return;

            float currentPeel = _stickerMaterial.GetFloat(_peelAmountPropId);
            float curlRad = _stickerMaterial.HasProperty("_CurlRadius") ? _stickerMaterial.GetFloat("_CurlRadius") : 0.3f;
            float sSize = _stickerMaterial.HasProperty("_SpriteSize") ? _stickerMaterial.GetFloat("_SpriteSize") : 5.0f;
            
            Vector2 peelDir = fixedPeelDirection.normalized;
            if (peelDir == Vector2.zero) peelDir = new Vector2(-1, 1).normalized;

            float d = Vector2.Dot((Vector2)_initialTipLocalPos, peelDir);
            float peelLine = sSize - (currentPeel * sSize * 2.0f);
            float distToLine = d - peelLine;

            Vector3 currentTipLocalPos = _initialTipLocalPos;

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

        private void OnDestroy()
        {
            if (_stickerMaterial != null)
            {
                Destroy(_stickerMaterial);
            }
        }
    }
}
