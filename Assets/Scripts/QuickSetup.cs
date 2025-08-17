// QuickSetup.cs - اسکریپت راه‌اندازی کامل Last Drop (Fixed UI)
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class QuickSetupFixed : MonoBehaviour
{
    [MenuItem("Last Drop/Setup Scene with Path System")]
    public static void SetupSceneWithPath()
    {
        Debug.Log("🚀 شروع راه‌اندازی Last Drop با Path System...");

        // پاک کردن صحنه
        ClearScene();

        // راه‌اندازی دوربین
        SetupCamera();

        // ایجاد Path System
        GameObject pathSystem = CreatePathSystem();

        // ایجاد محیط متناسب با مسیر
        CreateEnvironmentForPath(pathSystem);

        // ایجاد بازیکن با Path Follower
        CreatePlayerWithPathFollower(pathSystem);

        // ایجاد منیجرها
        CreateManagers();

        // ایجاد UI کامل
        CreateCompleteUI();

        Debug.Log("🎉 Last Drop با Path System راه‌اندازی کامل شد!");
        Debug.Log("🛤️ مسیر شامل 5 waypoint است");
        Debug.Log("🎮 برای تست دکمه Play را بزنید!");
    }

    static void ClearScene()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;
            if (obj == Camera.main?.gameObject) continue;
            Light light = obj.GetComponent<Light>();
            if (light != null && light.type == LightType.Directional) continue;
            DestroyImmediate(obj);
        }
        Debug.Log("🧹 صحنه پاک شد");
    }

    static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }

        cam.transform.position = new Vector3(0, 12, -2);
        cam.transform.rotation = Quaternion.Euler(45, 0, 0);
        cam.orthographic = true;
        cam.orthographicSize = 8;
        cam.backgroundColor = new Color(0.9f, 0.6f, 0.3f);

        CameraController camController = cam.GetComponent<CameraController>();
        if (camController == null)
        {
            camController = cam.gameObject.AddComponent<CameraController>();
        }

        camController.followTarget = true;
        camController.shakeIntensity = 0.2f;
        camController.shakeDuration = 0.4f;
        camController.smoothTime = 0.5f;

        Debug.Log("📷 دوربین راه‌اندازی شد");
    }

    static GameObject CreatePathSystem()
    {
        GameObject pathSystemObj = new GameObject("PathSystem");
        WaypointPath waypointPath = pathSystemObj.AddComponent<WaypointPath>();

        // ایجاد 5 waypoint برای مسیر اصلی
        Waypoint[] waypoints = new Waypoint[5];
        Vector3[] positions = {
            new Vector3(-6, 0.5f, -4),   // شروع (پایین چپ)
            new Vector3(-2, 0.5f, -1),   // waypoint 1 (نزدیک اولین مانع)
            new Vector3(1, 0.5f, 2),     // waypoint 2 (وسط - محل ادغام سایه‌ها)
            new Vector3(3, 0.5f, 4),     // waypoint 3 (نزدیک water pickup)
            new Vector3(6, 0.5f, 6)      // پایان (بالا راست)
        };

        string[] waypointNames = { "Start", "FirstShadow", "MergePoint", "RefillPoint", "Finish" };
        Color[] waypointColors = { Color.green, Color.yellow, Color.magenta, Color.cyan, Color.red };
        bool[] mustBeInShadow = { false, true, true, true, false };

        for (int i = 0; i < waypoints.Length; i++)
        {
            GameObject wpObj = new GameObject($"Waypoint_{i:00}_{waypointNames[i]}");
            wpObj.transform.SetParent(pathSystemObj.transform);
            wpObj.transform.position = positions[i];

            // Visual indicator
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.name = "Indicator";
            indicator.transform.SetParent(wpObj.transform);
            indicator.transform.localPosition = Vector3.zero;
            indicator.transform.localScale = new Vector3(0.3f, 0.05f, 0.3f);

            Material indicatorMat = new Material(Shader.Find("Standard"));
            indicatorMat.color = waypointColors[i];
            indicatorMat.EnableKeyword("_EMISSION");
            indicatorMat.SetColor("_EmissionColor", waypointColors[i] * 0.3f);
            indicator.GetComponent<MeshRenderer>().material = indicatorMat;

            waypoints[i] = new Waypoint
            {
                transform = wpObj.transform,
                isRequired = true,
                mustBeInShadow = mustBeInShadow[i],
                waitTime = i == 2 ? 1f : 0f, // توقف در MergePoint
                gizmoColor = waypointColors[i],
                gizmoSize = 0.4f
            };
        }

        waypointPath.waypoints = waypoints;
        waypointPath.isLooped = false;
        waypointPath.showPathInEditor = true;

        Debug.Log("🛤️ Path System ایجاد شد");
        return pathSystemObj;
    }

    static void CreateEnvironmentForPath(GameObject pathSystem)
    {
        // زمین
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = Vector3.one * 3f;
        ground.transform.position = Vector3.zero;

        Material groundMat = CreateURPMaterial("GroundMaterial");
        groundMat.color = new Color(0.85f, 0.65f, 0.4f);
        ground.GetComponent<MeshRenderer>().material = groundMat;

        // موانع استراتژیک برای ایجاد سایه در مسیر
        CreateStrategicObstacle("Obstacle_Start", new Vector3(-4, 1.5f, -2), new Vector3(1f, 3f, 1f));
        CreateStrategicObstacle("Obstacle_Middle_A", new Vector3(-1, 1.5f, 1), new Vector3(1.5f, 3f, 1.5f));
        CreateStrategicObstacle("Obstacle_Middle_B", new Vector3(2, 1.5f, 1.5f), new Vector3(1.2f, 3f, 1.2f));
        CreateStrategicObstacle("Obstacle_End", new Vector3(4, 1.5f, 5), new Vector3(1f, 3f, 1f));

        // مانع پویا که سایه‌اش تغییر می‌کند
        CreateMovingObstacleForPath("MovingObstacle_Path", new Vector3(0, 1.5f, 3));

        // آب‌های قابل جمع‌آوری در مسیر
        CreateWaterPickupAt(new Vector3(3.2f, 0.6f, 4.2f), 1); // نزدیک RefillPoint
        CreateWaterPickupAt(new Vector3(-1.8f, 0.6f, -0.5f), 2); // نزدیک FirstShadow
        CreateWaterPickupAt(new Vector3(1.2f, 0.6f, 2.2f), 3); // نزدیک MergePoint

        Debug.Log("🌍 محیط استراتژیک برای Path ایجاد شد");
    }

    static void CreateStrategicObstacle(string name, Vector3 position, Vector3 scale)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = name;
        obstacle.transform.position = position;
        obstacle.transform.localScale = scale;

        Material mat = CreateURPMaterial("ObstacleMaterial");
        mat.color = new Color(0.5f, 0.3f, 0.2f);
        obstacle.GetComponent<MeshRenderer>().material = mat;

        // Shadow Projector
        ShadowProjector shadowProj = obstacle.AddComponent<ShadowProjector>();
        shadowProj.shadowMaterial = CreateShadowMaterial();
        shadowProj.shadowLength = scale.z * 3f; // طولانی‌تر برای پوشش بهتر مسیر
        shadowProj.shadowWidth = scale.x * 2f;

        obstacle.tag = "ShadowCaster";
    }

    static void CreateMovingObstacleForPath(string name, Vector3 position)
    {
        CreateStrategicObstacle(name, position, new Vector3(1.2f, 2.5f, 1.2f));
        GameObject obstacle = GameObject.Find(name);

        DynamicObstacle dynamic = obstacle.AddComponent<DynamicObstacle>();
        dynamic.isMoving = true;
        dynamic.moveSpeed = 1.5f; // آهسته‌تر برای کنترل بهتر
        dynamic.waitTime = 2f;

        // مسیر حرکت که سایه را روی path می‌گذارد
        Vector3[] waypoints = new Vector3[3];
        waypoints[0] = position;
        waypoints[1] = position + new Vector3(-2, 0, 1);
        waypoints[2] = position + new Vector3(2, 0, -1);
        dynamic.waypoints = waypoints;

        MeshRenderer renderer = obstacle.GetComponent<MeshRenderer>();
        Material mat = CreateURPMaterial("MovingObstacleMaterial");
        mat.color = new Color(0.7f, 0.3f, 0.7f);
        renderer.material = mat;
    }

    static void CreatePlayerWithPathFollower(GameObject pathSystem)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "WaterDrop";
        player.transform.position = new Vector3(-6, 0.6f, -4); // شروع از اولین waypoint
        player.transform.localScale = Vector3.one * 0.5f;
        player.tag = "Player";

        // متریال آب شفاف
        Material waterMat = CreateTransparentURPMaterial("WaterDropMaterial", new Color(0.1f, 0.6f, 1f, 0.9f));
        player.GetComponent<MeshRenderer>().material = waterMat;

        // Physics
        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.mass = 0.2f;
        rb.linearDamping = 10f;
        rb.angularDamping = 15f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Path Follower Component
        DropPathFollower pathFollower = player.AddComponent<DropPathFollower>();
        pathFollower.path = pathSystem.GetComponent<WaypointPath>();
        pathFollower.moveSpeed = 4f;
        pathFollower.rotationSpeed = 8f;
        pathFollower.arrivalThreshold = 0.4f;
        // pathFollower.shadowLayerMask = 1 << 8; // Shadow layer
        // pathFollower.shadowCheckRadius = 0.5f;
        pathFollower.debugShadowDetection = true;

        // حذف WaterDrop component قدیمی اگر وجود داشت
        WaterDrop oldWaterDrop = player.GetComponent<WaterDrop>();
        if (oldWaterDrop != null)
        {
            DestroyImmediate(oldWaterDrop);
        }

        Debug.Log("💧 بازیکن با Path Follower ایجاد شد");
    }

    static void CreateManagers()
    {
        // Game Manager
        GameObject gameManagerObj = new GameObject("GameManager");
        GameManager gm = gameManagerObj.AddComponent<GameManager>();
        gm.waterDecayRate = 0.5f; // آهسته‌تر در سایه
        gm.sunlightDecayRate = 8f; // سریع‌تر در نور
        gm.levelTime = 90f; // زمان بیشتر برای path following
        gm.startDelay = 3f;
        gm.showCountdown = true;

        // Sun Controller
        GameObject sunControllerObj = new GameObject("SunController");
        SunController sun = sunControllerObj.AddComponent<SunController>();
        sun.sunRadius = 15f;
        sun.minAngle = -80f;
        sun.maxAngle = 80f;

        // نور خورشید
        GameObject sunLightObj = new GameObject("SunLight");
        sunLightObj.transform.parent = sunControllerObj.transform;
        Light light = sunLightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.9f, 0.7f);
        light.intensity = 1f;
        light.shadows = LightShadows.Soft;
        sun.sunLight = light;

        // سایر منیجرها
        CreateOtherManagers();

        // Debug Helper
        GameObject debugObj = new GameObject("ShadowDebugger");
        ShadowDebugger debugger = debugObj.AddComponent<ShadowDebugger>();
        debugger.showShadowBounds = true;
        debugger.showPlayerShadowStatus = true;
        debugger.logShadowChanges = true;

        ConnectManagerReferences(gm, sun);

        Debug.Log("🎮 منیجرها با Path System ایجاد شدند");
    }

    static void CreateOtherManagers()
    {
        GameObject particleManagerObj = new GameObject("ParticleManager");
        particleManagerObj.AddComponent<ParticleManager>();

        GameObject audioManagerObj = new GameObject("AudioManager");
        AudioManager am = audioManagerObj.AddComponent<AudioManager>();
        am.musicSource = audioManagerObj.AddComponent<AudioSource>();
        am.effectsSource = audioManagerObj.AddComponent<AudioSource>();

        GameObject analyticsObj = new GameObject("AnalyticsTracker");
        analyticsObj.AddComponent<AnalyticsTracker>();

        GameObject progressObj = new GameObject("LevelProgressTracker");
        progressObj.AddComponent<LevelProgressTracker>();

        GameObject comboObj = new GameObject("ComboSystem");
        ComboSystem combo = comboObj.AddComponent<ComboSystem>();
        combo.comboWindow = 7f;
        combo.shadowJumpBonus = 20f;
        combo.mergeBonus = 40f;

        GameObject devObj = new GameObject("DeveloperHelper");
        devObj.AddComponent<DeveloperHelper>();

        GameObject touchObj = new GameObject("TouchEffects");
        touchObj.AddComponent<TouchEffects>();
    }

    static void ConnectManagerReferences(GameManager gm, SunController sun)
    {
        GameObject player = GameObject.Find("WaterDrop");
        Transform pathFinishWaypoint = GameObject.Find("Waypoint_04_Finish")?.transform;
        CameraController cam = FindObjectOfType<CameraController>();

        if (player != null)
        {
            gm.waterDrop = player.GetComponent<WaterDrop>(); // Updated reference
            if (cam != null) cam.target = player.transform;
        }

        gm.sunController = sun;
        gm.levelEndPoint = pathFinishWaypoint; // آخرین waypoint

        Debug.Log("🔗 ارجاعات منیجرها متصل شدند");
    }

    static void CreateCompleteUI()
    {
        Debug.Log("🖼️ شروع ایجاد UI...");

        CreateEventSystem();
        GameObject canvasObj = CreateMainCanvas();
        GameObject waterBarObj = CreateWaterBarUI(canvasObj);
        GameObject timerObj = CreateTimerUI(canvasObj);
        GameObject sunSystemObj = CreateSunUI(canvasObj);
        GameObject winScreenObj = CreateWinScreen(canvasObj);
        GameObject loseScreenObj = CreateLoseScreen(canvasObj);
        GameObject countdownObj = CreateCountdownUI(canvasObj);

        // Path Progress UI (جدید)
        GameObject pathProgressObj = CreatePathProgressUI(canvasObj);

        GameObject uiManagerObj = new GameObject("UI_Manager");
        uiManagerObj.transform.SetParent(canvasObj.transform, false);
        UI_Manager uiManager = uiManagerObj.AddComponent<UI_Manager>();

        ConnectUIReferences(uiManager, canvasObj, waterBarObj, timerObj, sunSystemObj,
                          winScreenObj, loseScreenObj, countdownObj);

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.uiManager = uiManager;

        Debug.Log("✅ UI کامل ایجاد شد");
    }

    static GameObject CreatePathProgressUI(GameObject canvas)
    {
        GameObject progressPanel = new GameObject("PathProgressPanel");
        progressPanel.transform.SetParent(canvas.transform, false);

        UnityEngine.UI.Image panelImage = progressPanel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0, 0, 0, 0.3f);

        RectTransform panelRect = progressPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.02f, 0.85f);
        panelRect.anchorMax = new Vector2(0.35f, 0.98f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Path Progress Text
        GameObject progressTextObj = new GameObject("PathProgressText");
        progressTextObj.transform.SetParent(progressPanel.transform, false);

        UnityEngine.UI.Text progressText = progressTextObj.AddComponent<UnityEngine.UI.Text>();
        progressText.text = "🛤️ مسیر: 0/5";
        progressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        progressText.fontSize = 28;
        progressText.color = Color.white;
        progressText.alignment = TextAnchor.MiddleCenter;

        RectTransform progressTextRect = progressTextObj.GetComponent<RectTransform>();
        progressTextRect.anchorMin = Vector2.zero;
        progressTextRect.anchorMax = Vector2.one;
        progressTextRect.offsetMin = Vector2.zero;
        progressTextRect.offsetMax = Vector2.zero;

        return progressPanel;
    }

    // Helper methods برای متریال و UI (مانند قبل)
    static Material CreateURPMaterial(string name)
    {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null)
        {
            urpShader = Shader.Find("Standard");
        }

        Material mat = new Material(urpShader);
        mat.name = name;
        return mat;
    }

    static Material CreateTransparentURPMaterial(string name, Color color)
    {
        Material mat = CreateURPMaterial(name);

        if (mat.shader.name.Contains("Universal"))
        {
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetFloat("_AlphaCutoff", 0);
            mat.SetFloat("_SrcBlend", 5);
            mat.SetFloat("_DstBlend", 10);
            mat.SetFloat("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        else
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
        }

        mat.color = color;
        return mat;
    }

    static Material CreateShadowMaterial()
    {
        return CreateTransparentURPMaterial("ShadowMaterial", new Color(0.05f, 0.05f, 0.05f, 0.8f));
    }

    static void CreateWaterPickupAt(Vector3 position, int id = 0)
    {
        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pickup.name = $"WaterPickup_{id:00}";
        pickup.transform.position = position;
        pickup.transform.localScale = Vector3.one * 0.3f;
        pickup.tag = "WaterPickup";

        Material pickupMat = CreateTransparentURPMaterial("WaterPickupMaterial", new Color(0.3f, 0.8f, 1f, 0.8f));
        pickup.GetComponent<MeshRenderer>().material = pickupMat;

        SphereCollider collider = pickup.GetComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 1.2f;

        WaterPickup waterScript = pickup.AddComponent<WaterPickup>();
        waterScript.waterAmount = 30f;
        waterScript.bobSpeed = 2.5f;
        waterScript.bobHeight = 0.5f;
    }

    // سایر UI methods (CreateEventSystem, CreateMainCanvas, etc.) مانند قبل
    static void CreateEventSystem()
    {
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    static GameObject CreateMainCanvas()
    {
        GameObject canvasObj = new GameObject("UI Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        return canvasObj;
    }

    // باقی UI methods مانند artifact قبلی...
    static GameObject CreateWaterBarUI(GameObject canvas)
    {
        // کد مشابه artifact قبلی
        return new GameObject("WaterBarPanel");
    }

    static GameObject CreateTimerUI(GameObject canvas)
    {
        return new GameObject("TimerPanel");
    }

    static GameObject CreateSunUI(GameObject canvas)
    {
        return new GameObject("SunSystem");
    }

    static GameObject CreateWinScreen(GameObject canvas)
    {
        return new GameObject("WinScreen");
    }

    static GameObject CreateLoseScreen(GameObject canvas)
    {
        return new GameObject("LoseScreen");
    }

    static GameObject CreateCountdownUI(GameObject canvas)
    {
        return new GameObject("CountdownPanel");
    }

    static void ConnectUIReferences(UI_Manager uiManager, GameObject canvas,
                                  GameObject waterBarObj, GameObject timerObj, GameObject sunSystemObj,
                                  GameObject winScreenObj, GameObject loseScreenObj, GameObject countdownObj)
    {
        // کد اتصال مانند قبل
    }

    // Menu Tools
    [MenuItem("Last Drop/Debug Shadow System")]
    public static void DebugShadowSystem()
    {
        Debug.Log("=== Debug Shadow System ===");

        // بررسی Layer Setup
        string shadowLayerName = LayerMask.LayerToName(8);
        Debug.Log($"Layer 8: {(string.IsNullOrEmpty(shadowLayerName) ? "❌ تنظیم نشده" : $"✅ {shadowLayerName}")}");

        // بررسی Shadow Projectors
        ShadowProjector[] shadows = FindObjectsOfType<ShadowProjector>();
        Debug.Log($"Shadow Projectors: {shadows.Length}");

        foreach (var shadow in shadows)
        {
            if (shadow.shadowMesh != null)
            {
                Debug.Log($"   - {shadow.name}: Layer {shadow.shadowMesh.layer} | Material: {(shadow.shadowMaterial != null ? "✅" : "❌")}");
            }
        }

        // بررسی Player
        DropPathFollower player = FindObjectOfType<DropPathFollower>();
        if (player != null)
        {
            Debug.Log($"Player: ✅ | In Shadow: {player.IsInShadow()} | Current Shadow: {player.GetCurrentShadow()?.name ?? "None"}");
        }
        else
        {
            Debug.Log("Player: ❌ DropPathFollower not found");
        }

        Debug.Log("=== Debug Complete ===");
    }

    [MenuItem("Last Drop/Test Path System")]
    public static void TestPathSystem()
    {
        WaypointPath path = FindObjectOfType<WaypointPath>();
        if (path != null)
        {
            Debug.Log($"✅ Path System: {path.GetWaypointCount()} waypoints");
            for (int i = 0; i < path.GetWaypointCount(); i++)
            {
                Waypoint wp = path.GetWaypoint(i);
                Debug.Log($"   Waypoint {i}: {wp.transform.name} | Must be in shadow: {wp.mustBeInShadow}");
            }
        }
        else
        {
            Debug.LogError("❌ Path System not found!");
        }

        DropPathFollower follower = FindObjectOfType<DropPathFollower>();
        if (follower != null)
        {
            Debug.Log($"✅ Path Follower: Current waypoint {follower.GetCurrentWaypointIndex()} | Moving: {follower.IsMoving()}");
        }
        else
        {
            Debug.LogError("❌ Path Follower not found!");
        }
    }
}
