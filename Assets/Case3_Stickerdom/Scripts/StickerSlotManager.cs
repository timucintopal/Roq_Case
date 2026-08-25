using UnityEngine;
using System.Collections.Generic;

namespace Case3_Stickerdom.Scripts
{
    public enum StickerType
    {
        Dog,
        Apple,
        PoliceCar
    }
    
    public class StickerSlotManager : MonoBehaviour
    {
        public static StickerSlotManager Instance;
        private List<StickerSlot> allSlots = new List<StickerSlot>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void RegisterSlot(StickerSlot slot)
        {
            if (!allSlots.Contains(slot)) allSlots.Add(slot);
        }

        public StickerSlot GetAvailableSlot(StickerType type)
        {
            foreach (var slot in allSlots)
            {
                if (slot.stickerType == type && !slot.isFilled)
                {
                    return slot;
                }
            }
            return null;
        }
    }
}
