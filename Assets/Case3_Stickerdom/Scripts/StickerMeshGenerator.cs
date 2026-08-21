using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class StickerMeshGenerator : MonoBehaviour
{
    [Header("Mesh Settings")]
    [Tooltip("Gridin kaç parçaya bölüneceğini belirler. Yüksek değer = daha pürüzsüz kıvrım, ama daha çok poligon.")]
    [Range(2, 100)]
    public int resolution = 30;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogWarning("StickerMeshGenerator: SpriteRenderer'da bir resim bulunamadı.");
            return;
        }

        // 1. Mesh için yeni bir ALT (Child) obje yarat (SpriteRenderer ve MeshRenderer aynı objede olamaz!)
        GameObject meshObj = new GameObject("GeneratedStickerMesh");
        meshObj.transform.SetParent(this.transform);
        meshObj.transform.localPosition = Vector3.zero;
        meshObj.transform.localRotation = Quaternion.identity;
        meshObj.transform.localScale = Vector3.one;

        MeshFilter mf = meshObj.AddComponent<MeshFilter>();
        MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();

        // 2. Sprite'ın boyutlarına tam uyan ızgara (grid) mesh'ini üret
        GenerateMesh(mf);

        // 3. SpriteRenderer'daki ayarları MeshRenderer'a kopyala
        mr.material = spriteRenderer.material;
        mr.sortingLayerID = spriteRenderer.sortingLayerID;
        mr.sortingOrder = spriteRenderer.sortingOrder;
        
        // SpriteRenderer resmi Shader'a otomatik gönderir, ama MeshRenderer bunu yapmaz!
        // Bu yüzden Sprite'ın dokusunu (texture) ve rengini (tint) manuel olarak Materyale vermeliyiz.
        if (spriteRenderer.sprite != null && spriteRenderer.sprite.texture != null)
        {
            mr.material.SetTexture("_MainTex", spriteRenderer.sprite.texture);
        }
        mr.material.SetColor("_Color", spriteRenderer.color);

        // 4. Eski SpriteRenderer'ı kapat (iki kere çizilmesin)
        spriteRenderer.enabled = false;
        
        // StickerController'ı uyaralım ki yeni child objeyi bilsin (opsiyonel ama sağlıklı)
        StickerController controller = GetComponent<StickerController>();
        if (controller != null)
        {
            controller.SetActiveRenderer(mr);
        }
    }

    void GenerateMesh(MeshFilter mf)
    {
        Sprite sprite = spriteRenderer.sprite;
        
        // Sprite'ın local uzaydaki boyutu ve merkezi
        Vector2 size = sprite.bounds.size;
        Vector2 offset = sprite.bounds.center; 

        Mesh mesh = new Mesh();
        mesh.name = "StickerGridMesh";

        int numVertices = (resolution + 1) * (resolution + 1);
        int numTriangles = resolution * resolution * 6;

        Vector3[] vertices = new Vector3[numVertices];
        Vector2[] uvs = new Vector2[numVertices];
        int[] triangles = new int[numTriangles];

        if (sprite.texture == null)
        {
            Debug.LogError("StickerMeshGenerator: Sprite'ın texture'ı bulunamadı (null)!");
            return;
        }

        // UV koordinatlarını hesaplamak için sprite'ın texture üzerindeki bölgesini bul
        Rect uvRect = new Rect(
            sprite.rect.x / sprite.texture.width,
            sprite.rect.y / sprite.texture.height,
            sprite.rect.width / sprite.texture.width,
            sprite.rect.height / sprite.texture.height
        );

        float stepX = size.x / resolution;
        float stepY = size.y / resolution;

        float startX = -size.x / 2f + offset.x;
        float startY = -size.y / 2f + offset.y;

        int vIndex = 0;
        for (int y = 0; y <= resolution; y++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                // Vertex pozisyonu
                vertices[vIndex] = new Vector3(startX + (x * stepX), startY + (y * stepY), 0);

                // UV hesabı
                float u = (float)x / resolution;
                float v = (float)y / resolution;
                uvs[vIndex] = new Vector2(
                    Mathf.Lerp(uvRect.xMin, uvRect.xMax, u),
                    Mathf.Lerp(uvRect.yMin, uvRect.yMax, v)
                );

                vIndex++;
            }
        }

        int tIndex = 0;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = (y * (resolution + 1)) + x;

                // 1. Üçgen
                triangles[tIndex++] = i;
                triangles[tIndex++] = i + resolution + 1;
                triangles[tIndex++] = i + 1;

                // 2. Üçgen
                triangles[tIndex++] = i + 1;
                triangles[tIndex++] = i + resolution + 1;
                triangles[tIndex++] = i + resolution + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        
        // Eksik Vertex Renklerini (White) ekleyelim ki Shader'daki IN.color çarpımı siyah/şeffaf yapmasın.
        Color[] colors = new Color[numVertices];
        for (int c = 0; c < numVertices; c++) colors[c] = Color.white;
        mesh.colors = colors;
        
        mesh.triangles = triangles;
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (mf == null)
        {
            Debug.LogError("StickerMeshGenerator: mf parametresi null geldi!");
            return;
        }
        
        mf.sharedMesh = mesh;
    }
}
