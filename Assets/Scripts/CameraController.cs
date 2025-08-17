using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target; // Water Drop
    public Vector3 offset = new Vector3(0, 8, -8);
    public float smoothTime = 0.3f;
    public bool followTarget = false;

    [Header("Screen Shake")]
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.2f;

    private Vector3 velocity = Vector3.zero;
    private Vector3 originalPosition;
    private float shakeTimer = 0f;

    void Start()
    {
        originalPosition = transform.position;
    }

    void LateUpdate()
    {
        if (followTarget && target != null)
        {
            Vector3 targetPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }

        // Screen shake effect
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            Vector3 shakeOffset = Random.insideUnitSphere * shakeIntensity;
            shakeOffset.z = 0; // Keep camera on same Z plane
            transform.position += shakeOffset;
        }
    }

    public void ShakeCamera()
    {
        shakeTimer = shakeDuration;
    }
}