using UnityEngine;

namespace Case3_Stickerdom.Scripts
{
    public class StickerSlot : MonoBehaviour
    {
        public StickerType stickerType;
        public bool isFilled = false;
        
        void Start()
        {
            if (MSticker.Instance != null)
            {
                MSticker.Instance.RegisterSlot(this);
            }
        }
    }
}
