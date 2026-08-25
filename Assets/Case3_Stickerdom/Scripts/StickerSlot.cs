using UnityEngine;

namespace Case3_Stickerdom.Scripts
{
    public class StickerSlot : MonoBehaviour
    {
        public StickerType stickerType;
        public bool isFilled = false;
        
        void Start()
        {
            if (StickerSlotManager.Instance != null)
            {
                StickerSlotManager.Instance.RegisterSlot(this);
            }
        }
    }
}
