using UnityEngine;

public class SunController : MonoBehaviour
{
    [Header("Sun Settings")]
    public float sunRadius = 10f;
    public float minAngle = -60f;
    public float maxAngle = 60f;
    public Light sunLight;

    [Header("Shadow System")]
    public LayerMask shadowCasterLayer = -1;
    public Material shadowMaterial;

    private float currentAngle = 0f;
    private Camera shadowCamera;
    private Vector3 sunCenter;
    private bool isDragging = false;

    void Start()
    {
        sunCenter = transform.position;
        SetupShadowSystem();
        UpdateSunPosition();
    }

    void SetupShadowSystem()
    {
        // Create shadow camera for custom shadow rendering
        GameObject shadowCamObj = new GameObject("ShadowCamera");
        shadowCamera = shadowCamObj.AddComponent<Camera>();
        shadowCamera.orthographic = true;
        shadowCamera.orthographicSize = 15f;
        shadowCamera.depth = -10;
        shadowCamera.clearFlags = CameraClearFlags.Color;
        shadowCamera.backgroundColor = Color.white;
        shadowCamera.cullingMask = shadowCasterLayer;

        // Position shadow camera to look down
        shadowCamObj.transform.position = new Vector3(0, 20, 0);
        shadowCamObj.transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            float mouseX = Input.mousePosition.x / Screen.width;
            float targetAngle = Mathf.Lerp(minAngle, maxAngle, mouseX);
            currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * 5f);
            UpdateSunPosition();
        }
    }

    void UpdateSunPosition()
    {
        float radianAngle = currentAngle * Mathf.Deg2Rad;
        Vector3 sunPos = sunCenter + new Vector3(
            Mathf.Sin(radianAngle) * sunRadius,
            Mathf.Cos(radianAngle) * sunRadius,
            0
        );

        transform.position = sunPos;

        // Update light direction
        if (sunLight != null)
        {
            sunLight.transform.position = sunPos;
            sunLight.transform.LookAt(sunCenter);
        }

        // Update all shadow projectors
        UpdateShadows();
    }

    void UpdateShadows()
    {
        ShadowProjector[] projectors = FindObjectsOfType<ShadowProjector>();
        foreach (var projector in projectors)
        {
            projector.UpdateShadow(transform.position);
        }
    }

    public float GetCurrentAngle()
    {
        return currentAngle;
    }
}