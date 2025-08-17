using UnityEngine;

public class DeveloperHelper : MonoBehaviour
{
    [Header("Debug Options")]
    public bool showShadowBounds = true;
    public bool showPlayerPath = true;
    public bool showSunAngle = true;

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (showShadowBounds)
        {
            DrawShadowBounds();
        }

        if (showPlayerPath)
        {
            DrawPlayerPath();
        }

        if (showSunAngle)
        {
            DrawSunAngle();
        }
    }

    void DrawShadowBounds()
    {
        ShadowProjector[] shadows = FindObjectsOfType<ShadowProjector>();
        foreach (var shadow in shadows)
        {
            Gizmos.color = shadow.IsMerged() ? Color.red : Color.blue;
            if (shadow.transform != null)
            {
                Gizmos.DrawWireCube(shadow.transform.position, Vector3.one);
            }
        }
    }

    void DrawPlayerPath()
    {
        WaterDrop player = FindObjectOfType<WaterDrop>();
        if (player != null && player.GetCurrentShadow() != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(player.transform.position, player.GetCurrentShadow().transform.position);
        }
    }

    void DrawSunAngle()
    {
        SunController sun = FindObjectOfType<SunController>();
        if (sun != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 sunPos = sun.transform.position;
            Vector3 center = Vector3.zero;
            Gizmos.DrawLine(center, sunPos);
            Gizmos.DrawWireSphere(sunPos, 0.5f);
        }
    }
}