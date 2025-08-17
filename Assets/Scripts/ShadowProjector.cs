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
        shadowMesh.layer = LayerMask.NameToLayer("Shadow");

        MeshFilter meshFilter = shadowMesh.AddComponent<MeshFilter>();
        shadowRenderer = shadowMesh.AddComponent<MeshRenderer>();
        shadowRenderer.material = shadowMaterial;
        shadowRenderer.sortingOrder = -1; // زیر اجسام دیگر

        // Add collider for shadow detection
        BoxCollider shadowCollider = shadowMesh.AddComponent<BoxCollider>();
        shadowCollider.isTrigger = true;
        shadowCollider.size = new Vector3(shadowWidth, 0.1f, shadowLength);
        shadowCollider.center = new Vector3(0, 0, shadowLength / 2);

        // Create quad mesh for shadow
        meshFilter.mesh = CreateQuadMesh();
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
        if (isMerged || sunPosition == lastSunPosition) return;

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
        if (isMerged) return;

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
                // Calculate merge position
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
        shadowRenderer.material.color = new Color(0.3f, 0.3f, 0.3f, 0.8f); // Darker merged shadow
        otherShadow.shadowRenderer.material.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        // Effects
        ParticleManager.Instance?.PlayShadowMerge(mergePosition);
        AudioManager.Instance?.PlayShadowMerge();

        // Analytics
        AnalyticsTracker.TrackShadowMerge(GameManager.Instance.currentLevel, 1);

        Debug.Log($"Shadows merged between {gameObject.name} and {otherShadow.gameObject.name}!");
    }

    public bool IsPointInShadow(Vector3 point)
    {
        if (shadowMesh == null || shadowRenderer == null) return false;

        Bounds shadowBounds = shadowRenderer.bounds;
        shadowBounds.Expand(0.2f); // Small tolerance
        return shadowBounds.Contains(point);
    }

    public bool IsMerged() => isMerged;

    void OnDrawGizmosSelected()
    {
        // Draw shadow bounds in editor
        if (shadowMesh != null)
        {
            Gizmos.color = isMerged ? Color.red : Color.blue;
            Gizmos.matrix = shadowMesh.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0, 0, shadowLength / 2), new Vector3(shadowWidth, 0.1f, shadowLength));
        }
    }
}