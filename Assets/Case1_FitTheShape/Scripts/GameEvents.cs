using System;
using UnityEngine;

namespace Case1_FitTheShape.Scripts
{
    public static class GameEvents
    {
        // 1. Input Etkileşimi (Ekrana dokunma/tıklama). MShapes vb. sistemler sayacını sıfırlamak için dinler.
        public static Action OnPlayerInteract;

        // 2. Şekillerin yuvaya uçmak için kendilerine uygun boş bir hedef istemesi. (ShapeController -> MDrum)
        public static Func<ShapeType, SegmentController> RequestMatchingSegment;
        
        // 3. Şekil yuvaya başarıyla oturduğunda tetiklenir (SegmentController -> MDrum)
        public static Action<SegmentController, ShapeType> OnSegmentFilled;
    }
}
