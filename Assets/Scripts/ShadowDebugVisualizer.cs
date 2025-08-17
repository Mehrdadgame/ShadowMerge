using UnityEngine;

// ====== Shadow Debug Visualizer - اضافه کنید به پروژه ======
using UnityEngine;

public class ShadowDebugVisualizer : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableDebug = true;
    public bool drawOverlapSphere = true;
    public bool showShadowBounds = true;
    public bool showRealTimeInfo = true;
    public Color overlapSphereColor = Color.yellow;
    public Color shadowBoundColor = Color.blue;

    [Header("Performance")]
    public float debugUpdateInterval = 0.2f;

    private DropPathFollower player;
    private float lastDebugTime;
    private bool lastShadowState;
    private string debugInfo = "";

    void Start()
    {
        player = FindObjectOfType<DropPathFollower>();
        if (player == null)
        {
            Debug.LogWarning("⚠️ ShadowDebugVisualizer: DropPathFollower not found!");
        }
    }

    void Update()
    {
        if (!enableDebug || player == null) return;

        // Update debug info periodically for performance
        if (Time.time - lastDebugTime > debugUpdateInterval)
        {
            UpdateDebugInfo();
            lastDebugTime = Time.time;
        }

        // Draw real-time info on screen
        if (showRealTimeInfo)
        {
            DrawDebugGUI();
        }
    }

    void UpdateDebugInfo()
    {
        bool currentShadowState = player.IsInShadow();
        ShadowProjector currentShadow = player.GetCurrentShadow();

        // Log shadow state changes
        if (currentShadowState != lastShadowState)
        {
            string stateChange = currentShadowState ? "ENTERED SHADOW" : "LEFT SHADOW";
            string shadowName = currentShadow?.name ?? "None";
            Debug.Log($"🔄 {stateChange} | Shadow: {shadowName} | Time: {Time.time:F1}s");
        }

        // Build debug info string
        debugInfo = $"Shadow Status: {(currentShadowState ? "IN SHADOW ✅" : "IN SUNLIGHT ❌")}\n";
        debugInfo += $"Current Shadow: {(currentShadow?.name ?? "None")}\n";
        debugInfo += $"Waypoint: {player.GetCurrentWaypointIndex()}\n";
        debugInfo += $"Moving: {(player.IsMoving() ? "Yes" : "No")}\n";
        debugInfo += $"Position: {player.transform.position}\n";

        // Physics debug
        debugInfo += "\n--- Physics Debug ---\n";
        debugInfo += $"Shadow Layer Mask: {player.shadowLayerMask.value}\n";
        debugInfo += $"Check Radius: {player.shadowCheckRadius}\n";

        // Test overlap sphere
        Collider[] overlaps = Physics.OverlapSphere(
            player.transform.position,
            player.shadowCheckRadius,
            player.shadowLayerMask
        );
        debugInfo += $"Overlapping Colliders: {overlaps.Length}\n";

        for (int i = 0; i < Mathf.Min(overlaps.Length, 3); i++)
        {
            debugInfo += $"  - {overlaps[i].name} (Layer: {overlaps[i].gameObject.layer})\n";
        }

        lastShadowState = currentShadowState;
    }

    void DrawDebugGUI()
    {
        // Draw debug info on screen
        GUI.color = Color.white;
        GUI.backgroundColor = new Color(0, 0, 0, 0.7f);
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.alignment = TextAnchor.UpperLeft;
        boxStyle.fontSize = 14;

        GUILayout.BeginArea(new Rect(10, 10, 350, 300));
        GUILayout.Box(debugInfo, boxStyle, GUILayout.ExpandHeight(true));
        GUILayout.EndArea();

        // Shadow state indicator
        GUI.color = player.IsInShadow() ? Color.green : Color.red;
        GUI.Box(new Rect(10, 320, 100, 30), player.IsInShadow() ? "IN SHADOW" : "IN SUNLIGHT");
        GUI.color = Color.white;
    }

    void OnDrawGizmos()
    {
        if (!enableDebug || player == null) return;

        Vector3 playerPos = player.transform.position;

        // Draw overlap sphere
        if (drawOverlapSphere)
        {
            Gizmos.color = overlapSphereColor;
            Gizmos.DrawWireSphere(playerPos, player.shadowCheckRadius);

            // Draw filled sphere with transparency
            Color fillColor = overlapSphereColor;
            fillColor.a = 0.2f;
            Gizmos.color = fillColor;
            Gizmos.DrawSphere(playerPos, player.shadowCheckRadius);
        }

        // Draw all shadow bounds
        if (showShadowBounds)
        {
            DrawAllShadowBounds();
        }

        // Draw player to current shadow connection
        ShadowProjector currentShadow = player.GetCurrentShadow();
        if (currentShadow != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(playerPos, currentShadow.transform.position);

            // Draw current shadow highlight
            if (currentShadow.shadowMesh != null)
            {
                Bounds shadowBounds = currentShadow.shadowRenderer.bounds;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(shadowBounds.center, shadowBounds.size);
            }
        }
    }

    void DrawAllShadowBounds()
    {
        ShadowProjector[] allShadows = FindObjectsOfType<ShadowProjector>();

        foreach (var shadow in allShadows)
        {
            if (shadow.shadowMesh == null || shadow.shadowRenderer == null) continue;

            // Different colors for different shadow states
            if (shadow.IsMerged())
            {
                Gizmos.color = Color.red; // Merged shadows
            }
            else if (shadow == player.GetCurrentShadow())
            {
                Gizmos.color = Color.green; // Current shadow
            }
            else
            {
                Gizmos.color = shadowBoundColor; // Normal shadows
            }

            Bounds bounds = shadow.shadowRenderer.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            // Draw shadow direction
            Vector3 shadowStart = shadow.transform.position;
            Vector3 shadowEnd = shadow.shadowMesh.transform.position +
                               shadow.shadowMesh.transform.forward * shadow.shadowLength;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(shadowStart, shadowEnd);

            // Label shadows
#if UNITY_EDITOR
            UnityEditor.Handles.Label(bounds.center + Vector3.up * 0.5f, shadow.name);
#endif
        }
    }

    // Manual testing methods
    [ContextMenu("Test Shadow Detection")]
    public void TestShadowDetection()
    {
        if (player == null)
        {
            Debug.LogError("❌ Player not found!");
            return;
        }

        Debug.Log("🔍 Testing Shadow Detection...");

        // Test 1: Physics.OverlapSphere
        Collider[] overlaps = Physics.OverlapSphere(
            player.transform.position,
            player.shadowCheckRadius,
            player.shadowLayerMask
        );

        Debug.Log($"Physics.OverlapSphere found {overlaps.Length} colliders:");
        foreach (var collider in overlaps)
        {
            Debug.Log($"  - {collider.name} (Layer: {collider.gameObject.layer})");

            ShadowProjector shadow = collider.GetComponentInParent<ShadowProjector>();
            if (shadow != null)
            {
                bool isPointInShadow = shadow.IsPointInShadow(player.transform.position);
                Debug.Log($"    → ShadowProjector: {shadow.name} | Point in shadow: {isPointInShadow}");
            }
        }

        // Test 2: Direct ShadowProjector check
        ShadowProjector[] allShadows = FindObjectsOfType<ShadowProjector>();
        Debug.Log($"\nDirect check of {allShadows.Length} ShadowProjectors:");

        foreach (var shadow in allShadows)
        {
            bool isPointInShadow = shadow.IsPointInShadow(player.transform.position);
            float distance = Vector3.Distance(player.transform.position, shadow.transform.position);

            Debug.Log($"  - {shadow.name}: Point in shadow: {isPointInShadow} | Distance: {distance:F2}");
        }

        // Final result
        bool finalResult = player.IsInShadow();
        ShadowProjector currentShadow = player.GetCurrentShadow();
        Debug.Log($"\n🎯 Final Result: {(finalResult ? "IN SHADOW" : "NOT IN SHADOW")} | Current: {currentShadow?.name ?? "None"}");
    }

    [ContextMenu("Test All Layers")]
    public void TestAllLayers()
    {
        Debug.Log("🔍 Testing All Layers...");

        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName))
            {
                Debug.Log($"Layer {i}: {layerName}");
            }
        }

        // Test shadow meshes layers
        ShadowProjector[] shadows = FindObjectsOfType<ShadowProjector>();
        Debug.Log($"\nShadow Mesh Layers:");
        foreach (var shadow in shadows)
        {
            if (shadow.shadowMesh != null)
            {
                int layer = shadow.shadowMesh.layer;
                string layerName = LayerMask.LayerToName(layer);
                Debug.Log($"  - {shadow.name}: Layer {layer} ({layerName})");
            }
        }
    }

    [ContextMenu("Force Shadow State")]
    public void ForceShadowState()
    {
        if (player == null) return;

        // Temporarily move player to first shadow found
        ShadowProjector[] shadows = FindObjectsOfType<ShadowProjector>();
        if (shadows.Length > 0)
        {
            Vector3 shadowCenter = shadows[0].transform.position +
                                 shadows[0].transform.forward * (shadows[0].shadowLength * 0.3f);
            player.transform.position = shadowCenter;
            Debug.Log($"🚀 Moved player to {shadows[0].name} shadow");
        }
    }
}

// ====== Physics Debug Extension ======
public static class PhysicsDebugExtensions
{
    public static void DebugOverlapSphere(Vector3 position, float radius, LayerMask layerMask, float duration = 1f)
    {
        Gizmos.DrawWireSphere(position, radius);

        Collider[] results = Physics.OverlapSphere(position, radius, layerMask);

        Debug.Log($"🔍 OverlapSphere at {position} | Radius: {radius} | LayerMask: {layerMask.value} | Found: {results.Length}");

        foreach (var result in results)
        {
            Debug.Log($"  → {result.name} (Layer: {result.gameObject.layer})");
            Debug.DrawLine(position, result.transform.position, Color.green, duration);
        }
    }

    public static void DrawWireSphere(Vector3 center, float radius, Color color, float duration = 0f)
    {
        Vector3 forward = Vector3.forward * radius;
        Vector3 up = Vector3.up * radius;
        Vector3 right = Vector3.right * radius;

        // Draw circles
        for (int i = 0; i < 16; i++)
        {
            float angle = i * 22.5f * Mathf.Deg2Rad;
            float nextAngle = (i + 1) * 22.5f * Mathf.Deg2Rad;

            // XY plane
            Vector3 p1 = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            Vector3 p2 = center + new Vector3(Mathf.Cos(nextAngle), Mathf.Sin(nextAngle), 0) * radius;
            Debug.DrawLine(p1, p2, color, duration);

            // XZ plane
            p1 = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            p2 = center + new Vector3(Mathf.Cos(nextAngle), 0, Mathf.Sin(nextAngle)) * radius;
            Debug.DrawLine(p1, p2, color, duration);

            // YZ plane
            p1 = center + new Vector3(0, Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            p2 = center + new Vector3(0, Mathf.Cos(nextAngle), Mathf.Sin(nextAngle)) * radius;
            Debug.DrawLine(p1, p2, color, duration);
        }
    }
}

// ====== Enhanced UI Manager for Path Progress ======
// اضافه کنید به UI_Manager.cs:

/*
public class UI_Manager_PathExtension : MonoBehaviour
{
    [Header("Path Progress")]
    public Text pathProgressText;
    public Slider pathProgressSlider;
    
    private DropPathFollower pathFollower;
    
    void Start()
    {
        pathFollower = FindObjectOfType<DropPathFollower>();
        
        // Create path progress UI if not exists
        if (pathProgressText == null)
        {
            CreatePathProgressUI();
        }
    }
    
    void CreatePathProgressUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        
        GameObject progressPanel = new GameObject("PathProgressPanel");
        progressPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform rect = progressPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.02f, 0.85f);
        rect.anchorMax = new Vector2(0.35f, 0.98f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        Image bgImage = progressPanel.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.4f);
        
        // Progress text
        GameObject textObj = new GameObject("PathProgressText");
        textObj.transform.SetParent(progressPanel.transform, false);
        
        pathProgressText = textObj.AddComponent<Text>();
        pathProgressText.text = "🛤️ مسیر: 0/5";
        pathProgressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        pathProgressText.fontSize = 24;
        pathProgressText.color = Color.white;
        pathProgressText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = pathProgressText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
    
    void Update()
    {
        UpdatePathProgress();
    }
    
    void UpdatePathProgress()
    {
        if (pathFollower == null || pathProgressText == null) return;
        
        WaypointPath path = pathFollower.path;
        if (path == null) return;
        
        int currentWaypoint = pathFollower.GetCurrentWaypointIndex();
        int totalWaypoints = path.GetWaypointCount();
        
        pathProgressText.text = $"🛤️ مسیر: {currentWaypoint}/{totalWaypoints}";
        
        // Color based on progress
        float progress = (float)currentWaypoint / totalWaypoints;
        pathProgressText.color = Color.Lerp(Color.white, Color.green, progress);
        
        // Update slider if exists
        if (pathProgressSlider != null)
        {
            pathProgressSlider.value = progress;
        }
    }
}
*/

// ====== Runtime Test Commands ======
#if UNITY_EDITOR
[System.Serializable]
public class ShadowSystemTester
{
    [UnityEditor.MenuItem("Last Drop/Runtime Tests/Test Shadow Physics")]
    public static void TestShadowPhysicsRuntime()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ این تست باید در حین اجرای بازی انجام شود!");
            return;
        }

        DropPathFollower player = Object.FindObjectOfType<DropPathFollower>();
        if (player == null)
        {
            Debug.LogError("❌ Player not found!");
            return;
        }

        Debug.Log("🔍 Runtime Shadow Physics Test...");

        Vector3 playerPos = player.transform.position;

        // Test with different radii
        float[] testRadii = { 0.1f, 0.3f, 0.5f, 1.0f };

        foreach (float radius in testRadii)
        {
            Collider[] results = Physics.OverlapSphere(playerPos, radius, player.shadowLayerMask);
            Debug.Log($"  Radius {radius}: {results.Length} colliders found");

            foreach (var collider in results)
            {
                Debug.Log($"    - {collider.name} (Layer: {collider.gameObject.layer})");
            }
        }
    }

    [UnityEditor.MenuItem("Last Drop/Runtime Tests/Force Shadow Entry")]
    public static void ForceShadowEntry()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ این تست باید در حین اجرای بازی انجام شود!");
            return;
        }

        DropPathFollower player = Object.FindObjectOfType<DropPathFollower>();
        ShadowProjector[] shadows = Object.FindObjectsOfType<ShadowProjector>();

        if (player == null || shadows.Length == 0)
        {
            Debug.LogError("❌ Player یا Shadow یافت نشد!");
            return;
        }

        // Move player to first shadow center
        ShadowProjector firstShadow = shadows[0];
        Vector3 shadowCenter = firstShadow.transform.position +
                              firstShadow.transform.forward * (firstShadow.shadowLength * 0.5f);
        shadowCenter.y = player.transform.position.y;

        player.transform.position = shadowCenter;

        Debug.Log($"🚀 Player moved to {firstShadow.name} shadow center");

        // Wait a frame then test
        UnityEditor.EditorApplication.delayCall += () =>
        {
            bool inShadow = player.IsInShadow();
            Debug.Log($"🎯 Result: Player is {(inShadow ? "IN SHADOW ✅" : "NOT IN SHADOW ❌")}");
        };
    }
}
#endif