using UnityEngine;

namespace Case1_FitTheShape.Scripts
{
    public class SegmentController : MonoBehaviour
    {
        [SerializeField] private Shape shape;
        [SerializeField] private Transform hole;

        public bool SegmentCheck(ShapeType type)
        {
            return shape.Type == type;
        }
    }
}