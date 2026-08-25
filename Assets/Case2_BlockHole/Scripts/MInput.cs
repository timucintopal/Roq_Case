using DG.Tweening;
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
        [Tooltip("Zemin algılama (Raycast) maksimum mesafesi")]
        [SerializeField] private float raycastDistance = 100f;
        [Tooltip("Yanlış yere bırakıldığında geri dönme animasyonunun süresi")]
        [SerializeField] private float dropReturnDuration = 0.3f;

        [Header("Drag Settings")]
        [Tooltip("Objenin oyun alanı üzerindeki yüksekliği")]
        [SerializeField] private float yOffset = 0.5f;
        [Tooltip("Fat Finger Koruması: Parmağın objeyi kapatmaması için objenin Z ekseninde (ileri) ne kadar itileceği")]
        [SerializeField] private float dragForwardOffset = 1.5f;
        [Tooltip("Sürüklenme hassasiyeti (yumuşaklığı)")]
        [SerializeField] private float moveSpeed = 25f;

        [Header("Sway (Sallantı) Settings")]
        [Tooltip("Harekete göre ne kadar eğileceği çarpanı")]
        [SerializeField] private float swayMultiplier = 1.5f;
        [Tooltip("Eğilme açısının çıkabileceği maksimum sınır (Derece)")]
        [SerializeField] private float maxSwayAngle = 30f;
        [Tooltip("Eğilme ve geri toparlanma yumuşaklığı")]
        [SerializeField] private float swaySmoothness = 10f;

        [SerializeField] BlockController currentBlockController;
        private Camera _mainCamera;
        private Transform _draggedObject;
        private bool _isDragging;
        private Vector3 _initialDragPos;
        private Vector3 _lastFramePos;
        private Vector3 _currentDragPosition; // Fizik motoru senkronizasyon hatasını önlemek için içerde tutuyoruz
        private Quaternion _currentSwayRotation;

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
                if (Physics.Raycast(ray, out RaycastHit hitObj, raycastDistance, draggableLayer))
                {
                    _draggedObject = hitObj.transform;
                    _initialDragPos = _draggedObject.position; // Tutulduğu ilk yeri kaydet
                    _lastFramePos = _initialDragPos; // Hız ölçümü için ilk konumu al
                    _isDragging = true;
                    

                    currentBlockController = hitObj.collider.attachedRigidbody.GetComponent<BlockController>();
                    if (currentBlockController != null) 
                    {
                        currentBlockController.OnPickup(); // Eventleri kendi içinde fırlatacak
                    }
                    
                    // Obje tutulduğu an (Lerp gecikmesi olmadan) direkt farenin merkezine (Pivot'a) şak diye oturt!
                    if (Physics.Raycast(ray, out RaycastHit hitGround, raycastDistance, groundLayer))
                    {
                        Vector3 instantPos = hitGround.point;
                        instantPos.y = yOffset;
                        instantPos.z += dragForwardOffset; // İleri ofseti buradan da hesapla
                        
                        _currentDragPosition = instantPos;
                        _currentSwayRotation = _draggedObject.rotation;
                        _lastFramePos = instantPos; // Titremeyi önlemek için ilk karedeki pozisyonu güncelle
                        
                        if (currentBlockController != null && currentBlockController.Rb != null)
                        {
                            currentBlockController.Rb.MovePosition(instantPos);
                        }
                        else
                        {
                            _draggedObject.position = instantPos;
                        }
                    }
                }
            }

            if (Pointer.current.press.isPressed && _isDragging && _draggedObject != null)
            {
                Vector2 pointerPos = Pointer.current.position.ReadValue();
                Ray ray = _mainCamera.ScreenPointToRay(pointerPos);
                
                // İşaretçinin zemindeki yeni konumunu buluyoruz
                if (Physics.Raycast(ray, out RaycastHit hitGround, raycastDistance, groundLayer))
                {
                    // 2. ADIM: Objenin X'i parmağa tam ortalanıyor. 
                    // Y (Yükseklik) ve Z (İleri İtme) eksenlerine sabit ofsetler ekleniyor.
                    Vector3 targetPosition = hitGround.point;
                    targetPosition.y = yOffset; // Yüksekliği sabitliyoruz
                    targetPosition.z += dragForwardOffset; // Fat Finger: Parmağın objeyi kapatmaması için ileri itiyoruz

                    // 3. ADIM (Lerp ile Uçuş): Obje aniden ışınlanmak yerine bu yeni targetPosition'a yumuşakça süzülür.
                    _currentDragPosition = Vector3.Lerp(_currentDragPosition, targetPosition, Time.deltaTime * moveSpeed);
                    
                    // Procedural Sway (Hıza göre eğilme) Hesaplaması
                    Vector3 velocity = (_currentDragPosition - _lastFramePos) / Time.deltaTime;
                    _lastFramePos = _currentDragPosition; // Bir sonraki kare için konumu kaydet

                    // Hızın eksenlerine göre hedef dönüş açısını belirle
                    float targetRotX = Mathf.Clamp(velocity.z * swayMultiplier, -maxSwayAngle, maxSwayAngle);
                    float targetRotZ = Mathf.Clamp(-velocity.x * swayMultiplier, -maxSwayAngle, maxSwayAngle);
                    Quaternion targetRotation = Quaternion.Euler(targetRotX, 0, targetRotZ);
                    
                    // Rotasyonu da Transform'dan okumadan hesaplıyoruz ki fizik motoru gecikmesinden etkilenmesin
                    _currentSwayRotation = Quaternion.Lerp(_currentSwayRotation, targetRotation, Time.deltaTime * swaySmoothness);
                    
                    // Fiziksel objeleri (Rigidbody) Transform.position ile taşımak kasmaya sebep olur. MovePosition kullanıyoruz.
                    if (currentBlockController != null && currentBlockController.Rb != null)
                    {
                        currentBlockController.Rb.MovePosition(_currentDragPosition);
                        currentBlockController.Rb.MoveRotation(_currentSwayRotation);
                    }
                    else
                    {
                        _draggedObject.position = _currentDragPosition;
                        _draggedObject.rotation = _currentSwayRotation;
                    }
                }
            }

            if (Pointer.current.press.wasReleasedThisFrame)
            {
                if (_draggedObject != null)
                {
                    bool isSuccess = false;
                    
                    // Aşağıya (-y yönünde) bir ışın gönderip Hole layer'ındaki bir objeye çarpıyor mu kontrol et
                    if (Physics.Raycast(_draggedObject.position, Vector3.down, out RaycastHit holeHit, 10f, holeLayer))
                    {
                        var targetHole = holeHit.transform.GetComponent<HoleController>();
                        if (targetHole != null)
                        {
                            var targetTransform = targetHole.TryInsertBlock(currentBlockController.holeColor);
                            if (targetTransform != null)
                            {
                                currentBlockController.MoveToHole(targetTransform);
                                isSuccess = true;
                            }
                        }
                    }

                    // Eğer boşluğa veya yanlış deliğe bırakıldıysa, ilk aldığı yere geri dönsün
                    if (!isSuccess)
                    {
                        var returnedBlock = currentBlockController; // Lambda için saklıyoruz
                        _draggedObject.DOMove(_initialDragPos, dropReturnDuration).SetEase(Ease.OutQuad)
                            .OnComplete(() => 
                            {
                                if (returnedBlock != null) returnedBlock.RestorePhysics();
                            });
                    }
                    else
                    {
                        // Başarılı girişse objenin fiziği kalıcı olarak kapalı kalsın (uzaya uçmasın)
                        currentBlockController.Rb.isKinematic = true;
                    }
                    
                    // Bırakıldığında (ister deliğe ister boşluğa), yamuk kaldıysa düz (0,0,0) konumuna geri dönsün
                    _draggedObject.DORotate(Vector3.zero, dropReturnDuration).SetEase(Ease.OutQuad);
                }

                _isDragging = false;
                _draggedObject = null;
                
                if (currentBlockController != null) 
                {
                    currentBlockController.OnDrop(); // Eventleri kendi fırlatacak, MHole burayı ilgilendirmiyor
                    currentBlockController = null;
                }
            }
        }
    }
}