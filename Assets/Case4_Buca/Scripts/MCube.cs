using System;
using UnityEngine;

namespace Case4_Buca.Scripts
{
    public class MCube : MonoBehaviour
    {
        public static MCube Instance { get; private set; }

        public Action OnAllCubesHit; // Tüm küpler vurulduğunda tetiklenecek olay (Event)

        [Header("Cube Tracking")]
        public int totalCubes = 0;
        public int hitCubes = 0;

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Oyun başlarken küplerin kendini buraya kaydetmesi için
        public void RegisterCube()
        {
            totalCubes++;
        }

        // Bir küp vurulduğunda bu fonksiyon çağırılır
        public void CubeHit()
        {
            hitCubes++;

            // Eğer vurulan küp sayısı toplam sayıya ulaştıysa (veya geçtiyse)
            if (hitCubes >= totalCubes && totalCubes > 0)
            {
                // Sinyali yay! (Abonelere haber ver)
                OnAllCubesHit?.Invoke();
            }
        }
    }
}
