using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Case4_Buca.Scripts
{
    public class Cube : MonoBehaviour
    {
        private static float lastHitStopTime = 0f; // Çoklu çarpmalarda Hit Stop'u sınırlamak için

        [SerializeField] Renderer _renderer;
        [SerializeField] Rigidbody _rigidbody;
        
        [SerializeField] LayerMask triggerLayers;

        [Space]
        [SerializeField] Color targetColor;
        [SerializeField] float coloringDuration;

        [SerializeField] bool isHit = false;

        private void Start()
        {
            if (MCube.Instance != null)
            {
                MCube.Instance.RegisterCube();
                MCube.Instance.OnAllCubesHit += OnAllCubesCompleted;
            }
        }

        private void OnDestroy()
        {
            if (MCube.Instance != null)
            {
                MCube.Instance.OnAllCubesHit -= OnAllCubesCompleted;
            }
        }

        public void OnAllCubesCompleted()
        {
            
            transform.DOScale(Vector3.zero, 1)
                .OnStart(() =>
            {
                _rigidbody.isKinematic = true;
            })
                .SetEase(Ease.InBack).SetDelay(1.5f);
            // Gelecekte eklenecek tüm küplere ait ortak davranışlar buraya gelecek.
        }

        [ContextMenu("SET")]
        private void Set()
        {
            _renderer = GetComponent<Renderer>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision other)
        {
            if (isHit) return;

            if ((triggerLayers.value & (1 << other.gameObject.layer)) > 0)
            {
                isHit = true;
                if (MCube.Instance != null) MCube.Instance.CubeHit();
                
                _renderer.material.DOColor(targetColor, coloringDuration);
                
                // --- JUICE: Squash & Stretch (Scale Punch) ---
                // Çarpışma anında küp jöle gibi titreyip şişer/ezilir
                transform.DOPunchScale(new Vector3(0.4f, -0.4f, 0.4f), 0.4f, 6, 1f);
                
                // --- JUICE: Hit Stop (Mikro Zaman Durması) ---
                // 16 hedef art arda vurulduğunda oyunun kasıyor gibi hissettirmemesi için "Cooldown" (soğuma) süresi ekliyoruz.
                // Sadece son hit stop'tan bu yana yeterli zaman (örn: 0.15 sn) geçmişse çalışır.
                if (Time.unscaledTime - lastHitStopTime > 0.15f)
                {
                    lastHitStopTime = Time.unscaledTime;
                    Time.timeScale = 0.05f; // Zamanı yavaşlat
                    DG.Tweening.DOVirtual.DelayedCall(0.03f, () => 
                    {
                        Time.timeScale = 1f; // Daha kısa sürede (0.03sn) normale dön
                    }, ignoreTimeScale: true);
                }
                
                // Çarpışmadan 1.5 saniye sonra başla, Drag ve Angular Drag değerlerini 2 saniye içinde 5'e kadar çıkart.
                DOVirtual.Float(_rigidbody.linearDamping, 5f, 1f, (value) => {
                    _rigidbody.linearDamping = value;
                    _rigidbody.angularDamping = value;
                }).SetDelay(1.5f);
            }
        }

        IEnumerator CheckVelocity()
        {
            yield return new WaitForSeconds(1);
            
            yield return new WaitUntil(() => _rigidbody.linearVelocity.sqrMagnitude <= 0.05f);
        }
    }
}
