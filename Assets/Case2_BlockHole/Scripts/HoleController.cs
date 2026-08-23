using UnityEngine;

namespace Case2_BlockHole.Scripts
{
    public class HoleController : MonoBehaviour
    {
        [SerializeField] private Transform targetPos;
        [SerializeField] private Collider holeCollider;
        
        public Hole.HoleColor currentColor;

        private void Awake()
        {
            // Eğer Inspector'dan atanmamışsa otomatik bulmaya çalış
            if (holeCollider == null) 
                holeCollider = GetComponent<Collider>();
        }

        public Transform Compare(Hole.HoleColor targetColor)
        {
            if(targetColor == currentColor)
            {
                // Renkler eşleştiğinde (obje yuvaya oturduğunda) deliğin collider'ını kapatıyoruz
                if (holeCollider != null) 
                    holeCollider.enabled = false;
                    
                return targetPos;
            }
            return null;
        }
        
    }
}
