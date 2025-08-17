using UnityEngine;

public class ShadowProjector : MonoBehaviour
{
    [Header("Shadow Settings")]
    public float shadowLength = 5f;
    public float shadowWidth = 2f;
    public Material shadowMaterial;

    [Header("Trigger Collider")]
    public float colliderHeight = 3f;

    [Header("Runtime")]
    public GameObject shadowMesh;
    public MeshRenderer shadowRenderer;

    private MeshFilter meshFilter;
    private bool isMerged = false;
    private Vector3 lastSunPosition;

    void Start()
    {
        CreateShadowMesh();
        gameObject.tag = "ShadowCaster";
    }

    void CreateShadowMesh()
    {
        shadowMesh = new GameObject("Shadow_" + gameObject.name);
        shadowMesh.transform.parent = transform;

        // Layer مهم نیست - فقط Tag
        shadowMesh.layer = gameObject.layer;

        // موقعیت: پایین‌ترین نقطه parent
        Vector3 localBottom = CalculateBottomPosition();
        shadowMesh.transform.localPosition = localBottom;

        // Mesh برای نمایش
        meshFilter = shadowMesh.AddComponent<MeshFilter>();
        shadowRenderer = shadowMesh.AddComponent<MeshRenderer>();

        if (shadowMaterial == null)
            shadowMaterial = CreateDefaultShadowMaterial();

        shadowRenderer.material = shadowMaterial;
        shadowRenderer.sortingOrder = -1;
        meshFilter.mesh = CreateQuadMesh();

        // ===== مهم‌ترین قسمت: Trigger Collider =====
        var shadowCollider = shadowMesh.AddComponent<BoxCollider>();
        shadowCollider.isTrigger = true;
        shadowCollider.size = new Vector3(shadowWidth * 1.2f, colliderHeight, shadowLength * 1.1f);
        shadowCollider.center = new Vector3(0f, colliderHeight / 2f, shadowLength / 2f);

        // Tag برای تشخیص
        shadowMesh.tag = "Shadow";

        // موقعیت جهانی
        shadowMesh.transform.position = GetBottomWorldPosition();
        shadowMesh.transform.rotation = Quaternion.identity;

        Debug.Log($"✅ سایه ساخته شد: {shadowMesh.name} با Tag: {shadowMesh.tag}");
    }

    Vector3 CalculateBottomPosition()
    {
        Renderer objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            float bottomY = objectRenderer.bounds.min.y - transform.position.y;
            return new Vector3(0, bottomY, 0);
        }

        Collider objectCollider = GetComponent<Collider>();
        if (objectCollider != null)
        {
            float bottomY = objectCollider.bounds.min.y - transform.position.y;
            return new Vector3(0, bottomY, 0);
        }

        return new Vector3(0, -0.5f, 0);
    }

    Vector3 GetBottomWorldPosition()
    {
        Renderer objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            return new Vector3(
                transform.position.x,
                objectRenderer.bounds.min.y,
                transform.position.z
            );
        }

        Collider objectCollider = GetComponent<Collider>();
        if (objectCollider != null)
        {
            return new Vector3(
                transform.position.x,
                objectCollider.bounds.min.y,
                transform.position.z
            );
        }

        return transform.position;
    }

    public void UpdateShadow(Vector3 sunPosition)
    {
        if (shadowMesh == null || isMerged) return;
        if (sunPosition == lastSunPosition) return;

        // جهت سایه
        Vector3 direction = (transform.position - sunPosition).normalized;
        direction.y = 0f;

        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, angle, 0f);

        shadowMesh.transform.rotation = Quaternion.Lerp(
            shadowMesh.transform.rotation,
            targetRotation,
            Time.deltaTime * 8f
        );

        shadowMesh.transform.position = GetBottomWorldPosition();
        lastSunPosition = sunPosition;
    }

    // برای compatibility اگر جای دیگری استفاده شود
    public bool IsPointInShadow(Vector3 worldPoint)
    {
        // دیگر نیازی نیست - Trigger خودش تشخیص می‌دهد
        if (shadowRenderer == null) return false;

        Bounds bounds = shadowRenderer.bounds;
        bounds.Expand(1f);
        return bounds.Contains(worldPoint);
    }

    public float GetShadowStrength(Vector3 worldPoint)
    {
        if (!IsPointInShadow(worldPoint)) return 0f;

        // ساده: اگر در سایه است، قدرت 100%
        return 1f;
    }

    public bool IsMerged() => isMerged;

    Material CreateDefaultShadowMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mat.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
        return mat;
    }

    Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh { name = "ShadowQuad" };

        Vector3[] vertices = new Vector3[4];
        int[] triangles = new int[6];
        Vector2[] uvs = new Vector2[4];

        vertices[0] = new Vector3(-shadowWidth / 2f, 0f, 0f);
        vertices[1] = new Vector3(shadowWidth / 2f, 0f, 0f);
        vertices[2] = new Vector3(-shadowWidth / 2f, 0f, shadowLength);
        vertices[3] = new Vector3(shadowWidth / 2f, 0f, shadowLength);

        triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
        triangles[3] = 2; triangles[4] = 3; triangles[5] = 1;

        uvs[0] = new Vector2(0f, 0f);
        uvs[1] = new Vector2(1f, 0f);
        uvs[2] = new Vector2(0f, 1f);
        uvs[3] = new Vector2(1f, 1f);

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void OnDrawGizmosSelected()
    {
        if (shadowMesh != null)
        {
            // نمایش Trigger Collider
            Gizmos.color = Color.green;
            Gizmos.matrix = shadowMesh.transform.localToWorldMatrix;
            Vector3 colliderCenter = new Vector3(0f, colliderHeight / 2f, shadowLength / 2f);
            Vector3 colliderSize = new Vector3(shadowWidth * 1.2f, colliderHeight, shadowLength * 1.1f);
            Gizmos.DrawWireCube(colliderCenter, colliderSize);

            // نمایش Tag
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(shadowMesh.transform.position + Vector3.up * 2f, 0.3f);
            }
        }
    }
}