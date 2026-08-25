using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Case4_Buca.Scripts
{
    public class Cube : MonoBehaviour
    {
        [SerializeField] Renderer _renderer;
        [SerializeField] Rigidbody _rigidbody;
        
        [SerializeField] LayerMask triggerLayers;

        [Space]
        [SerializeField] Gradient colorTransition;
        [SerializeField] float coloringDuration;
        [SerializeField] float impactForce = 3f;
        [SerializeField] float angularForce = 1.5f;

        [Header("Hole Effect")]
        [SerializeField] ParticleSystem holeParticle;
        [Tooltip("Hangi katmanlara ray atılacağını seçin (örn: Ground ve Obstacle)")]
        [SerializeField] LayerMask surfaceLayers;
        [Tooltip("Partikülün zeminden ne kadar yukarıda olacağı.")]
        [SerializeField] float particleYOffset = 0.1f;

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
            // Bu metod artık boş, çünkü küçülme ve yok olma işlemi her küpün kendi çarpışmasında bağımsız olarak gerçekleşiyor.
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
                
                // Hafif ve yukarı doğru kalkan yumuşak bir darbe gücü uyguluyoruz
                ContactPoint contact = other.GetContact(0);
                Vector3 forceDirection = (transform.position - contact.point).normalized;
                forceDirection.y += 0.2f; 
                _rigidbody.AddForceAtPosition(forceDirection.normalized * impactForce, contact.point, ForceMode.Impulse);

                // Geriye doğru doğal devrilme için tork (devrilme ekseni) hesaplama
                Vector3 tipAxis = Vector3.Cross(Vector3.up, forceDirection.normalized);
                _rigidbody.AddTorque(tipAxis * angularForce, ForceMode.Impulse);

                _renderer.material.DOGradientColor(colorTransition, coloringDuration);
                
                // Çarpışmadan 1.5 saniye sonra başla, Drag ve Angular Drag değerlerini 2 saniye içinde 5'e kadar çıkart.
                DOVirtual.Float(_rigidbody.linearDamping, 5f, 1f, (value) => {
                    _rigidbody.linearDamping = value;
                    _rigidbody.angularDamping = value;
                }).SetDelay(1.5f);

                // Her küp çarpışmadan belli süre sonra kendi deliğini açıp küçülür
                Sequence seq = DOTween.Sequence();
                
                // Küçülmeden 0.25 saniye önce partikülün açılması için 1.25 saniye bekliyoruz
                seq.AppendInterval(1.25f);
                seq.AppendCallback(() => 
                {
                    _rigidbody.isKinematic = true;

                    if (holeParticle != null)
                    {
                        holeParticle.transform.SetParent(null);
                        
                        if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out RaycastHit hit, 10f, surfaceLayers))
                        {
                            holeParticle.transform.position = new Vector3(transform.position.x, hit.point.y + particleYOffset, transform.position.z);
                            holeParticle.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                        }
                        else
                        {
                            holeParticle.transform.position = new Vector3(transform.position.x, particleYOffset, transform.position.z);
                            holeParticle.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                        }
                        
                        holeParticle.gameObject.SetActive(true);
                        holeParticle.Play();
                        
                        Destroy(holeParticle.gameObject, 3f);
                    }
                });
                
                // Partikül açıldıktan sonra küpün küçülmeye başlaması için 0.25 saniye bekle
                seq.AppendInterval(0.25f);
                seq.Append(transform.DOScale(Vector3.zero, 1).SetEase(Ease.InBack));
            }
        }

        IEnumerator CheckVelocity()
        {
            yield return new WaitForSeconds(1);
            
            yield return new WaitUntil(() => _rigidbody.linearVelocity.sqrMagnitude <= 0.05f);
        }
    }
}
