using System.Collections.Generic;
using UnityEngine;

namespace Case2_BlockHole.Scripts
{
    public class MHole : MonoBehaviour
    {
        public static MHole Instance { get; private set; }
        
        [Header("Manual Hole Assignment")]
        [Tooltip("Sahnedeki tüm delikleri buraya sürükleyip bırakabilirsiniz.")]
        [SerializeField] private List<HoleController> _holes = new List<HoleController>();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void OnEnable()
        {
            BlockController.OnAnyBlockPickedUp += HighlightHole;
            BlockController.OnAnyBlockDropped += StopAllGlows;
        }

        private void OnDisable()
        {
            BlockController.OnAnyBlockPickedUp -= HighlightHole;
            BlockController.OnAnyBlockDropped -= StopAllGlows;
        }

        // Seçilen renge ait Hole'un parlamasını başlatır, diğerlerini kapatır
        public void HighlightHole(Hole.HoleColor color)
        {
            foreach (var hole in _holes)
            {
                if (hole.currentColor == color)
                {
                    hole.StartGlow();
                }
                else
                {
                    hole.StopGlow();
                }
            }
        }

        // Bırakıldığında tüm parlamaları kapatır
        public void StopAllGlows()
        {
            foreach (var hole in _holes)
            {
                hole.StopGlow();
            }
        }
    }
}
