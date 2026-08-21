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
    public float lineStartYOffset = 0.5f;
    public float lineEndYOffset = 0.5f;
    [Tooltip("Çizginin en kalın hali (kısa çekildiğinde).")]
    public float lineMaxWidth = 0.5f;
    [Tooltip("Çizginin en ince hali (uzun çekildiğinde).")]
    public float lineMinWidth = 0.1f;
    [Tooltip("Çizginin bitiş noktasına doğru ne kadar inceldiği (0 = iğne gibi sivri, 1 = başlangıçla aynı kalınlık).")]
    [Range(0f, 1f)]
    public float lineEndWidthMultiplier = 0.2f;

    private Rigidbody rb;
    private bool isDragging = false;
    private Vector3 dragStartPos;
    private Plane dragPlane;
    private Camera mainCamera;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 2;
        }
    }

    private void Update()
    {
        if (Pointer.current == null) return;

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
        Vector3 startOffset = new Vector3(0, lineStartYOffset, 0);
        Vector3 endOffset = new Vector3(0, lineEndYOffset, 0);
        
        lineRenderer.SetPosition(0, dragStartPos + startOffset);
        lineRenderer.SetPosition(1, currentPointerPos + endOffset);

        float currentDistance = Vector3.Distance(dragStartPos, currentPointerPos);
        float distanceRatio = Mathf.Clamp01(currentDistance / maxDragDistance);
        
        float currentWidth = Mathf.Lerp(lineMaxWidth, lineMinWidth, distanceRatio);
        lineRenderer.startWidth = currentWidth;
        lineRenderer.endWidth = currentWidth * lineEndWidthMultiplier;
    }
}
