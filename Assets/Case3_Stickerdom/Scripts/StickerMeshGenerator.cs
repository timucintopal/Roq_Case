using Case3_Stickerdom.Scripts;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(SpriteRenderer))]
public class StickerMeshGenerator : MonoBehaviour
{
    [Header("Mesh Settings")]
    [Tooltip("Gridin kaç parçaya bölüneceğini belirler. Yüksek değer = daha pürüzsüz kıvrım, ama daha çok poligon.")]
    [Range(2, 100)]
    public int resolution = 30;

    void Awake()
    {
        if (Application.isPlaying)
        {
            // Eğer Editor'de önceden yaratılmış bir mesh varsa (child obje), tekrar yaratmaya gerek yok.
            if (transform.Find("GeneratedStickerMesh") != null)
            {
                // Sadece eski SpriteRenderer'ı kapat
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;
                return;
            }
            
            // Eğer yoksa Run-time (Oyun anında) yarat
            GenerateRuntimeMesh();
        }
    }

    private void GenerateRuntimeMesh()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        GameObject meshObj = new GameObject("GeneratedStickerMesh");
        meshObj.transform.SetParent(this.transform);
        meshObj.transform.localPosition = Vector3.zero;
        meshObj.transform.localRotation = Quaternion.identity;
        meshObj.transform.localScale = Vector3.one;

        MeshFilter mf = meshObj.AddComponent<MeshFilter>();
        MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();

        Mesh mesh = CreateGridMesh(spriteRenderer.sprite);
        if (mesh == null) return;

        mf.sharedMesh = mesh;

        mr.material = spriteRenderer.material;
        mr.sortingLayerID = spriteRenderer.sortingLayerID;
        mr.sortingOrder = spriteRenderer.sortingOrder;
        
        if (spriteRenderer.sprite.texture != null)
        {
            mr.material.SetTexture("_MainTex", spriteRenderer.sprite.texture);
        }
        mr.material.SetColor("_Color", spriteRenderer.color);

        spriteRenderer.enabled = false;
        
        StickerController controller = GetComponent<StickerController>();
        if (controller != null) controller.SetActiveRenderer(mr);
    }

    private Mesh CreateGridMesh(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return null;

        Vector2 size = sprite.bounds.size;
        Vector2 offset = sprite.bounds.center; 

        Mesh mesh = new Mesh();
        mesh.name = "StickerGridMesh";

        int numVertices = (resolution + 1) * (resolution + 1);
        int numTriangles = resolution * resolution * 6;

        Vector3[] vertices = new Vector3[numVertices];
        Vector2[] uvs = new Vector2[numVertices];
        int[] triangles = new int[numTriangles];

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
                vertices[vIndex] = new Vector3(startX + (x * stepX), startY + (y * stepY), 0);

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
                triangles[tIndex++] = i;
                triangles[tIndex++] = i + resolution + 1;
                triangles[tIndex++] = i + 1;
                triangles[tIndex++] = i + 1;
                triangles[tIndex++] = i + resolution + 1;
                triangles[tIndex++] = i + resolution + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        
        Color[] colors = new Color[numVertices];
        for (int c = 0; c < numVertices; c++) colors[c] = Color.white;
        mesh.colors = colors;
        
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

#if UNITY_EDITOR
    [ContextMenu("Meshi Uret ve Sahneye Kaydet (Onerilen)")]
    public void GenerateMeshInEditor()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            Debug.LogError("Sprite bulunamadi!");
            return;
        }

        Transform existingChild = transform.Find("GeneratedStickerMesh");
        if (existingChild != null)
        {
            DestroyImmediate(existingChild.gameObject);
        }

        GameObject meshObj = new GameObject("GeneratedStickerMesh");
        meshObj.transform.SetParent(this.transform);
        meshObj.transform.localPosition = Vector3.zero;
        meshObj.transform.localRotation = Quaternion.identity;
        meshObj.transform.localScale = Vector3.one;

        MeshFilter mf = meshObj.AddComponent<MeshFilter>();
        MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();

        Mesh mesh = CreateGridMesh(sr.sprite);
        if (mesh == null) return;

        // Dosyaya kaydet
        string folderPath = "Assets/Case3_Stickerdom/Meshes";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        string assetPath = $"{folderPath}/{sr.sprite.name}_GridMesh.asset";
        Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        
        if (existingMesh != null)
        {
            existingMesh.Clear();
            EditorUtility.CopySerialized(mesh, existingMesh);
            existingMesh.RecalculateNormals();
            existingMesh.RecalculateBounds();
            mf.sharedMesh = existingMesh;
        }
        else
        {
            AssetDatabase.CreateAsset(mesh, assetPath);
            mf.sharedMesh = mesh;
        }
        
        AssetDatabase.SaveAssets();

        mr.sharedMaterial = sr.sharedMaterial;
        mr.sortingLayerID = sr.sortingLayerID;
        mr.sortingOrder = sr.sortingOrder;
        
        if (sr.sprite.texture != null)
        {
            mr.sharedMaterial.SetTexture("_MainTex", sr.sprite.texture);
        }
        mr.sharedMaterial.SetColor("_Color", sr.color);

        sr.enabled = false;
        
        StickerController controller = GetComponent<StickerController>();
        if (controller != null)
        {
            controller.SetActiveRenderer(mr);
        }

        EditorUtility.SetDirty(this.gameObject);
        Debug.Log($"<color=green>Mesh basariyla uretildi ve dosyaya kaydedildi: {assetPath}</color>");
    }
#endif
}
