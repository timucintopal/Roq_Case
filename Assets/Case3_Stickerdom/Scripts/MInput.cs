using UnityEngine;
using UnityEngine.InputSystem;

namespace Case3_Stickerdom.Scripts
{
    public class MInput : MonoBehaviour
    {
        private StickerController currentDraggedSticker;
        private Camera mainCam;

        void Start()
        {
            mainCam = Camera.main;
        }

        void Update()
        {
            if (Mouse.current == null) return;

            bool isPointerDown = Mouse.current.leftButton.wasPressedThisFrame;
            bool isPointerDrag = Mouse.current.leftButton.isPressed;
            bool isPointerUp = Mouse.current.leftButton.wasReleasedThisFrame;

            // Tıklama
            if (isPointerDown)
            {
                Vector3 mouseWorld = GetMouseWorldPos(0f); // Z=0 düzleminde arama yap
                Vector2 mousePos2D = new Vector2(mouseWorld.x, mouseWorld.y);
                
                RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
                
                if (hit.collider != null)
                {
                    StickerController sticker = hit.collider.GetComponent<StickerController>();
                    if (sticker != null && !sticker.IsPlaced)
                    {
                        currentDraggedSticker = sticker;
                        // Objeye özel derinlikte fare pozisyonunu tekrar al
                        Vector3 exactMouseWorld = GetMouseWorldPos(currentDraggedSticker.transform.position.z);
                        currentDraggedSticker.OnInputDown(exactMouseWorld);
                    }
                }
            }
            // Sürükleme
            else if (isPointerDrag && currentDraggedSticker != null)
            {
                Vector3 exactMouseWorld = GetMouseWorldPos(currentDraggedSticker.transform.position.z);
                currentDraggedSticker.OnInputDrag(exactMouseWorld);
            }
            // Bırakma
            else if (isPointerUp && currentDraggedSticker != null)
            {
                currentDraggedSticker.OnInputUp();
                currentDraggedSticker = null;
            }
        }

        private Vector3 GetMouseWorldPos(float objectZ)
        {
            if (Mouse.current == null) return Vector3.zero;

            Vector2 rawMousePos = Mouse.current.position.ReadValue();
            Vector3 mousePos = new Vector3(rawMousePos.x, rawMousePos.y, 0f);
            
            // Kamera Z ile Obje Z arasındaki farkı bul (Perspektif için doğru hizalama)
            mousePos.z = Mathf.Abs(mainCam.transform.position.z - objectZ);
            return mainCam.ScreenToWorldPoint(mousePos);
        }
    }
}
