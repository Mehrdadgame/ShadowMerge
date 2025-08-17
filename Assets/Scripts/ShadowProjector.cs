using UnityEngine;

public class ShadowProjector : MonoBehaviour
{
    [Header("Shadow Settings")]
    public float shadowLength = 5f;
    public float shadowWidth = 1f;
    public Material shadowMaterial;
    public LayerMask mergeDetectionLayer = -1;

    private GameObject shadowMesh;
    private MeshRenderer shadowRenderer;
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
        shadowMesh.layer = LayerMask.NameToLayer("Default"); // Changed from Shadow layer

        MeshFilter meshFilter = shadowMesh.AddComponent<MeshFilter>();
        shadowRenderer = shadowMesh.AddComponent<MeshRenderer>();

        // Create default shadow material if none provided
        if (shadowMaterial == null)
        {
            shadowMaterial = CreateDefaultShadowMaterial();
        }

        shadowRenderer.material = shadowMaterial;
        shadowRenderer.sortingOrder = -1;

        // Add collider for shadow detection
        BoxCollider shadowCollider = shadowMesh.AddComponent<BoxCollider>();
        shadowCollider.isTrigger = true;
        shadowCollider.size = new Vector3(shadowWidth, 0.1f, shadowLength);
        shadowCollider.center = new Vector3(0, 0, shadowLength / 2);

        // Create quad mesh for shadow
        meshFilter.mesh = CreateQuadMesh();
    }

    Material CreateDefaultShadowMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3); // Transparent mode
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
        Mesh mesh = new Mesh();
        mesh.name = "ShadowQuad";

        Vector3[] vertices = new Vector3[4];
        int[] triangles = new int[6];
        Vector2[] uvs = new Vector2[4];

        vertices[0] = new Vector3(-shadowWidth / 2, 0, 0);
        vertices[1] = new Vector3(shadowWidth / 2, 0, 0);
        vertices[2] = new Vector3(-shadowWidth / 2, 0, shadowLength);
        vertices[3] = new Vector3(shadowWidth / 2, 0, shadowLength);

        triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
        triangles[3] = 2; triangles[4] = 3; triangles[5] = 1;

        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(1, 0);
        uvs[2] = new Vector2(0, 1);
        uvs[3] = new Vector2(1, 1);

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    public void UpdateShadow(Vector3 sunPosition)
    {
        // NULL CHECK - این خط مشکل رو حل میکنه
        if (shadowMesh == null || shadowRenderer == null || isMerged)
            return;

        if (sunPosition == lastSunPosition)
            return;

        Vector3 direction = (transform.position - sunPosition).normalized;
        direction.y = 0; // Keep shadow on ground

        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        // Smooth rotation
        Quaternion targetRotation = Quaternion.Euler(0, angle, 0);
        shadowMesh.transform.rotation = Quaternion.Lerp(
            shadowMesh.transform.rotation,
            targetRotation,
            Time.deltaTime * 8f
        );

        shadowMesh.transform.position = transform.position;
        lastSunPosition = sunPosition;

        // Check for potential merges
        CheckForMerge();
    }

    void CheckForMerge()
    {
        if (isMerged || shadowMesh == null) return;

        Collider[] nearbyColliders = Physics.OverlapBox(
            shadowMesh.transform.position + shadowMesh.transform.forward * shadowLength / 2,
            new Vector3(shadowWidth / 2, 0.1f, shadowLength / 2),
            shadowMesh.transform.rotation,
            mergeDetectionLayer
        );

        foreach (var collider in nearbyColliders)
        {
            ShadowProjector otherShadow = collider.GetComponentInParent<ShadowProjector>();
            if (otherShadow != null && otherShadow != this && !otherShadow.isMerged && !isMerged)
            {
                Vector3 mergePosition = Vector3.Lerp(
                    shadowMesh.transform.position,
                    otherShadow.shadowMesh.transform.position,
                    0.5f
                );

                MergeShadows(otherShadow, mergePosition);
                break;
            }
        }
    }

    void MergeShadows(ShadowProjector otherShadow, Vector3 mergePosition)
    {
        isMerged = true;
        otherShadow.isMerged = true;

        // Visual feedback
        if (shadowRenderer != null && shadowRenderer.material != null)
        {
            shadowRenderer.material.color = new Color(0.2f, 0.2f, 0.2f, 0.9f); // Darker merged shadow
        }

        if (otherShadow.shadowRenderer != null && otherShadow.shadowRenderer.material != null)
        {
            otherShadow.shadowRenderer.material.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        }

        // Effects
        ParticleManager.Instance?.PlayShadowMerge(mergePosition);
        AudioManager.Instance?.PlayShadowMerge();

        // Analytics
        if (GameManager.Instance != null)
        {
            AnalyticsTracker.TrackShadowMerge(GameManager.Instance.currentLevel, 1);
        }

        // اضافه کردن کومبو برای ادغام سایه
        ComboSystem.Instance?.AddShadowMerge();

        Debug.Log($"✨ سایه‌ها ادغام شدند: {gameObject.name} + {otherShadow.gameObject.name}");
    }

    public bool IsPointInShadow(Vector3 point)
    {
        if (shadowMesh == null || shadowRenderer == null) return false;

        // تحمل بیشتر برای تشخیص سایه
        Bounds shadowBounds = shadowRenderer.bounds;
        shadowBounds.Expand(0.3f); // Increased tolerance
        return shadowBounds.Contains(point);
    }

    public bool IsMerged() => isMerged;

    // بهبود سایه برای قطره آب
    public float GetShadowStrength(Vector3 point)
    {
        if (!IsPointInShadow(point) || shadowMesh == null) return 0f;

        Vector3 localPoint = shadowMesh.transform.InverseTransformPoint(point);
        float distanceFromCenter = Vector3.Distance(localPoint, Vector3.zero);
        float maxDistance = shadowLength * 0.5f;

        return Mathf.Clamp01(1f - (distanceFromCenter / maxDistance));
    }

    void OnDrawGizmosSelected()
    {
        if (shadowMesh != null)
        {
            Gizmos.color = isMerged ? Color.red : Color.blue;
            Gizmos.matrix = shadowMesh.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0, 0, shadowLength / 2), new Vector3(shadowWidth, 0.1f, shadowLength));
        }
    }
}