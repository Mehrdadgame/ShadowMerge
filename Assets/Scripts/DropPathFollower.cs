using System.Collections;
using UnityEngine;

public class DropPathFollower : MonoBehaviour
{
    [Header("Path Following")]
    public WaypointPath path;
    public float moveSpeed = 3f;
    public float rotationSpeed = 8f;
    public float arrivalThreshold = 0.3f;

    [Header("Shadow Detection - TRIGGER BASED")]
    public bool debugShadowDetection = true;

    // Private variables
    private int currentWaypointIndex = 0;
    private bool isMoving = false;
    private Rigidbody rb;

    // Shadow detection - فقط Trigger
    private bool inShadow = false;
    private ShadowProjector currentShadow = null;

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
    }

    // ===== TRIGGER-BASED SHADOW DETECTION =====
    void OnTriggerEnter(Collider other)
    {
        // بررسی اگر Shadow است
        if (other.CompareTag("Shadow"))
        {
            ShadowProjector shadow = other.GetComponentInParent<ShadowProjector>();
            if (shadow != null)
            {
                inShadow = true;
                currentShadow = shadow;

                if (debugShadowDetection)
                {
                    Debug.Log($"🛡️ وارد سایه شد: {shadow.name}");
                }
            }
        }

        // WaterPickup هم اینجا
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

    void OnTriggerStay(Collider other)
    {
        // اطمینان از اینکه هنوز در سایه هستیم
        if (other.CompareTag("Shadow") && !inShadow)
        {
            ShadowProjector shadow = other.GetComponentInParent<ShadowProjector>();
            if (shadow != null)
            {
                inShadow = true;
                currentShadow = shadow;

                if (debugShadowDetection)
                {
                    Debug.Log($"🛡️ هنوز در سایه: {shadow.name}");
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // خروج از سایه
        if (other.CompareTag("Shadow"))
        {
            ShadowProjector shadow = other.GetComponentInParent<ShadowProjector>();
            if (shadow != null && currentShadow == shadow)
            {
                inShadow = false;
                currentShadow = null;

                if (debugShadowDetection)
                {
                    Debug.Log($"☀️ از سایه خارج شد: {shadow.name}");
                }
            }
        }
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
            }

            // توقف در waypoint
            if (currentWP.waitTime > 0)
            {
                yield return new WaitForSeconds(currentWP.waitTime);
            }

            currentWaypointIndex++;
        }

        // پایان مسیر
        isMoving = false;
        Debug.Log("🎯 به پایان مسیر رسیدیم!");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.WinLevel();
        }
    }

    IEnumerator MoveToWaypoint(int waypointIndex)
    {
        if (waypointIndex >= path.GetWaypointCount()) yield break;

        Vector3 targetPos = path.GetWaypointPosition(waypointIndex);

        while (Vector3.Distance(transform.position, targetPos) > arrivalThreshold)
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameStarted())
            {
                yield return null;
                continue;
            }

            Vector3 direction = (targetPos - transform.position).normalized;

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

    // Public methods برای GameManager
    public bool IsInShadow() => inShadow;
    public ShadowProjector GetCurrentShadow() => currentShadow;
    public bool IsMoving() => isMoving;
    public int GetCurrentWaypointIndex() => currentWaypointIndex;

    void OnDrawGizmosSelected()
    {
        // نمایش وضعیت سایه
        Gizmos.color = inShadow ? Color.blue : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Current waypoint target
        if (path != null && currentWaypointIndex < path.GetWaypointCount())
        {
            Gizmos.color = Color.green;
            Vector3 targetPos = path.GetWaypointPosition(currentWaypointIndex);
            Gizmos.DrawLine(transform.position, targetPos);
            Gizmos.DrawWireSphere(targetPos, arrivalThreshold);
        }

        // اتصال به سایه فعلی
        if (Application.isPlaying && inShadow && currentShadow != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentShadow.transform.position);
        }
    }
}