using System.Collections;
using UnityEngine;


public class DropPathFollower : MonoBehaviour
{
    [Header("Path Following")]
    public WaypointPath path;
    public float moveSpeed = 3f;
    public float rotationSpeed = 8f;
    public float arrivalThreshold = 0.3f;

    [Header("Shadow Detection - FIXED")]
    public LayerMask shadowLayerMask = 1 << 8; // Layer 8 = Shadow
    public float shadowCheckRadius = 0.4f;
    public bool debugShadowDetection = true;

    // [Header("Visual Effects")]
    // public ParticleSystem movementTrail;
    // public Transform dropletModel;

    // Private variables
    private int currentWaypointIndex = 0;
    private bool isMoving = false;
    private Rigidbody rb;
    private bool inShadow = false;
    private ShadowProjector currentShadow;

    // Shadow detection - چندگانه
    private Collider[] shadowColliders = new Collider[10];

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        gameObject.tag = "Player";

        // شروع حرکت
        if (path != null && path.GetWaypointCount() > 0)
        {
            transform.position = path.GetWaypointPosition(0);
            StartCoroutine(FollowPath());
        }
        else
        {
            Debug.LogError("❌ Path تنظیم نشده! WaypointPath component اضافه کنید.");
        }

        StartCoroutine(ShadowDetectionLoop());
    }

    IEnumerator FollowPath()
    {
        isMoving = true;

        while (currentWaypointIndex < path.GetWaypointCount())
        {
            yield return StartCoroutine(MoveToWaypoint(currentWaypointIndex));

            Waypoint currentWP = path.GetWaypoint(currentWaypointIndex);

            // بررسی شرایط waypoint
            if (currentWP.mustBeInShadow && !inShadow)
            {
                Debug.LogWarning($"⚠️ Waypoint {currentWaypointIndex} باید در سایه باشد ولی نیست!");
                // می‌تونید عملکرد خاصی اضافه کنید (مثل توقف یا جریمه)
            }

            // توقف در waypoint (اگر تعریف شده)
            if (currentWP.waitTime > 0)
            {
                yield return new WaitForSeconds(currentWP.waitTime);
            }

            currentWaypointIndex++;
        }

        // پایان مسیر
        isMoving = false;
        Debug.Log("🎯 به پایان مسیر رسیدیم!");

        // اعلام برد به GameManager
        if (GameManager.Instance != null)
        {
            // فرض می‌کنیم آخرین waypoint همان levelEndPoint است
            GameManager.Instance.WinLevel(); // این method را public کنید
        }
    }

    IEnumerator MoveToWaypoint(int waypointIndex)
    {
        if (waypointIndex >= path.GetWaypointCount() && !GameManager.Instance.IsGameStarted()) yield break;

        Vector3 targetPos = path.GetWaypointPosition(waypointIndex);

        while (Vector3.Distance(transform.position, targetPos) > arrivalThreshold)
        {
            // محاسبه جهت حرکت
            Vector3 direction = (targetPos - transform.position).normalized;

            // حرکت
            if (rb != null)
            {
                Vector3 moveVector = direction * moveSpeed * Time.fixedDeltaTime;
                rb.MovePosition(transform.position + moveVector);
            }
            else
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );
            }

            // چرخش به سمت حرکت
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            yield return null;
        }

        Debug.Log($"✅ رسیدیم به Waypoint {waypointIndex}");
    }

    // ====== FIXED Shadow Detection System ======
    IEnumerator ShadowDetectionLoop()
    {
        while (true)
        {
            CheckShadowStatus();
            yield return new WaitForSeconds(0.1f); // بررسی هر 100ms
        }
    }

    void CheckShadowStatus()
    {
        inShadow = false;
        currentShadow = null;

        // روش ۱: Physics.OverlapSphere - دقیق‌ترین روش
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            shadowCheckRadius,
            shadowColliders,
            shadowLayerMask
        );

        if (hitCount > 0)
        {
            for (int i = 0; i < hitCount; i++)
            {
                ShadowProjector shadow = shadowColliders[i].GetComponentInParent<ShadowProjector>();
                if (shadow != null && shadow.IsPointInShadow(transform.position))
                {
                    inShadow = true;
                    currentShadow = shadow;
                    break;
                }
            }
        }

        // روش ۲: مستقیم از ShadowProjector ها (backup)
        if (!inShadow)
        {
            ShadowProjector[] allShadows = Object.FindObjectsByType<ShadowProjector>(FindObjectsSortMode.None);
            foreach (var shadow in allShadows)
            {
                if (shadow.IsPointInShadow(transform.position))
                {
                    inShadow = true;
                    currentShadow = shadow;
                    break;
                }
            }
        }

        if (debugShadowDetection)
        {
            Debug.Log($"🔍 Shadow Status: {(inShadow ? "IN SHADOW" : "IN SUNLIGHT")} | Current: {currentShadow?.name}");
        }
    }

    // Getters برای GameManager
    public bool IsInShadow() => inShadow;
    public ShadowProjector GetCurrentShadow() => currentShadow;
    public bool IsMoving() => isMoving;
    public int GetCurrentWaypointIndex() => currentWaypointIndex;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WaterPickup"))
        {
            WaterPickup pickup = other.GetComponent<WaterPickup>();
            if (pickup != null)
            {
                GameManager.Instance?.AddWater(pickup.waterAmount);
                Destroy(other.gameObject);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Shadow detection radius
        Gizmos.color = inShadow ? Color.blue : Color.red;
        Gizmos.DrawWireSphere(transform.position, shadowCheckRadius);

        // Current waypoint target
        if (path != null && currentWaypointIndex < path.GetWaypointCount())
        {
            Gizmos.color = Color.green;
            Vector3 targetPos = path.GetWaypointPosition(currentWaypointIndex);
            Gizmos.DrawLine(transform.position, targetPos);
            Gizmos.DrawWireSphere(targetPos, arrivalThreshold);
        }
    }
}