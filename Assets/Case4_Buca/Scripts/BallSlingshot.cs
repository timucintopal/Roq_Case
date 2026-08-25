using UnityEngine;
using DG.Tweening;
using Shapes;

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

    [Header("Line Settings")]
    public Shapes.Line dragLine;
    [Tooltip("Çizginin merkezden olan uzaklığı (Yarıçap). Çizgi diskin etrafında döner.")]
    public float lineStartRadius = 0.5f;
    [Tooltip("Çizgilerin Y eksenindeki yüksekliği (Yere değmemesi için).")]
    public float lineHeightOffset = 0.5f;
    [Tooltip("Çizginin en kalın hali (kısa çekildiğinde).")]
    public float lineMaxWidth = 0.5f;
    [Tooltip("Çizginin en ince hali (uzun çekildiğinde).")]
    public float lineMinWidth = 0.1f;

    [Header("Visual Effects")]
    [Tooltip("Sürükleme sırasında diskin etrafında çıkacak olan beyaz halka objesi.")]
    public GameObject dragRingObj;
    [Tooltip("Atış yapıldıktan sonra lastiğin çarpma animasyon süresi (Aynı zamanda fırlatma gecikmesi).")]
    public float snapAnimationDuration = 0.1f;
    [Tooltip("Lastik diske çarptıktan sonra fırlatmadan önce beklenecek ekstra süre.")]
    public float postSnapDelay = 0.05f;

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
        
        if (dragLine != null)
        {
            dragLine.enabled = false;
        }

        if (dragRingObj != null)
        {
            dragRingObj.transform.localPosition = new Vector3(0, lineHeightOffset, 0);
            dragRingObj.transform.localScale = Vector3.zero;
            dragRingObj.SetActive(false);
        }
    }

    private void Start()
    {
        if (Case4_Buca.Scripts.MInput.Instance != null)
        {
            Case4_Buca.Scripts.MInput.Instance.OnPointerDownEvent += HandlePointerDown;
            Case4_Buca.Scripts.MInput.Instance.OnPointerDragEvent += HandlePointerDrag;
            Case4_Buca.Scripts.MInput.Instance.OnPointerUpEvent += HandlePointerUp;
        }
        else
        {
            Debug.LogError("MInput.Instance bulunamadı! Inputlar çalışmayacak.");
        }
    }

    private void OnDestroy()
    {
        if (Case4_Buca.Scripts.MInput.Instance != null)
        {
            Case4_Buca.Scripts.MInput.Instance.OnPointerDownEvent -= HandlePointerDown;
            Case4_Buca.Scripts.MInput.Instance.OnPointerDragEvent -= HandlePointerDrag;
            Case4_Buca.Scripts.MInput.Instance.OnPointerUpEvent -= HandlePointerUp;
        }
    }

    private void Update()
    {
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
    }

    private void HandlePointerDown(Vector2 pointerPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(pointerPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == this.gameObject)
            {
                if (!canShootWhileMoving && rb.linearVelocity.sqrMagnitude > 0.1f)
                    return;

                isDragging = true;
                dragStartPos = transform.position;
                dragPlane = new Plane(Vector3.up, dragStartPos);

                if (dragLine != null) dragLine.enabled = false;
                
                if (dragRingObj != null)
                {
                    dragRingObj.transform.DOKill();
                    dragRingObj.SetActive(true);
                    dragRingObj.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                }
            }
        }
    }

    private void HandlePointerDrag(Vector2 pointerPos)
    {
        if (!isDragging) return;

        Vector3 currentPointerPos = GetPointerPositionOnPlane(pointerPos);
        Vector3 dragVector = currentPointerPos - dragStartPos;
        
        if (dragVector.magnitude < minDragDistance)
        {
            if (dragLine != null) dragLine.enabled = false;
        }
        else
        {
            if (dragLine != null) dragLine.enabled = true;
            dragVector = Vector3.ClampMagnitude(dragVector, maxDragDistance);
            Vector3 clampedPointerPos = dragStartPos + dragVector;
            if (dragLine != null) UpdateLine(clampedPointerPos);
        }
    }

    private void HandlePointerUp(Vector2 pointerPos)
    {
        if (!isDragging) return;
        isDragging = false;

        Vector3 currentPointerPos = GetPointerPositionOnPlane(pointerPos);
        Vector3 dragVector = currentPointerPos - dragStartPos;
        
        dragVector = Vector3.ClampMagnitude(dragVector, maxDragDistance);

        bool isShotValid = dragVector.magnitude >= minDragDistance;

        Sequence animSeq = DOTween.Sequence();

        // 1. Lastik Çarpma Animasyonu (Snap)
        if (dragLine != null && dragLine.enabled)
        {
            Vector3 endPos = dragLine.End;
            Vector3 startPos = dragLine.Start;
            
            animSeq.Append(DOVirtual.Vector3(endPos, startPos, snapAnimationDuration, (val) => {
                if (dragLine != null) dragLine.End = val;
            }).SetEase(Ease.Linear));

            animSeq.AppendCallback(() => {
                if (dragLine != null) dragLine.enabled = false;
            });
        }
        else
        {
            animSeq.AppendInterval(snapAnimationDuration);
        }

        // 2. Beyaz Halkanın Küçülüp Kaybolması
        if (dragRingObj != null)
        {
            animSeq.AppendCallback(() => dragRingObj.transform.DOKill());
            animSeq.Append(dragRingObj.transform.DOScale(Vector3.zero, 0.1f).SetEase(Ease.InBack));
            animSeq.AppendCallback(() => dragRingObj.SetActive(false));
        }

        // 3. Fırlatma (Sadece Geçerli Atışta)
        if (isShotValid)
        {
            if (postSnapDelay > 0f)
            {
                animSeq.AppendInterval(postSnapDelay);
            }

            animSeq.AppendCallback(() => {
                Vector3 force = -dragVector * powerMultiplier;
                rb.AddForce(force, ForceMode.Impulse);
                
                isLaunched = true;
                hasHitCube = false;
                launchTime = Time.time;
            });
        }
    }

    private Vector3 GetPointerPositionOnPlane(Vector2 pointerPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(pointerPos);
        if (dragPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        return dragStartPos;
    }

    private void UpdateLine(Vector3 currentPointerPos)
    {
        if (dragLine == null) return;
        
        Vector3 dragDirection = (currentPointerPos - dragStartPos);
        dragDirection.y = 0; 
        
        Vector3 dynamicStartPos = dragStartPos;
        if (dragDirection.sqrMagnitude > 0.001f)
        {
            dynamicStartPos += dragDirection.normalized * lineStartRadius;
        }
        dynamicStartPos.y += lineHeightOffset;

        Vector3 dynamicEndPos = currentPointerPos;
        dynamicEndPos.y += lineHeightOffset;

        Vector3 localStart = dragLine.transform.InverseTransformPoint(dynamicStartPos);
        Vector3 localEnd = dragLine.transform.InverseTransformPoint(dynamicEndPos);

        dragLine.Start = localStart;
        dragLine.End = localEnd;

        float currentDistance = Vector3.Distance(dragStartPos, currentPointerPos);
        float distanceRatio = Mathf.Clamp01(currentDistance / maxDragDistance);
        
        float currentWidth = Mathf.Lerp(lineMaxWidth, lineMinWidth, distanceRatio);
        dragLine.Thickness = currentWidth;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.rigidbody != null)
        {
            Cube hitCube = collision.rigidbody.GetComponent<Cube>();
            if (hitCube != null)
            {
                if (!hasHitCube) 
                {
                    // Debug.Log("Disk bir Kübe ÇARPTI! hasHitCube = true oldu.");
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
