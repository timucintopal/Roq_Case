using System.Collections.Generic;
using UnityEngine;

namespace Case2_BlockHole.Scripts
{
    public class MHole : MonoBehaviour
    {
        public static MHole Instance { get; private set; }
        
        private List<HoleController> _holes = new List<HoleController>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void RegisterHole(HoleController hole)
        {
            if (!_holes.Contains(hole)) _holes.Add(hole);
        }

        public void UnregisterHole(HoleController hole)
        {
            if (_holes.Contains(hole)) _holes.Remove(hole);
        }

        // Seçilen renge ait Hole'un parlamasını başlatır, diğerlerini kapatır
        public void HighlightHole(Hole.HoleColor color)
        {
            foreach (var hole in _holes)
            {
                if (hole.currentColor == color)
                    hole.StartGlow();
                else
                    hole.StopGlow();
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
