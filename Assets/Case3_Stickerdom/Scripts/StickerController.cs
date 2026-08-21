using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class StickerController : MonoBehaviour
{
    [Header("References")]
    public Transform snapTarget; // Hedef yapışma noktası (boşluk/gölge olan yer)
    public Transform shadowTransform; // Sticker'ın altındaki gölge (çocuk obje olmalı)

    [Header("Settings")]
    public float snapDistance = 1.5f; // Ne kadar yaklaşınca yapışsın
    public float maxPeelAmount = 0.8f; // Sürüklerken maksimum ne kadar kıvrılsın (0-1)
    public float peelDuration = 0.3f; // Kıvrılma animasyon hızı

    private SpriteRenderer spriteRenderer;
    private Material stickerMaterial;
    private Vector3 originalPosition;
    
    // Shader Property ID'leri (Performans için)
    private int peelAmountPropId;
    private int peelDirPropId;

    private Vector3 dragOffset;
    private bool isDragging = false;
    private bool isPlaced = false;
    private Camera mainCam;
    private float originalZ;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Material'in instance'ını alıyoruz ki diğer sticker'lar aynı anda kıvrılmasın
        if (spriteRenderer.material != null)
        {
            stickerMaterial = new Material(spriteRenderer.material);
            spriteRenderer.material = stickerMaterial;
        }

        peelAmountPropId = Shader.PropertyToID("_PeelAmount");
        peelDirPropId = Shader.PropertyToID("_PeelDirection");

        originalPosition = transform.position;
        originalZ = transform.position.z;
        mainCam = Camera.main;
        
        if (stickerMaterial != null)
        {
            stickerMaterial.SetFloat(peelAmountPropId, 0f);
        }
    }

    void OnMouseDown()
    {
        if (isPlaced) return;

        isDragging = true;
        
        Vector3 mouseWorldPos = GetMouseWorldPos();
        dragOffset = transform.position - mouseWorldPos;

        // Dokunduğumuz yere göre soyulma yönünü hesapla (Dokunulan noktadan merkeze doğru)
        Vector3 clickLocalDir = transform.InverseTransformPoint(mouseWorldPos).normalized;
        Vector2 peelDir = new Vector2(-clickLocalDir.x, -clickLocalDir.y);
        
        if (peelDir == Vector2.zero) peelDir = new Vector2(-1, 1); // Varsayılan yön
        
        if (stickerMaterial != null)
        {
            stickerMaterial.SetVector(peelDirPropId, peelDir);
            // Başlangıçta hafifçe kıvrılma (Jelly/Tepki hissi)
            DOTween.Kill(stickerMaterial);
            stickerMaterial.DOFloat(0.2f, peelAmountPropId, peelDuration / 2f);
        }
        
        // Havaya kalkma hissi için Z ekseninde öne al
        transform.position = new Vector3(transform.position.x, transform.position.y, originalZ - 1f);

        // Gölgeyi Z ekseninde geriye iterek ve kaydırarak derinlik hissi yarat (Drop Shadow)
        if (shadowTransform != null)
        {
            shadowTransform.DOLocalMove(new Vector3(0.15f, -0.15f, 0.5f), peelDuration);
            SpriteRenderer shadowSR = shadowTransform.GetComponent<SpriteRenderer>();
            if (shadowSR != null) shadowSR.DOFade(0.4f, peelDuration);
        }
    }

    void OnMouseDrag()
    {
        if (isPlaced || !isDragging) return;

        // Fareyi takip et
        transform.position = GetMouseWorldPos() + dragOffset;

        // Sürüklerken kıvrılma miktarını dinamik olarak artır
        if (stickerMaterial != null)
        {
            float currentPeel = stickerMaterial.GetFloat(peelAmountPropId);
            if (currentPeel < maxPeelAmount)
            {
                // Yumuşak geçişle kıvrılma miktarını hedefe çek
                float newPeel = Mathf.Lerp(currentPeel, maxPeelAmount, Time.deltaTime * 5f);
                stickerMaterial.SetFloat(peelAmountPropId, newPeel);
            }
        }
    }

    void OnMouseUp()
    {
        if (isPlaced || !isDragging) return;
        isDragging = false;

        float distanceToTarget = snapTarget != null ? Vector2.Distance(transform.position, snapTarget.position) : float.MaxValue;

        if (stickerMaterial != null) DOTween.Kill(stickerMaterial);

        if (distanceToTarget <= snapDistance && snapTarget != null)
        {
            // --- BAŞARILI YAPIŞTIRMA ---
            isPlaced = true;
            
            // Hedefe doğru git (OutBack ile hafif taşma efekti - Juice)
            transform.DOMove(snapTarget.position, 0.2f).SetEase(Ease.OutBack);
            
            // Sticker'ı düzelt (Kıvrılmayı sıfırla)
            if (stickerMaterial != null)
            {
                stickerMaterial.DOFloat(0f, peelAmountPropId, 0.2f).OnComplete(() => {
                    // Tam yapıştığı an jöle gibi titreme (Punch Scale)
                    transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.3f, 5, 0.5f);
                    transform.position = new Vector3(transform.position.x, transform.position.y, snapTarget.position.z - 0.1f);
                    
                    // TODO: Buraya "Particle System" (toz bulutu veya yıldızlar) eklenebilir.
                    // TODO: Tok bir yapışma sesi (SFX) çalınabilir.
                });
            }

            // Gölgeyi sıfırla/kapat
            if (shadowTransform != null)
            {
                shadowTransform.DOLocalMove(Vector3.zero, 0.2f);
                SpriteRenderer shadowSR = shadowTransform.GetComponent<SpriteRenderer>();
                if (shadowSR != null) shadowSR.DOFade(0f, 0.2f);
            }
        }
        else
        {
            // --- YANLIŞ YER / GERİ DÖNÜŞ ---
            
            transform.DOMove(originalPosition, 0.3f).SetEase(Ease.OutQuad);
            transform.DOMoveZ(originalZ, 0.3f);

            if (stickerMaterial != null)
            {
                stickerMaterial.DOFloat(0f, peelAmountPropId, 0.3f);
            }
            
            if (shadowTransform != null)
            {
                shadowTransform.DOLocalMove(Vector3.zero, 0.3f);
                SpriteRenderer shadowSR = shadowTransform.GetComponent<SpriteRenderer>();
                if (shadowSR != null) shadowSR.DOFade(0f, 0.3f);
            }
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        // Kamera Z ile Obje Z arasındaki farkı bul (Perspektif için)
        mousePos.z = Mathf.Abs(mainCam.transform.position.z - transform.position.z);
        return mainCam.ScreenToWorldPoint(mousePos);
    }
}
