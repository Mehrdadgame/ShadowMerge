using UnityEngine;


[System.Serializable]
public class Waypoint
{
    public Transform transform;
    public bool isRequired = true; // آیا باید از این نقطه عبور کرد؟
    public float waitTime = 0f; // زمان توقف در این نقطه
    public bool mustBeInShadow = false; // آیا باید در سایه باشد؟

    [Header("Visual")]
    public Color gizmoColor = Color.white;
    public float gizmoSize = 0.5f;
}