using UnityEngine;

namespace Case2_BlockHole.Scripts
{
    public class HoleController : MonoBehaviour
    {
        [SerializeField] private Transform targetPos;
        
        public Hole.HoleColor currentColor;

        public Transform Compare(Hole.HoleColor targetColor)
        {
            if(targetColor == currentColor)
                return targetPos;
            return null;
        }
        
    }
}
