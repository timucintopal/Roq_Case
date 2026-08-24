using UnityEngine;

namespace Case2_BlockHole.Scripts
{
    [CreateAssetMenu(fileName = "New Block Theme", menuName = "Case2 / Block Theme Data", order = 1)]
    public class BlockThemeData : ScriptableObject
    {
        [Header("Hole Glow Settings")]
        [ColorUsage(true, true)]
        public Color glowColorStart = new Color(1f, 1f, 1f, 1f);
        
        [ColorUsage(true, true)]
        public Color glowColorEnd = new Color(1f, 1f, 1f, 5f);
        
        [Header("Particle Flow Settings (Dust & Cloud)")]
        [Tooltip("Ana tozun (DustDirtyPoofSoft) rastgele seçebileceği 1. Renk")]
        public Color dustColor1 = Color.white;
        
        [Tooltip("Ana tozun rastgele seçebileceği 2. Renk")]
        public Color dustColor2 = Color.gray;
        
        [Tooltip("İç bulutun (Cloud - 0. index child) rastgele seçebileceği 1. Renk")]
        public Color cloudColor1 = Color.white;
        
        [Tooltip("İç bulutun rastgele seçebileceği 2. Renk")]
        public Color cloudColor2 = Color.gray;

        [Header("Glow Particle Settings (GlowFlash)")]
        [Tooltip("Tozun 1. indexli child'ı (GlowFlash) için uygulanacak renk/şeffaflık geçişi.")]
        public Gradient particleGlowGradient;
    }
}
