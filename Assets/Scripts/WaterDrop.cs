using UnityEngine;
using System.Collections;

public class WaterDrop : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public LayerMask shadowLayer = -1;

    [Header("Visual Effects")]
    public ParticleSystem movementTrail;
    public GameObject dropletModel;

    private bool inShadow = false;
    private Vector3 targetPosition;
    private Rigidbody rb;
    private ShadowProjector currentShadow;
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private const float STUCK_THRESHOLD = 2f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        targetPosition = transform.position;
        lastPosition = transform.position;

        gameObject.tag = "Player";

        StartCoroutine(PathfindingLoop());
        StartCoroutine(StuckDetection());
    }

    void Update()
    {
        CheckShadowStatus();
        MoveToTarget();
        UpdateVisualEffects();
    }

    void CheckShadowStatus()
    {
        inShadow = false;
        currentShadow = null;

        ShadowProjector[] shadows = FindObjectsOfType<ShadowProjector>();
        float closestDistance = Mathf.Infinity;

        foreach (var shadow in shadows)
        {
            if (shadow.IsPointInShadow(transform.position))
            {
                float distance = Vector3.Distance(transform.position, shadow.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    inShadow = true;
                    currentShadow = shadow;
                }
            }
        }
    }

    void MoveToTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        if (distanceToTarget > 0.3f)
        {
            // Move towards target
            Vector3 moveVector = direction * moveSpeed * Time.deltaTime;
            rb.MovePosition(transform.position + moveVector);

            // Rotate towards movement direction
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    void UpdateVisualEffects()
    {
        // Movement trail effect
        if (movementTrail != null)
        {
            var emission = movementTrail.emission;
            emission.enabled = Vector3.Distance(transform.position, lastPosition) > 0.01f;
        }

        // Scale effect based on water level
        float waterPercentage = GameManager.Instance?.GetWaterPercentage() ?? 1f;
        float targetScale = Mathf.Lerp(0.2f, 0.4f, waterPercentage);

        if (dropletModel != null)
        {
            dropletModel.transform.localScale = Vector3.Lerp(
                dropletModel.transform.localScale,
                Vector3.one * targetScale,
                Time.deltaTime * 2f
            );
        }

        lastPosition = transform.position;
    }

    IEnumerator PathfindingLoop()
    {
        while (true)
        {
            if (!inShadow || ShouldFindNewTarget())
            {
                FindBestShadowTarget();
            }

            yield return new WaitForSeconds(0.3f);
        }
    }

    bool ShouldFindNewTarget()
    {
        // Find new target if current shadow is too small or far
        if (currentShadow == null) return true;

        float distanceToCurrentTarget = Vector3.Distance(transform.position, targetPosition);
        return distanceToCurrentTarget < 0.5f; // Reached current target
    }

    void FindBestShadowTarget()
    {
        ShadowProjector[] shadows = FindObjectsOfType<ShadowProjector>();

        if (shadows.Length == 0) return;

        ShadowProjector bestShadow = null;
        float bestScore = -1f;

        foreach (var shadow in shadows)
        {
            float score = EvaluateShadowTarget(shadow);
            if (score > bestScore)
            {
                bestScore = score;
                bestShadow = shadow;
            }
        }

        if (bestShadow != null)
        {
            // Set target to center of shadow
            targetPosition = bestShadow.transform.position + bestShadow.transform.forward * (bestShadow.shadowLength * 0.3f);

            // Ensure target is on ground level
            targetPosition.y = transform.position.y;
        }
    }

    float EvaluateShadowTarget(ShadowProjector shadow)
    {
        if (shadow == null) return -1f;

        float distance = Vector3.Distance(transform.position, shadow.transform.position);
        float proximityScore = 1f / (1f + distance * 0.1f); // Closer = better

        // Prefer larger shadows
        float sizeScore = shadow.shadowLength * shadow.shadowWidth * 0.1f;

        // Prefer merged shadows (they're permanent)
        float stabilityScore = shadow.IsMerged() ? 2f : 1f;

        // Avoid current shadow if we're already in it (encourage movement)
        float diversityScore = (currentShadow == shadow && inShadow) ? 0.5f : 1f;

        return proximityScore * sizeScore * stabilityScore * diversityScore;
    }

    IEnumerator StuckDetection()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (Vector3.Distance(transform.position, lastPosition) < 0.1f)
            {
                stuckTimer += 1f;
                if (stuckTimer > STUCK_THRESHOLD)
                {
                    // Force find new target if stuck
                    FindEmergencyTarget();
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }
    }

    void FindEmergencyTarget()
    {
        // Find any nearby position that's not our current location
        Vector3 randomDirection = Random.insideUnitSphere;
        randomDirection.y = 0;
        randomDirection.Normalize();

        targetPosition = transform.position + randomDirection * 2f;
        Debug.Log("Emergency target set - was stuck!");
    }

    public bool IsInShadow() => inShadow;

    public ShadowProjector GetCurrentShadow() => currentShadow;

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
        else if (other.CompareTag("Finish"))
        {
            // Reached end point
            Debug.Log("Reached finish line!");
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw current target
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPosition, 0.3f);

        // Draw line to target
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, targetPosition);

        // Draw shadow detection area
        if (inShadow && currentShadow != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
    }
}