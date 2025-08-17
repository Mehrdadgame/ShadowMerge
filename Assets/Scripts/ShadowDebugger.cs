using UnityEngine;

public class ShadowDebugger : MonoBehaviour
{
    [Header("Debug Options")]
    public bool showShadowBounds = true;
    public bool showPlayerShadowStatus = true;
    public bool logShadowChanges = true;

    private DropPathFollower player;
    private bool lastShadowState = false;

    void Start()
    {
        player = FindObjectOfType<DropPathFollower>();
    }

    void Update()
    {
        if (player == null) return;

        bool currentShadowState = player.IsInShadow();

        // Log تغییرات وضعیت سایه
        if (logShadowChanges && currentShadowState != lastShadowState)
        {
            string status = currentShadowState ? "ENTERED SHADOW" : "LEFT SHADOW";
            string shadowName = player.GetCurrentShadow()?.name ?? "None";
            Debug.Log($"🔄 {status} | Shadow: {shadowName}");
        }

        lastShadowState = currentShadowState;
    }

    void OnDrawGizmos()
    {
        if (!showShadowBounds) return;

        ShadowProjector[] shadows = FindObjectsOfType<ShadowProjector>();
        foreach (var shadow in shadows)
        {
            if (shadow.shadowMesh != null && shadow.shadowRenderer != null)
            {
                Gizmos.color = shadow.IsMerged() ? Color.red : Color.blue;
                Gizmos.matrix = shadow.shadowMesh.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(
                    new Vector3(0, 0, shadow.shadowLength / 2),
                    new Vector3(shadow.shadowWidth, 0.1f, shadow.shadowLength)
                );
            }
        }

        // Player shadow status
        if (showPlayerShadowStatus && player != null)
        {
            Gizmos.color = player.IsInShadow() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(player.transform.position, 0.6f);
        }
    }
}