using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    [Header("Path Settings")]
    public Waypoint[] waypoints;
    public bool isLooped = false;
    public bool showPathInEditor = true;

    [Header("Auto Generate")]
    [SerializeField] private bool autoCreateWaypoints = false;
    [SerializeField] private int waypointCount = 5;

    void OnValidate()
    {
        if (autoCreateWaypoints && waypoints.Length == 0)
        {
            CreateDefaultWaypoints();
            autoCreateWaypoints = false;
        }
    }

    void CreateDefaultWaypoints()
    {
        waypoints = new Waypoint[waypointCount];

        for (int i = 0; i < waypointCount; i++)
        {
            GameObject wpObj = new GameObject($"Waypoint_{i:00}");
            wpObj.transform.SetParent(transform);

            // مسیر خطی از (0,0,-5) تا (0,0,5)
            float t = (float)i / (waypointCount - 1);
            wpObj.transform.position = Vector3.Lerp(
                new Vector3(0, 0, -5),
                new Vector3(0, 0, 5),
                t
            );

            waypoints[i] = new Waypoint
            {
                transform = wpObj.transform,
                isRequired = true,
                mustBeInShadow = i > 0 && i < waypointCount - 1, // نقاط میانی باید در سایه باشند
                gizmoColor = i == 0 ? Color.green : (i == waypointCount - 1 ? Color.red : Color.yellow)
            };
        }
    }

    public Vector3 GetWaypointPosition(int index)
    {
        if (index < 0 || index >= waypoints.Length) return Vector3.zero;
        return waypoints[index].transform.position;
    }

    public Waypoint GetWaypoint(int index)
    {
        if (index < 0 || index >= waypoints.Length) return null;
        return waypoints[index];
    }

    public int GetWaypointCount() => waypoints.Length;

    void OnDrawGizmos()
    {
        if (!showPathInEditor || waypoints == null) return;

        // رسم waypoints
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i]?.transform == null) continue;

            Gizmos.color = waypoints[i].gizmoColor;
            Gizmos.DrawWireSphere(waypoints[i].transform.position, waypoints[i].gizmoSize);

            // شماره waypoint
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                waypoints[i].transform.position + Vector3.up * 0.5f,
                i.ToString(),
                new GUIStyle { normal = { textColor = waypoints[i].gizmoColor } }
            );
#endif

            // اتصال به waypoint بعدی
            if (i < waypoints.Length - 1)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(
                    waypoints[i].transform.position,
                    waypoints[i + 1].transform.position
                );
            }
            else if (isLooped && waypoints.Length > 2)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(
                    waypoints[i].transform.position,
                    waypoints[0].transform.position
                );
            }
        }
    }
}