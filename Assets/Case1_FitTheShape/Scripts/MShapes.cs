using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Case1_FitTheShape.Scripts
{
    public class MShapes : MonoBehaviour
    {
        [Tooltip("Inspector'dan sahnedeki tıklanabilir buton şekillerini atayın.")]
        [SerializeField] private List<ShapeController> activeShapes;
        
        [Tooltip("Kaç saniye boşta kalınca ipucu verilsin?")]
        [SerializeField] private float hintDelay = 2.0f;

        private float _idleTimer = 0f;

        void Update()
        {
            // Eğer oyuncu ekrana dokunursa (veya fareye tıklarsa) sayacı sıfırla
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                _idleTimer = 0f;
                return;
            }

            _idleTimer += Time.deltaTime;

            if (_idleTimer >= hintDelay)
            {
                _idleTimer = 0f;
                ShowHint();
            }
        }

        private void ShowHint()
        {
            // Atanan şekillerden eşleşmesi olan İLK bulduğumuzu oynat
            foreach (var shape in activeShapes)
            {
                if (shape == null) continue;
                
                // Şekil o an havalanmıyorsa ve eşleşebileceği bir boşluk varsa
                if (!shape.IsMoving && shape.HasAvailableMatch())
                {
                    shape.PlayHintAnimation();
                    break; // Sadece TEK bir şekle ipucu ver ve döngüden çık
                }
            }
        }
    }
}
