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
    [Tooltip("Top hareket halindeyken tekrar atış yapılabilir mi?")]
    public bool canShootWhileMoving = false;

    [Header("Line Renderer Settings")]
    public LineRenderer lineRenderer;
    [Tooltip("Çizginin en kalın hali (kısa çekildiğinde).")]
    public float lineMaxWidth = 0.5f;
    [Tooltip("Çizginin en ince hali (uzun çekildiğinde).")]
    public float lineMinWidth = 0.1f;

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

                    if (lineRenderer != null)
                    {
                        lineRenderer.enabled = true;
                        UpdateLineRenderer(dragStartPos);
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

            if (dragVector.magnitude > maxDragDistance)
            {
                dragVector = dragVector.normalized * maxDragDistance;
            }

            // Çekilen yönün tersine güç uyguluyoruz
            Vector3 force = -dragVector * powerMultiplier;
            rb.AddForce(force, ForceMode.Impulse);
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
        lineRenderer.SetPosition(0, dragStartPos);
        lineRenderer.SetPosition(1, currentPointerPos);

        float currentDistance = Vector3.Distance(dragStartPos, currentPointerPos);
        float distanceRatio = Mathf.Clamp01(currentDistance / maxDragDistance);
        
        float currentWidth = Mathf.Lerp(lineMaxWidth, lineMinWidth, distanceRatio);
        lineRenderer.startWidth = currentWidth;
        lineRenderer.endWidth = currentWidth;
    }
}
