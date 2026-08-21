using UnityEngine;
using UnityEngine.InputSystem;

namespace Case3_Stickerdom.Scripts
{
    public class MInput : MonoBehaviour
    {
        private StickerController _currentDraggedSticker;
        private Camera _mainCam;

        void Start()
        {
            _mainCam = Camera.main;
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
                        _currentDraggedSticker = sticker;
                        // Objeye özel derinlikte fare pozisyonunu tekrar al
                        Vector3 exactMouseWorld = GetMouseWorldPos(_currentDraggedSticker.transform.position.z);
                        _currentDraggedSticker.OnInputDown(exactMouseWorld);
                    }
                }
            }
            // Sürükleme
            else if (isPointerDrag && _currentDraggedSticker != null)
            {
                Vector3 exactMouseWorld = GetMouseWorldPos(_currentDraggedSticker.transform.position.z);
                _currentDraggedSticker.OnInputDrag(exactMouseWorld);
            }
            // Bırakma
            else if (isPointerUp && _currentDraggedSticker != null)
            {
                _currentDraggedSticker.OnInputUp();
                _currentDraggedSticker = null;
            }
        }

        private Vector3 GetMouseWorldPos(float objectZ)
        {
            if (Mouse.current == null) return Vector3.zero;

            Vector2 rawMousePos = Mouse.current.position.ReadValue();
            Vector3 mousePos = new Vector3(rawMousePos.x, rawMousePos.y, 0f);
            
            // Kamera Z ile Obje Z arasındaki farkı bul (Perspektif için doğru hizalama)
            mousePos.z = Mathf.Abs(_mainCam.transform.position.z - objectZ);
            return _mainCam.ScreenToWorldPoint(mousePos);
        }
    }
}
