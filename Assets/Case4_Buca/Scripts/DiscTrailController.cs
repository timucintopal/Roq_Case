using UnityEngine;

public class DiscTrailController : MonoBehaviour
{
    [Header("Physics Reference")]
    [Tooltip("Hızını takip edeceğimiz ana obje (Rigidbody). Inspector'dan atanabilir, boş bırakılırsa objede veya parent'ta arar.")]
    public Rigidbody targetRigidbody;

    [Header("Trail References")]
    [Tooltip("Eğer boş bırakılırsa, otomatik olarak objenin üzerindeki veya altındaki TrailRenderer'ı bulur.")]
    public TrailRenderer trailRenderer;
    
    [Header("Speed Settings")]
    [Tooltip("Trail'in görünmeye başlaması için gereken minimum hız.")]
    public float minSpeed = 2f;
    [Tooltip("Trail'in maksimum belirginliğe (kalınlık ve uzunluk) ulaştığı hız.")]
    public float maxSpeed = 15f;

    [Header("Trail Visuals (Dynamic)")]
    [Tooltip("Maksimum hızdayken Trail'in ulaşacağı genişlik çarpanı.")]
    public float maxWidthMultiplier = 1f;
    [Tooltip("Minimum hızdayken Trail'in genişlik çarpanı.")]
    public float minWidthMultiplier = 0.2f;
    
    [Tooltip("Maksimum hızdayken Trail'in ekranda kalma süresi (uzunluğu).")]
    public float maxTime = 0.5f;
    [Tooltip("Minimum hızdayken Trail'in ekranda kalma süresi.")]
    public float minTime = 0.1f;

    private void Awake()
    {
        if (targetRigidbody == null)
        {
            targetRigidbody = GetComponentInParent<Rigidbody>();
        }
        
        if (trailRenderer == null)
        {
            trailRenderer = GetComponentInChildren<TrailRenderer>();
        }
        
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false; // Başlangıçta kapalı
        }
    }

    private void Update()
    {
        if (trailRenderer == null || targetRigidbody == null) return;

        float speed = targetRigidbody.linearVelocity.magnitude;

        // Disk çok yavaşsa trail'i kapat
        if (speed < minSpeed)
        {
            if (trailRenderer.emitting)
                trailRenderer.emitting = false;
        }
        else
        {
            if (!trailRenderer.emitting)
                trailRenderer.emitting = true;
            
            // Hızı 0 ile 1 arasında normalize et (minSpeed=0, maxSpeed=1 olacak şekilde)
            float speedFactor = Mathf.Clamp01((speed - minSpeed) / (maxSpeed - minSpeed));
            
            // Hıza göre kalınlığı ve uzunluğu (time) dinamik olarak ayarla
            trailRenderer.widthMultiplier = Mathf.Lerp(minWidthMultiplier, maxWidthMultiplier, speedFactor);
            trailRenderer.time = Mathf.Lerp(minTime, maxTime, speedFactor);
        }
    }
}
