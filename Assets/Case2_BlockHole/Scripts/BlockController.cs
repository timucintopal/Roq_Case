using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Case2_BlockHole.Scripts
{
    public class BlockController : MonoBehaviour
    {
        public Hole.HoleColor holeColor;
        
        [SerializeField] MeshRenderer mainRenderer;
        [SerializeField] Collider mainCollider; // Ana objenin collider'ı
        
        [SerializeField] List<GameObject> blocks = new List<GameObject>();

        public void MoveToHole(Transform target)
        {
            StartCoroutine(MoveSequence(target));
        }

        private Vector3 _originalScale;
        private Tween _scaleTween;
        // Olay Güdümlü (Event-Driven) Mimari için tanımlamalar
        public static event System.Action<Hole.HoleColor> OnAnyBlockPickedUp;
        public static event System.Action OnAnyBlockDropped;

        private Material _outlineMaterial;
        private Rigidbody _rb;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _originalScale = transform.localScale;
            // Dinamik olarak Outline materyalini oluşturuyoruz
            _outlineMaterial = new Material(Shader.Find("Custom/SimpleOutline"));
            // Glow yapması için rengi şiddetlendiriyoruz (HDR Color - PostProcessing Bloom varsa parlaklık saçar)
            _outlineMaterial.SetColor("_OutlineColor", Color.white * 4f);
            _outlineMaterial.SetFloat("_OutlineWidth", 4f);
        }

        private void OnDestroy()
        {
            // Hafıza sızıntısını (Memory Leak) önlemek için dinamik oluşturulan materyali temizliyoruz
            if (_outlineMaterial != null)
            {
                Destroy(_outlineMaterial);
            }
        }

        public void OnPickup()
        {
            if (_rb != null) _rb.isKinematic = true; // Fizik motoruyla savaşmasını engelle (Kasma/Lag sorununu çözer)

            _scaleTween?.Kill();
            // Sadece ilk tutulduğunda bir kere büyüyüp hafifçe küçülecek (tatlı bir pop efekti)
            _scaleTween = transform.DOScale(_originalScale * 1.15f, 0.15f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => transform.DOScale(_originalScale * 1.05f, 0.15f).SetEase(Ease.InOutSine));

            if (mainRenderer != null)
            {
                // Mevcut materyallerin sonuna outline materyalini ekliyoruz
                var mats = mainRenderer.materials;
                var newMats = new Material[mats.Length + 1];
                for (int i = 0; i < mats.Length; i++) newMats[i] = mats[i];
                newMats[mats.Length] = _outlineMaterial;
                mainRenderer.materials = newMats;
            }
            
            // Olayı fırlat (Dinleyen yöneticiler -örneğin MHole- bu bloğun rengini alıp parlayacak)
            OnAnyBlockPickedUp?.Invoke(holeColor);
        }

        public void OnDrop()
        {
            if (_rb != null) _rb.isKinematic = false; // Yere bırakıldığında fiziği geri aç

            _scaleTween?.Kill();
            // Orijinal boyutuna tatlı bir şekilde geri dönsün
            transform.DOScale(_originalScale, 0.2f).SetEase(Ease.OutBack);
            
            // Olayı fırlat (Dinleyen yöneticiler -örneğin MHole- tüm parlamaları kapatacak)
            OnAnyBlockDropped?.Invoke();

            if (mainRenderer != null)
            {
                // Outline materyalini listeden çıkarıyoruz
                var mats = mainRenderer.materials;
                if (mats.Length > 0 && mats[mats.Length - 1].shader.name == "Custom/SimpleOutline")
                {
                    var newMats = new Material[mats.Length - 1];
                    for (int i = 0; i < newMats.Length; i++) newMats[i] = mats[i];
                    mainRenderer.materials = newMats;
                }
            }
        }

        IEnumerator MoveSequence(Transform target )
        {
            yield return transform.DOMove(target.position, .4f).SetEase(Ease.OutBack).WaitForCompletion();

            yield return new WaitForSeconds(.1f);

            if (mainRenderer != null) mainRenderer.enabled = false;
            if (mainCollider != null) mainCollider.enabled = false; // Parçalar çıkarken ana collidera çarpıp patlamaması için kapattık
                
            Vector3 center = transform.position;

            foreach(var block in blocks)
            {
                GameObject b = block; // Güvenli değişken kopyalama (Lambda içinde kaybolmaması için)
                b.SetActive(true);
                b.transform.localScale = Vector3.one; // Obje havuzlaması (pooling) varsa önceki küçülmeden dolayı 0 kalmasın

                // 1. Rigidbody'i güvenli şekilde buluyoruz
                Rigidbody rb = b.GetComponent<Rigidbody>();
                if (rb == null) rb = b.GetComponentInChildren<Rigidbody>();

                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = true; // Yerçekimi kapalı kalmış olabilir diye garantiye alıyoruz
                }
                else
                {
                    Debug.LogWarning("Block üzerinde Rigidbody bulunamadı! Lütfen objeye Rigidbody eklediğinizden emin olun: " + b.name);
                }

                // 2. Hedef noktaları hesaplıyoruz
                Vector3 currentPos = b.transform.position;
                
                // Merkezden dışa doğru hafif bir vektör bulalım (açılı fırlasınlar diye)
                Vector3 outwardDir = (currentPos - center).normalized;
                outwardDir.y = 0;
                if (outwardDir == Vector3.zero) 
                    outwardDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;

                // Tepe noktası: Sağa sola saçılmayı çok aza indirdik (0.05 ile 0.2 arası)
                Vector3 peakPos = currentPos + (outwardDir * Random.Range(0.05f, 0.2f)) + (Vector3.up * Random.Range(1.0f, 1.8f));

                // Yukarı kalkış sırasında sahte dönme animasyonu (0.5 saniye)
                Vector3 randomRotation = new Vector3(Random.Range(-180f, 180f), Random.Range(-180f, 180f), Random.Range(-180f, 180f));
                b.transform.DORotate(randomRotation, 0.5f, RotateMode.FastBeyond360).SetEase(Ease.OutCubic);

                // Yukarı kalkarken fiziksel çarpışmaları tamamen önlemek ve şık bir etki için boyutu pürüzsüzce 0.5'e düşür
                b.transform.DOScale(Vector3.one * 0.5f, 0.5f).SetEase(Ease.OutCubic);

                // Yukarı tatlı kalkış
                b.transform.DOMove(peakPos, 0.5f).SetEase(Ease.OutCubic).OnComplete(() =>
                {
                    // TEPE NOKTASINDA FİZİĞİ SERBEST BIRAK!
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                        rb.WakeUp(); // Fizik motorunu zorla uyandır (bazen kinematic objeler uyku modunda kalabilir)
                        
                        // Aşırı fırıldak gibi dönmesini engelleyen Unity C++ kısıtlamaları (Performanslı çözüm)
                        rb.maxAngularVelocity = 5f; // Maksimum dönüş hızını sınırla
                        rb.angularDamping = 0.5f;      // Havadayken yavaşça dönme hızını kessin
                        
                        // Yerçekimiyle düşerken dönmeye (takla atmaya) devam etmeleri için tork (dönüş ivmesi) veriyoruz
                        rb.AddTorque(Random.insideUnitSphere * Random.Range(10f, 20f), ForceMode.Impulse);
                    }
                    
                    // 0.5 boyutuna indikten sonra, düşüş boyunca kaybolma efekti. 
                    // DİKKAT: Fiziksel (Rigidbody) bir objeyi tam olarak Vector3.zero'ya (0) küçültmek, 
                    // PhysX motorunda hacmin sıfır olmasına ve "Infinite / NaN" hatalarına yol açar.
                    // Bu yüzden 0.01'e kadar küçültüp sonra objeyi tamamen kapatıyoruz.
                    b.transform.DOScale(Vector3.one * 0.01f, 1.5f).SetEase(Ease.InQuad).OnComplete(() => 
                    {
                        if (b != null) b.SetActive(false);
                    });
                });
            }
        }
        
        
    }
}