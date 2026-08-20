using System.Collections.Generic;
using UnityEngine;

namespace Case1_FitTheShape.Scripts
{
    public class MDrum : MonoBehaviour
    {
        [SerializeField] private List<SegmentController> segments = new List<SegmentController>();
        
        [SerializeField] private List<SegmentController> selectedSegments = new List<SegmentController>();

        private void Awake()
        {
            GatherSegments();
        }

        // Unity editöründe scriptin yanındaki 3 noktaya (...) basıp "Gather Segments" diyerek
        // manuel olarak da listeyi doldurabilirsiniz.
        [ContextMenu("Gather Segments")]
        public void GatherSegments()
        {
            // Tüm alt objelerdeki SegmentController'ları bulup listeye çeviriyoruz.
            segments = new List<SegmentController>(GetComponentsInChildren<SegmentController>());
        }
    }
}
