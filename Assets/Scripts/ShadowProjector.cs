using UnityEngine;

public class ShadowProjector : MonoBehaviour
{
    [Header("Shadow Settings")]
    public float shadowLength = 5f;   // ← نگه داشته شد (برای کلاس‌های دیگر)
    public float shadowWidth = 1f;   // ← نگه داشته شد (برای کلاس‌های دیگر)
    public Material shadowMaterial;
    public LayerMask mergeDetectionLayer = -1;

    [Header("Detection Tuning")]
    [Tooltip("تحمل عمودی برای تشخیص درون سایه (واحد: متر)")]
    public float yTolerance = 2f;

    [Tooltip("ارتفاع کلايدر سایه (برای Overlap/Trigger بهتر)")]
    public float colliderHeight = 2f;

    [Tooltip("جبران مرکز کلايدر در محور Y (نصف ارتفاع کفایت می‌کند)")]
    public float colliderCenterY = 1f;

    [Header("Runtime")]
    public GameObject shadowMesh;
    public MeshRenderer shadowRenderer;

    private MeshFilter meshFilter;
    private bool isMerged = false;    // ← نگه داشته شد
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

        // تلاش برای یافتن لایه‌ی "Shadow"، وگرنه 8
        int shadowLayer = LayerMask.NameToLayer("Shadow");
        shadowMesh.layer = (shadowLayer == -1) ? 8 : shadowLayer;

        // موقعیت اولیه: پایین‌ترین نقطه پرنت (local یا world هر دو را پوشش می‌دهیم)
        Vector3 localBottom = CalculateBottomPosition();
        shadowMesh.transform.localPosition = localBottom;

        meshFilter = shadowMesh.AddComponent<MeshFilter>();
        shadowRenderer = shadowMesh.AddComponent<MeshRenderer>();

        if (shadowMaterial == null)
            shadowMaterial = CreateDefaultShadowMaterial();

        shadowRenderer.material = shadowMaterial;

        // ترتیب رندر برای شفاف‌ها — اگر رندررت از این پشتیبانی کند
        shadowRenderer.sortingOrder = -1;

        // هندسه سایه (یک Quad روی زمین)
        meshFilter.mesh = CreateQuadMesh();

        // کلايدر برای تشخیص راحت‌تر (Trigger و کمی بلندتر)
        var shadowCollider = shadowMesh.AddComponent<BoxCollider>();
        shadowCollider.isTrigger = true;
        shadowCollider.size = new Vector3(shadowWidth * 1.2f, colliderHeight, shadowLength * 1.1f);
        shadowCollider.center = new Vector3(0f, colliderCenterY, shadowLength / 2f);

        shadowMesh.tag = "Shadow";

        // یک بار هم پوزیشن جهانی را دقیقاً بچسبانیم به کف
        shadowMesh.transform.position = GetBottomWorldPosition();
        shadowMesh.transform.rotation = Quaternion.identity;
    }

    // پایین‌ترین نقطه در فضای محلی نسبت به پرنت
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

        // fallback
        return new Vector3(0, -0.4f, 0);
    }

    // پایین‌ترین نقطه در فضای جهانی
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

        return transform.position; // fallback
    }

    public void UpdateShadow(Vector3 sunPosition)
    {
        if (shadowMesh == null || shadowRenderer == null || isMerged)
            return;

        if (sunPosition == lastSunPosition)
            return;

        // جهت سایه (روی زمین)
        Vector3 direction = (transform.position - sunPosition).normalized;
        direction.y = 0f;

        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        // چرخش نرم
        Quaternion targetRotation = Quaternion.Euler(0f, angle, 0f);
        shadowMesh.transform.rotation = Quaternion.Lerp(
            shadowMesh.transform.rotation,
            targetRotation,
            Time.deltaTime * 8f
        );

        // سایه در پایین‌ترین نقطه پرنت می‌نشیند
        shadowMesh.transform.position = GetBottomWorldPosition();

        lastSunPosition = sunPosition;

        // بررسی ادغام
        CheckForMerge();
    }

    void CheckForMerge()
    {
        if (isMerged || shadowMesh == null) return;

        // جعبه‌ای در امتداد سایه برای تشخیص هم‌پوشانی
        Collider[] nearbyColliders = Physics.OverlapBox(
            shadowMesh.transform.position + shadowMesh.transform.forward * shadowLength / 2f,
            new Vector3(shadowWidth / 2f, colliderHeight / 2f, shadowLength / 2f),
            shadowMesh.transform.rotation,
            mergeDetectionLayer
        );

        foreach (var col in nearbyColliders)
        {
            ShadowProjector otherShadow = col.GetComponentInParent<ShadowProjector>();
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

        // Feedback تصویری
        if (shadowRenderer != null && shadowRenderer.material != null)
            shadowRenderer.material.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        if (otherShadow.shadowRenderer != null && otherShadow.shadowRenderer.material != null)
            otherShadow.shadowRenderer.material.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        // افکت/صدا (اختیاری)
        ParticleManager.Instance?.PlayShadowMerge(mergePosition);
        AudioManager.Instance?.PlayShadowMerge();

        // آنالیتیکس (اختیاری)
        if (GameManager.Instance != null)
            AnalyticsTracker.TrackShadowMerge(GameManager.Instance.currentLevel, 1);

        // کومبو (اختیاری)
        ComboSystem.Instance?.AddShadowMerge();

        Debug.Log($"✨ سایه‌ها ادغام شدند: {gameObject.name} + {otherShadow.gameObject.name}");
    }

    // نسخه‌ی اصلی که DropPathFollower ازش استفاده می‌کند
    public bool IsPointInShadow(Vector3 worldPoint)
    {
        if (shadowMesh == null || shadowRenderer == null) return false;

        // ۱) چک سریع Bounds با کمی توسعه
        Bounds b = shadowRenderer.bounds;
        b.Expand(0.3f);
        if (!b.Contains(worldPoint)) return false;

        // ۲) چک دقیق در فضای محلی
        Vector3 local = shadowMesh.transform.InverseTransformPoint(worldPoint);
        return (local.x >= -shadowWidth / 2f && local.x <= shadowWidth / 2f) &&
               (local.z >= 0f && local.z <= shadowLength) &&
               (local.y >= -yTolerance && local.y <= yTolerance);
    }

    // نگه داشته شد برای سازگاری اگر جای دیگری صدا زده می‌شه
    public bool IsPointInShadowImproved(Vector3 worldPoint) => IsPointInShadow(worldPoint);

    // نگه‌داشتن API قبلی
    public bool IsMerged() => isMerged;

    Material CreateDefaultShadowMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        // Transparent
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
    // شدت سایه در نقطه‌ی worldPoint (۰=لبه/بیرون، ۱=مرکز)
    public float GetShadowStrength(Vector3 worldPoint)
    {
        if (shadowMesh == null || !IsPointInShadow(worldPoint))
            return 0f;

        // به فضای محلی سایه ببریم
        Vector3 local = shadowMesh.transform.InverseTransformPoint(worldPoint);

        // مرکز مستطیلِ سایه در فضای محلی
        float halfW = shadowWidth * 0.5f;
        float halfL = shadowLength * 0.5f;
        float cx = 0f;
        float cz = halfL;

        // فاصله‌های نرمال‌شده تا مرکز در دو محور
        float dx = Mathf.Abs(local.x - cx) / halfW;     // 0 در مرکز تا 1 در لبه x
        float dz = Mathf.Abs(local.z - cz) / halfL;     // 0 در مرکز تا 1 در لبه z

        // قدرت سایه را بر اساس دوری از مرکز بگیریم (لبه صفر، مرکز یک)
        float strength = 1f - Mathf.Max(dx, dz);

        return Mathf.Clamp01(strength);
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
            Gizmos.color = isMerged ? Color.red : Color.blue;
            Gizmos.matrix = shadowMesh.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(
                new Vector3(0f, colliderCenterY, shadowLength / 2f),
                new Vector3(shadowWidth, colliderHeight, shadowLength)
            );
        }
    }
}
