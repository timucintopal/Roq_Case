using UnityEngine;
using UnityEngine.InputSystem;

namespace Case2_BlockHole.Scripts
{
    public class MInput : MonoBehaviour
    {
        [Header("Layer Settings")]
        [Tooltip("Sürüklenecek objelerin layer'ı (Örn: 'Draggable')")]
        [SerializeField] private LayerMask draggableLayer;
        [Tooltip("Zemin olarak kabul edilecek oyun alanının layer'ı (Örn: 'Ground')")]
        [SerializeField] private LayerMask groundLayer;
        [Tooltip("Bırakıldığında deliği algılamak için kullanılacak layer")]
        [SerializeField] private LayerMask holeLayer;

        [Header("Drag Settings")]
        [Tooltip("Objenin oyun alanı üzerindeki yüksekliği")]
        [SerializeField] private float yOffset = 0.5f;
        [Tooltip("Sürüklenme hassasiyeti (yumuşaklığı)")]
        [SerializeField] private float moveSpeed = 25f;

        [SerializeField] BlockController currentBlockController;
        private Camera _mainCamera;
        private Transform _draggedObject;
        private bool _isDragging;
        private Vector3 _offset;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            // Eğer aktif bir işaretçi (mouse, dokunmatik ekran) yoksa kod çalışmasın
            if (Pointer.current == null) return;

            if (Pointer.current.press.wasPressedThisFrame)
            {
                Vector2 pointerPos = Pointer.current.position.ReadValue();
                Ray ray = _mainCamera.ScreenPointToRay(pointerPos);
                
                // 1. Önce sürüklenebilir bir objeye tıklayıp tıklamadığımızı kontrol ediyoruz
                if (Physics.Raycast(ray, out RaycastHit hitObj, 100f, draggableLayer))
                {
                    _draggedObject = hitObj.transform;
                    _isDragging = true;
                    
                    Debug.Log(_draggedObject.name);

                    currentBlockController = hitObj.collider.attachedRigidbody.GetComponent<BlockController>();
                    
                    // 2. Tıklanan noktanın zemindeki karşılığını buluyoruz (offset hesaplamak için)
                    if (Physics.Raycast(ray, out RaycastHit hitGround, 100f, groundLayer))
                    {
                        // Sürüklerken objenin fareye aniden atlamaması için aradaki mesafeyi kaydediyoruz
                        _offset = _draggedObject.position - hitGround.point;
                        _offset.y = 0; // Sadece X ve Z'deki farkı koruyoruz
                        
                        
                    }
                    else
                    {
                        _offset = Vector3.zero;
                    }
                }
            }

            if (Pointer.current.press.isPressed && _isDragging && _draggedObject != null)
            {
                Vector2 pointerPos = Pointer.current.position.ReadValue();
                Ray ray = _mainCamera.ScreenPointToRay(pointerPos);
                
                // 3. İşaretçinin zemindeki yeni konumunu buluyoruz
                if (Physics.Raycast(ray, out RaycastHit hitGround, 100f, groundLayer))
                {
                    Vector3 targetPosition = hitGround.point + _offset;
                    targetPosition.y = yOffset; // Yüksekliği sabitliyoruz

                    // Objeyi yumuşak bir şekilde yeni pozisyona taşıyoruz
                    _draggedObject.position = Vector3.Lerp(_draggedObject.position, targetPosition, Time.deltaTime * moveSpeed);
                }
            }

            if (Pointer.current.press.wasReleasedThisFrame)
            {
                if (_draggedObject != null)
                {
                    // Aşağıya (-y yönünde) bir ışın gönderip Hole layer'ındaki bir objeye çarpıyor mu kontrol et
                    if (Physics.Raycast(_draggedObject.position, Vector3.down, out RaycastHit holeHit, 10f, holeLayer))
                    {
                        Debug.Log("Hole bulundu: " + holeHit.transform.name);
                    }
                }

                _isDragging = false;
                _draggedObject = null;
                currentBlockController = null;
            }
        }
    }
}