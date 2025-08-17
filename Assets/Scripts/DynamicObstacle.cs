using UnityEngine;
using System.Collections;

public class DynamicObstacle : MonoBehaviour
{
    [Header("Movement Settings")]
    public bool isMoving = false;
    public Vector3[] waypoints;
    public float moveSpeed = 2f;
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float waitTime = 2f; // زمان توقف در هر نقطه

    [Header("Special Types")]
    public bool isRotating = false;
    public float rotationSpeed = 45f;
    public bool isPulsing = false;
    public float pulseMin = 0.8f;
    public float pulseMax = 1.2f;
    public float pulseSpeed = 2f;

    [Header("Water Effect")]
    public bool givesWaterWhenHidden = true;
    public float waterReward = 15f;

    private int currentWaypointIndex = 0;
    private float journeyTime = 0f;
    private bool isWaiting = false;
    private Vector3 originalScale;
    private ShadowProjector shadowProjector;

    void Start()
    {
        originalScale = transform.localScale;
        shadowProjector = GetComponent<ShadowProjector>();

        if (waypoints.Length == 0)
        {
            // اگر نقطه‌ای تعریف نشده، نقاط تصادفی بساز
            GenerateRandomWaypoints();
        }

        if (isMoving)
        {
            StartCoroutine(MoveAlongWaypoints());
        }
    }

    void GenerateRandomWaypoints()
    {
        waypoints = new Vector3[4];
        Vector3 center = transform.position;

        for (int i = 0; i < waypoints.Length; i++)
        {
            float angle = (i * 90f) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * Random.Range(2f, 4f),
                0,
                Mathf.Sin(angle) * Random.Range(2f, 4f)
            );
            waypoints[i] = center + offset;
        }
    }

    void Update()
    {
        HandleRotation();
        HandlePulsing();
        CheckWaterReward();
    }

    void HandleRotation()
    {
        if (isRotating)
        {
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
    }

    void HandlePulsing()
    {
        if (isPulsing)
        {
            float scale = Mathf.Lerp(pulseMin, pulseMax,
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
            transform.localScale = originalScale * scale;
        }
    }

    void CheckWaterReward()
    {
        if (!givesWaterWhenHidden) return;

        // اگر قطره آب در سایه این مانع باشد، آب اضافه کن
        WaterDrop player = GameManager.Instance?.waterDrop;
        if (player != null && shadowProjector != null)
        {
            if (shadowProjector.IsPointInShadow(player.transform.position))
            {
                // هر 3 ثانیه آب اضافه کن
                if (Time.time % 3f < 0.1f)
                {
                    GameManager.Instance?.AddWater(waterReward * 0.1f); // مقدار کم اما مداوم

                    // افکت
                    ParticleManager.Instance?.PlayWaterCollect(player.transform.position);
                }
            }
        }
    }

    IEnumerator MoveAlongWaypoints()
    {
        while (isMoving && waypoints.Length > 1)
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = waypoints[currentWaypointIndex];

            journeyTime = 0f;
            float distance = Vector3.Distance(startPos, targetPos);
            float journeyDuration = distance / moveSpeed;

            // حرکت به نقطه بعدی
            while (journeyTime < journeyDuration)
            {
                journeyTime += Time.deltaTime;
                float progress = journeyTime / journeyDuration;
                float curvedProgress = movementCurve.Evaluate(progress);

                transform.position = Vector3.Lerp(startPos, targetPos, curvedProgress);
                yield return null;
            }

            // رسیدن به مقصد
            transform.position = targetPos;

            // توقف در نقطه
            if (waitTime > 0f)
            {
                isWaiting = true;
                yield return new WaitForSeconds(waitTime);
                isWaiting = false;
            }

            // نقطه بعدی
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    // برای نمایش مسیر در ادیتور
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        // رسم مسیر
        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length; i++)
        {
            Vector3 currentWaypoint = waypoints[i];
            Vector3 nextWaypoint = waypoints[(i + 1) % waypoints.Length];

            Gizmos.DrawWireSphere(currentWaypoint, 0.3f);
            Gizmos.DrawLine(currentWaypoint, nextWaypoint);
        }

        // نمایش نقطه فعلی
        if (Application.isPlaying && waypoints.Length > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(waypoints[currentWaypointIndex], 0.5f);
        }
    }

    // تغییر سرعت در زمان اجرا
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    // توقف/شروع حرکت
    public void ToggleMovement()
    {
        isMoving = !isMoving;
        if (isMoving)
        {
            StartCoroutine(MoveAlongWaypoints());
        }
    }

    // اضافه کردن نقطه جدید
    public void AddWaypoint(Vector3 newPoint)
    {
        Vector3[] newWaypoints = new Vector3[waypoints.Length + 1];
        for (int i = 0; i < waypoints.Length; i++)
        {
            newWaypoints[i] = waypoints[i];
        }
        newWaypoints[waypoints.Length] = newPoint;
        waypoints = newWaypoints;
    }
}