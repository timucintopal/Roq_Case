using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class BallSlingshot : MonoBehaviour
{
    [Header("Slingshot Settings")]
    [Tooltip("Çekme mesafesini güce dönüştüren katsayı.")]
    public float powerMultiplier = 10f;
    [Tooltip("Maksimum çekme mesafesi sınırı.")]
    public float maxDragDistance = 5f;
    [Tooltip("Atışın gerçekleşmesi ve çizginin görünmesi için gereken minimum çekme mesafesi (Ölü Alan).")]
    public float minDragDistance = 0.5f;
    [Tooltip("Top hareket halindeyken tekrar atış yapılabilir mi?")]
    public bool canShootWhileMoving = false;

    [Header("Line Renderer Settings")]
    public LineRenderer lineRenderer;
    [Tooltip("Çizginin merkezden olan uzaklığı (Yarıçap). Çizgi diskin etrafında döner.")]
    public float lineStartRadius = 0.5f;
    [Tooltip("Çizgilerin Y eksenindeki yüksekliği (Yere değmemesi için).")]
    public float lineHeightOffset = 0.5f;
    [Tooltip("Çizginin en kalın hali (kısa çekildiğinde).")]
    public float lineMaxWidth = 0.5f;
    [Tooltip("Çizginin en ince hali (uzun çekildiğinde).")]
    public float lineMinWidth = 0.1f;
    [Tooltip("Çizginin bitiş noktasına doğru ne kadar inceldiği (0 = iğne gibi sivri, 1 = başlangıçla aynı kalınlık).")]
    [Range(0f, 1f)]
    public float lineEndWidthMultiplier = 0.2f;

    [Header("Visual Effects")]
    [Tooltip("Sürükleme sırasında diskin etrafında çıkacak olan beyaz halka objesi.")]
    public GameObject dragRingObj;

    [Header("Respawn Settings")]
    [Tooltip("Disk durduktan kaç saniye sonra respawn olsun?")]
    public float respawnIdleTime = 1f;

    private Rigidbody rb;
    private bool isDragging = false;
    private Vector3 dragStartPos;
    private Plane dragPlane;
    private Camera mainCamera;
    
    private bool hasHitCube = false;
    private bool isLaunched = false;
    private float idleTimer = 0f;
    private float launchTime = 0f;
    private MDisk mDiskRef;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        
        mDiskRef = FindObjectOfType<MDisk>();
        if (mDiskRef == null)
        {
            Debug.LogWarning("MDisk scripti sahnede bulunamadı! 'MDisk' adlı objeyi isimden arıyorum...");
            GameObject mDiskObj = GameObject.Find("MDisk");
            if (mDiskObj != null)
            {
                mDiskRef = mDiskObj.AddComponent<MDisk>();
            }
        }
        
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 2;
        }

        if (dragRingObj != null)
        {
            dragRingObj.transform.localPosition = new Vector3(0, lineHeightOffset, 0);
            dragRingObj.SetActive(false);
        }
    }

    private void Update()
    {
        if (Pointer.current == null) return;

        // --- Respawn & Idle Mantığı ---
        bool isMoving = rb.linearVelocity.sqrMagnitude > 0.05f;
        
        // Fırlatıldıktan sonra en az 0.5 saniye geçmesini bekleyelim ki fizik motoru hızı tam işlesin
        if (!isMoving && !isDragging && isLaunched && (Time.time - launchTime > 0.5f))
        {
            if (!hasHitCube)
            {
                // Hiçbir küpe değmeden durduysa anında respawn
                RespawnAtMDisk();
            }
            else
            {
                // Küpe değdi ve durdu, etkileşim yoksa sayacı başlat
                idleTimer += Time.deltaTime;
                if (idleTimer >= respawnIdleTime)
                {
                    RespawnAtMDisk();
                }
            }
        }
        else if (isMoving || isDragging)
        {
            idleTimer = 0f;
        }
        // -----------------------------

        // 1. TIKLAMA BAŞLANGICI
        if (Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 pointerPos = Pointer.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(pointerPos);

            // Tıklanan yer bu obje mi diye raycast atıyoruz
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject == this.gameObject)
                {
                    // Eğer top hareket ediyorsa ve hareket halindeyken atışa izin yoksa iptal et
                    if (!canShootWhileMoving && rb.linearVelocity.sqrMagnitude > 0.1f)
                        return;

                    isDragging = true;
                    dragStartPos = transform.position;
                    // Topun merkezinden geçen, yukarıya (Y) bakan bir düzlem oluşturuyoruz
                    dragPlane = new Plane(Vector3.up, dragStartPos);

                    // Çizgiyi anında göstermiyoruz, sürükleyip ölü alanı geçmesini bekliyoruz
                    if (lineRenderer != null)
                    {
                        lineRenderer.enabled = false;
                    }
                    if (dragRingObj != null)
                    {
                        dragRingObj.SetActive(true);
                    }
                }
            }
        }

        // 2. SÜRÜKLEME DEVAM EDİYOR
        if (Pointer.current.press.isPressed && isDragging)
        {
            Vector3 currentPointerPos = GetPointerPositionOnPlane();
            
            // Çekme vektörünü hesapla (topun merkezi -> farenin/parmağın pozisyonu)
            Vector3 dragVector = currentPointerPos - dragStartPos;
            
            // Ölü alan kontrolü (Deadzone)
            if (dragVector.magnitude < minDragDistance)
            {
                if (lineRenderer != null) lineRenderer.enabled = false;
            }
            else
            {
                if (lineRenderer != null) lineRenderer.enabled = true;

                // Mesafeyi maxDragDistance ile sınırlandır
                if (dragVector.magnitude > maxDragDistance)
                {
                    dragVector = dragVector.normalized * maxDragDistance;
                }

                Vector3 clampedPointerPos = dragStartPos + dragVector;

                if (lineRenderer != null)
                {
                    UpdateLineRenderer(clampedPointerPos);
                }
            }
        }

        // 3. TIKLAMA BIRAKILDI
        if (Pointer.current.press.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;

            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
            if (dragRingObj != null)
            {
                dragRingObj.SetActive(false);
            }

            Vector3 currentPointerPos = GetPointerPositionOnPlane();
            Vector3 dragVector = currentPointerPos - dragStartPos;

            // Atışı sadece ölü alanı (minDragDistance) geçtiysek yap
            if (dragVector.magnitude >= minDragDistance)
            {
                if (dragVector.magnitude > maxDragDistance)
                {
                    dragVector = dragVector.normalized * maxDragDistance;
                }

                // Çekilen yönün tersine güç uyguluyoruz
                Vector3 force = -dragVector * powerMultiplier;
                rb.AddForce(force, ForceMode.Impulse);
                
                isLaunched = true;
                hasHitCube = false;
                launchTime = Time.time;
            }
        }
    }

    private Vector3 GetPointerPositionOnPlane()
    {
        Vector2 pointerPos = Pointer.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(pointerPos);
        
        if (dragPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        return dragStartPos;
    }

    private void UpdateLineRenderer(Vector3 currentPointerPos)
    {
        // Fare/parmak yönünü hesapla
        Vector3 dragDirection = (currentPointerPos - dragStartPos);
        dragDirection.y = 0; // Sadece X ve Z ekseninde (yatayda) dönüş istiyoruz
        
        // Merkezden farenin olduğu yöne doğru 'yarıçap' kadar git
        Vector3 dynamicStartPos = dragStartPos;
        if (dragDirection.sqrMagnitude > 0.001f)
        {
            dynamicStartPos += dragDirection.normalized * lineStartRadius;
        }
        dynamicStartPos.y += lineHeightOffset; // Yerden kaldır

        Vector3 dynamicEndPos = currentPointerPos;
        dynamicEndPos.y += lineHeightOffset; // Yerden kaldır

        lineRenderer.SetPosition(0, dynamicStartPos);
        lineRenderer.SetPosition(1, dynamicEndPos);

        float currentDistance = Vector3.Distance(dragStartPos, currentPointerPos);
        float distanceRatio = Mathf.Clamp01(currentDistance / maxDragDistance);
        
        float currentWidth = Mathf.Lerp(lineMaxWidth, lineMinWidth, distanceRatio);
        lineRenderer.startWidth = currentWidth;
        lineRenderer.endWidth = currentWidth * lineEndWidthMultiplier;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Attached Rigidbody üzerinden kontrol ediyoruz (script genelde rb ile aynı yerdedir)
        if (collision.rigidbody != null)
        {
            Case4_Buca.Scripts.Cube hitCube = collision.rigidbody.GetComponent<Case4_Buca.Scripts.Cube>();
            
            if (hitCube != null)
            {
                if (!hasHitCube) 
                {
                    Debug.Log("Disk bir Kübe ÇARPTI! hasHitCube = true oldu.");
                    hasHitCube = true;
                }
            }
        }
    }

    private void RespawnAtMDisk()
    {
        if (mDiskRef != null)
        {
            Debug.Log("Respawn tetiklendi! Disk MDisk'in konumuna ışınlanıyor.");
            // Fizik motorunun çakışmasını önlemek için pozisyon atamasından önce fiziği uyutabiliriz
            rb.Sleep(); 
            transform.position = mDiskRef.transform.position;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            hasHitCube = false;
            isLaunched = false;
            idleTimer = 0f;
            
            TrailRenderer tr = GetComponentInChildren<TrailRenderer>();
            if (tr != null) tr.Clear();
            
            if (dragRingObj != null) dragRingObj.SetActive(false);
            
            rb.WakeUp();
        }
        else
        {
            Debug.LogError("Respawn tetiklendi fakat MDisk referansı NULL! Sahnede MDisk scriptine sahip bir obje yok.");
        }
    }
}
