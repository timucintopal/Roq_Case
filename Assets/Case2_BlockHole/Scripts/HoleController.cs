using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Case2_BlockHole.Scripts
{
    public class HoleController : MonoBehaviour
    {
        [SerializeField] private Transform targetPos;
        [SerializeField] private Collider holeCollider;
        
        [Header("Tile Settings")]
        [SerializeField] private List<Transform> tiles = new List<Transform>();
        [SerializeField] private float tileHiddenY = -2f; // Oyun başı saklanacakları Y derinliği
        [SerializeField] private float tilePopDelay = 0.1f; // Tile'ların çıkış hızı (Meksika dalgası gecikmesi)
        
        public Hole.HoleColor currentColor;

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

        public Transform Compare(Hole.HoleColor targetColor)
        {
            if(targetColor == currentColor)
            {
                // Renkler eşleştiğinde (obje yuvaya oturduğunda) deliğin collider'ını kapatıyoruz
                if (holeCollider != null) 
                    holeCollider.enabled = false;
                    
                // 1.5 saniye sonra (Blok çukura ulaşıp, parçalanma başladıktan 1 saniye sonra)
                DOVirtual.DelayedCall(1.5f, () =>
                {
                    for (int i = 0; i < tiles.Count; i++)
                    {
                        if (tiles[i] != null)
                        {
                            Transform tile = tiles[i]; // Lambda içinde değişken kaybolmasın diye yakalıyoruz
                            // Meksika dalgası (tık tık tık) efekti:
                            // Her obje bir öncekinden 'tilePopDelay' saniye kadar sonra fırlar. 
                            // OnStart ile de tam fırlayacağı an objeyi aktif hale getiririz ki aşağıda beklerken gözükmesinler.
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
