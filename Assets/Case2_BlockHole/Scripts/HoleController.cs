using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Case2_BlockHole.Scripts
{
    public class HoleController : MonoBehaviour
    {
        [SerializeField] private Transform targetPos;
        [SerializeField] private Collider holeCollider;
        [SerializeField] private Renderer holeRenderer; // Hem MeshRenderer hem de SpriteRenderer destekler
        
        [Header("Tile Settings")]
        [SerializeField] private List<Transform> tiles = new List<Transform>();
        [SerializeField] private float tileHiddenY = -2f; // Oyun başı saklanacakları Y derinliği
        [SerializeField] private float tilePopDelay = 0.1f; // Tile'ların çıkış hızı (Meksika dalgası gecikmesi)
        
        [Header("Glow Settings")]
        [ColorUsage(true, true)] // Unity Inspector'da HDR (Işık patlaması) özelliğini açar
        [SerializeField] private Color glowColorStart = new Color(1f, 1f, 1f, 1f); // Sönük halindeki renk ve ışık şiddeti
        [ColorUsage(true, true)] 
        [SerializeField] private Color glowColorEnd = new Color(1f, 1f, 1f, 5f);   // Patlama anındaki renk ve ışık şiddeti
        [SerializeField] private float glowPulseDuration = 1.0f; // Bir nefes alış süresi
        
        public Hole.HoleColor currentColor;

        private Material _topGlowMaterial;
        private Tween _glowTween;
        private bool _isGlowing = false;
        private bool _isFilled = false; // Yuvaya obje oturdu mu?

        private void Awake()
        {
            // Eğer Inspector'dan atanmamışsa otomatik bulmaya çalış
            if (holeCollider == null) 
                holeCollider = GetComponent<Collider>();
                
            // Oyun başladığında tile'ları (fayansları) aşağıya (görünmez alana) gizle
            foreach (var tile in tiles)
            {
                if (tile != null)
                {
                    Vector3 pos = tile.localPosition;
                    pos.y = tileHiddenY;
                    tile.localPosition = pos;
                }
            }
        }

        public void StartGlow()
        {
            // Eğer zaten parlıyorsa veya yuva zaten doluysa tekrar başlatma
            if (_isGlowing)
            {
                Debug.Log($"[HoleController] {gameObject.name} zaten parlıyor, yeni istek reddedildi.");
                return;
            }
            if (_isFilled)
            {
                Debug.Log($"[HoleController] {gameObject.name} yuvası zaten dolu, parlamayacak.");
                return;
            }
            if (holeRenderer == null)
            {
                Debug.LogError($"[HoleController] {gameObject.name} objesinde 'Hole Renderer' (Mesh) ATANMAMIŞ! Parlama yapılamıyor!");
                return;
            }
            
            Debug.Log($"[HoleController] {gameObject.name} (Renk: {currentColor}) parlamaya BAŞLADI!");
            _isGlowing = true;

            Shader topFaceShader = Shader.Find("Custom/TopFaceGlow");
            if (topFaceShader != null)
            {
                if (_topGlowMaterial == null) _topGlowMaterial = new Material(topFaceShader);
                
                // Inspector'dan seçtiğimiz Başlangıç (Sönük) rengini atıyoruz
                _topGlowMaterial.SetColor("_GlowColor", glowColorStart);
                
                // Mevcut materyallerin üzerine şeffaf bir ışık (Additive) katmanı olarak ekliyoruz
                var mats = holeRenderer.materials;
                var newMats = new Material[mats.Length + 1];
                for (int i = 0; i < mats.Length; i++) newMats[i] = mats[i];
                newMats[mats.Length] = _topGlowMaterial;
                holeRenderer.materials = newMats;

                // Neon efekti
                _glowTween = _topGlowMaterial.DOColor(glowColorEnd, "_GlowColor", glowPulseDuration)
                    .SetEase(Ease.InOutQuad)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        public void StopGlow()
        {
            if (!_isGlowing) return;
            _isGlowing = false;

            if (_glowTween != null) _glowTween.Kill();
            
            // Ve eklediğimiz Glow materyalini listeden siliyoruz ki yuva tamamen normale dönsün
            if (holeRenderer != null && _topGlowMaterial != null)
            {
                var mats = holeRenderer.materials;
                if (mats.Length > 0 && mats[mats.Length - 1].shader.name == "Custom/TopFaceGlow")
                {
                    var newMats = new Material[mats.Length - 1];
                    for (int i = 0; i < newMats.Length; i++) newMats[i] = mats[i];
                    holeRenderer.materials = newMats;
                }
            }
        }

        public Transform Compare(Hole.HoleColor targetColor)
        {
            if(targetColor == currentColor)
            {
                _isFilled = true;

                // Renkler eşleştiğinde (obje yuvaya oturduğunda) deliğin collider'ını kapatıyoruz
                if (holeCollider != null) 
                    holeCollider.enabled = false;
                    
                // Yuvaya obje oturduğu için Glow efektini iptal ediyoruz
                StopGlow();
                    
                // 1.5 saniye sonra (Blok çukura ulaşıp, parçalanma başladıktan 1 saniye sonra)
                DOVirtual.DelayedCall(1.5f, () =>
                {
                    for (int i = 0; i < tiles.Count; i++)
                    {
                        if (tiles[i] != null)
                        {
                            Transform tile = tiles[i]; // Lambda içinde değişken kaybolmasın diye yakalıyoruz
                            // Meksika dalgası (tık tık tık) efekti
                            tile.DOLocalMoveY(0f, 0.4f).SetEase(Ease.OutBack).SetDelay(i * tilePopDelay)
                                .OnStart(() => tile.gameObject.SetActive(true));
                        }
                    }
                });
                    
                return targetPos;
            }
            return null;
        }
    }
}
