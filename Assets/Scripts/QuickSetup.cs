
// QuickSetup.cs - اسکریپت راه‌اندازی سریع
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class QuickSetup : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Last Drop/Quick Setup Scene")]
    public static void SetupScene()
    {
        // Clear existing objects
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj == null || obj.Equals(null)) continue; // skip destroyed objects
            if (obj != Camera.main && obj.name != "Directional Light")
            {
                DestroyImmediate(obj);
            }
        }

        // Setup Camera
        SetupCamera();

        // Create Environment
        CreateEnvironment();

        // Create Player
        CreatePlayer();

        // Create Managers
        CreateManagers();

        // Create UI
        CreateUI();

        Debug.Log("Last Drop scene setup complete!");
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
        cam.transform.position = new Vector3(0, 10, -8);
        cam.transform.rotation = Quaternion.Euler(35, 0, 0);
        cam.orthographic = true;
        cam.orthographicSize = 8;
        cam.backgroundColor = new Color(1f, 0.64f, 0.4f); // Orange background
    }

    static void CreateEnvironment()
    {
        // Ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = Vector3.one * 1.5f;

        Material groundMat = new Material(Shader.Find("Standard"));
        groundMat.color = new Color(0.9f, 0.72f, 0.47f); // Sandy color
        ground.GetComponent<MeshRenderer>().material = groundMat;

        // Obstacles with ShadowProjector
        CreateObstacle("Obstacle1", new Vector3(-3, 1, -2));
        CreateObstacle("Obstacle2", new Vector3(1, 1, 0));
        CreateObstacle("Obstacle3", new Vector3(2, 1, 0.5f));

        // End Point
        GameObject endPoint = new GameObject("EndPoint");
        endPoint.transform.position = new Vector3(0, 0.5f, 5);
        endPoint.tag = "Finish";

        // Visual indicator for endpoint
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        indicator.transform.parent = endPoint.transform;
        indicator.transform.localPosition = Vector2.zero;
        indicator.transform.localScale = new Vector3(1, 0.1f, 1);

        Material endMat = new Material(Shader.Find("Standard"));
        endMat.color = Color.green;
        endMat.SetFloat("_Metallic", 0.8f);
        indicator.GetComponent<MeshRenderer>().material = endMat;
    }

    static void CreateObstacle(string name, Vector3 position)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = name;
        obstacle.transform.position = position;

        // Material
        Material obstacleMat = new Material(Shader.Find("Standard"));
        obstacleMat.color = new Color(0.6f, 0.4f, 0.3f); // Brown
        obstacle.GetComponent<MeshRenderer>().material = obstacleMat;

        // Add ShadowProjector
        obstacle.AddComponent<ShadowProjector>();
        ShadowProjector shadowProj = obstacle.GetComponent<ShadowProjector>();

        // Create shadow material
        Material shadowMat = new Material(Shader.Find("Standard"));
        shadowMat.SetFloat("_Mode", 3); // Transparent mode
        shadowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        shadowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        shadowMat.SetInt("_ZWrite", 0);
        shadowMat.DisableKeyword("_ALPHATEST_ON");
        shadowMat.EnableKeyword("_ALPHABLEND_ON");
        shadowMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        shadowMat.renderQueue = 3000;
        shadowMat.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);

        shadowProj.shadowMaterial = shadowMat;
        shadowProj.shadowLength = 5f;
        shadowProj.shadowWidth = 1.5f;
    }

    static void CreatePlayer()
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "WaterDrop";
        player.transform.position = new Vector3(0, 0.5f, -4);
        player.transform.localScale = Vector3.one * 0.3f;
        player.tag = "Player";

        // Material
        Material waterMat = new Material(Shader.Find("Standard"));
        waterMat.color = new Color(0, 0.6f, 1f, 0.8f);
        waterMat.SetFloat("_Metallic", 0.8f);
        waterMat.SetFloat("_Smoothness", 0.9f);
        waterMat.EnableKeyword("_EMISSION");
        waterMat.SetColor("_EmissionColor", new Color(0, 0.3f, 0.6f));
        player.GetComponent<MeshRenderer>().material = waterMat;

        // Physics
        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.mass = 0.1f;
        rb.linearDamping = 5f;

        // Script
        player.AddComponent<WaterDrop>();
    }

    static void CreateManagers()
    {
        // Game Manager
        GameObject gameManager = new GameObject("GameManager");
        GameManager gm = gameManager.AddComponent<GameManager>();

        // Sun Controller
        GameObject sunController = new GameObject("SunController");
        sunController.AddComponent<SunController>();
        SunController sun = sunController.GetComponent<SunController>();

        // Create sun light
        GameObject sunLight = new GameObject("SunLight");
        sunLight.transform.parent = sunController.transform;
        Light light = sunLight.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.8f);
        light.intensity = 1.2f;
        light.shadows = LightShadows.None;

        sun.sunLight = light;

        // Particle Manager
        GameObject particleManager = new GameObject("ParticleManager");
        particleManager.AddComponent<ParticleManager>();

        // Audio Manager
        GameObject audioManager = new GameObject("AudioManager");
        AudioManager am = audioManager.AddComponent<AudioManager>();
        am.musicSource = audioManager.AddComponent<AudioSource>();
        am.effectsSource = audioManager.AddComponent<AudioSource>();

        // Analytics
        GameObject analytics = new GameObject("AnalyticsTracker");
        analytics.AddComponent<AnalyticsTracker>();

        // Level Progress Tracker
        GameObject progressTracker = new GameObject("LevelProgressTracker");
        progressTracker.AddComponent<LevelProgressTracker>();

        // Connect references
        gm.waterDrop = FindObjectOfType<WaterDrop>();
        gm.sunController = sun;
        gm.levelEndPoint = GameObject.Find("EndPoint").transform;
    }

    static void CreateUI()
    {
        // Canvas
        GameObject canvas = new GameObject("UI Canvas");
        Canvas canvasComp = canvas.AddComponent<Canvas>();
        canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
        UnityEngine.UI.CanvasScaler scaler = canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Water Bar
        CreateWaterBar(canvas);

        // Timer
        CreateTimer(canvas);

        // Sun Icon
        CreateSunIcon(canvas);

        // Win/Lose Screens
        CreateGameScreens(canvas);

        // UI Manager
        GameObject uiManager = new GameObject("UI_Manager");
        uiManager.transform.parent = canvas.transform;
        UI_Manager uim = uiManager.AddComponent<UI_Manager>();

        // Connect UI references
        uim.waterBar = canvas.GetComponentInChildren<UnityEngine.UI.Slider>();
        uim.timerText = GameObject.Find("TimerText").GetComponent<UnityEngine.UI.Text>();
        uim.sunIcon = GameObject.Find("SunIcon").GetComponent<UnityEngine.UI.Image>();
        uim.winScreen = GameObject.Find("WinScreen");
        uim.loseScreen = GameObject.Find("LoseScreen");
        // Find RestartButton even if inactive
        UnityEngine.UI.Button[] buttons = canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.gameObject.name == "RestartButton")
            {
                uim.restartButton = btn;
                break;
            }
        }

        // Connect to GameManager
        FindObjectOfType<GameManager>().uiManager = uim;
    }

    static void CreateWaterBar(GameObject canvas)
    {
        GameObject waterBar = new GameObject("WaterBar", typeof(RectTransform));
        waterBar.transform.parent = canvas.transform;

        UnityEngine.UI.Slider slider = waterBar.AddComponent<UnityEngine.UI.Slider>();
        RectTransform sliderRect = waterBar.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.05f, 0.05f);
        sliderRect.anchorMax = new Vector2(0.4f, 0.1f);
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        // Background
        GameObject background = new GameObject("Background", typeof(RectTransform));
        background.transform.parent = waterBar.transform;
        UnityEngine.UI.Image bgImage = background.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0, 0, 0, 0.3f);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.parent = waterBar.transform;
        RectTransform fillRect = fillArea.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Fill
        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.parent = fillArea.transform;
        UnityEngine.UI.Image fillImage = fill.AddComponent<UnityEngine.UI.Image>();
        fillImage.color = new Color(0, 0.8f, 1f, 0.8f);
        RectTransform fillImageRect = fill.GetComponent<RectTransform>();
        fillImageRect.anchorMin = Vector2.zero;
        fillImageRect.anchorMax = Vector2.one;
        fillImageRect.offsetMin = Vector2.zero;
        fillImageRect.offsetMax = Vector2.zero;

        slider.fillRect = fillImageRect;
        slider.value = 1f;
    }

    static void CreateTimer(GameObject canvas)
    {
        GameObject timer = new GameObject("TimerText");
        timer.transform.parent = canvas.transform;

        UnityEngine.UI.Text text = timer.AddComponent<UnityEngine.UI.Text>();
        text.text = "40";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 48;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        // Add shadow
        UnityEngine.UI.Shadow shadow = timer.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(2, -2);

        RectTransform timerRect = timer.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.45f, 0.9f);
        timerRect.anchorMax = new Vector2(0.55f, 0.95f);
        timerRect.offsetMin = Vector2.zero;
        timerRect.offsetMax = Vector2.zero;
    }

    static void CreateSunIcon(GameObject canvas)
    {
        GameObject sunIcon = new GameObject("SunIcon");
        sunIcon.transform.parent = canvas.transform;

        UnityEngine.UI.Image sunImage = sunIcon.AddComponent<UnityEngine.UI.Image>();
        sunImage.color = new Color(1f, 0.9f, 0.3f);

        RectTransform sunRect = sunIcon.GetComponent<RectTransform>();
        sunRect.anchorMin = new Vector2(0.5f, 0.85f);
        sunRect.anchorMax = new Vector2(0.5f, 0.85f);
        sunRect.sizeDelta = new Vector2(50, 50);
    }

    static void CreateGameScreens(GameObject canvas)
    {
        // Win Screen
        CreateGameScreen(canvas, "WinScreen", "YOU WIN!", Color.green);

        // Lose Screen  
        CreateGameScreen(canvas, "LoseScreen", "TRY AGAIN", Color.red);
    }

    static void CreateGameScreen(GameObject canvas, string screenName, string message, Color color)
    {
        GameObject screen = new GameObject(screenName);
        screen.transform.parent = canvas.transform;

        UnityEngine.UI.Image screenImage = screen.AddComponent<UnityEngine.UI.Image>();
        screenImage.color = new Color(0, 0, 0, 0.7f);

        RectTransform screenRect = screen.GetComponent<RectTransform>();
        screenRect.anchorMin = Vector2.zero;
        screenRect.anchorMax = Vector2.one;
        screenRect.offsetMin = Vector2.zero;
        screenRect.offsetMax = Vector2.zero;

        // Message Text
        GameObject messageText = new GameObject("MessageText");
        messageText.transform.parent = screen.transform;

        UnityEngine.UI.Text text = messageText.AddComponent<UnityEngine.UI.Text>();
        text.text = message;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 72;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = messageText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.2f, 0.6f);
        textRect.anchorMax = new Vector2(0.8f, 0.7f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Restart Button
        GameObject restartButton = new GameObject("RestartButton");
        restartButton.transform.parent = screen.transform;

        UnityEngine.UI.Image buttonImage = restartButton.AddComponent<UnityEngine.UI.Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        UnityEngine.UI.Button button = restartButton.AddComponent<UnityEngine.UI.Button>();

        RectTransform buttonRect = restartButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.4f, 0.4f);
        buttonRect.anchorMax = new Vector2(0.6f, 0.5f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        // Button Text
        GameObject buttonText = new GameObject("ButtonText");
        buttonText.transform.parent = restartButton.transform;

        UnityEngine.UI.Text btnText = buttonText.AddComponent<UnityEngine.UI.Text>();
        btnText.text = "RESTART";
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 32;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;

        RectTransform btnTextRect = buttonText.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        screen.SetActive(false); // Hidden by default
    }

    [MenuItem("Last Drop/Create Water Pickup")]
    public static void CreateWaterPickup()
    {
        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pickup.name = "WaterPickup";
        pickup.transform.localScale = Vector3.one * 0.2f;
        pickup.tag = "WaterPickup";

        // Material
        Material pickupMat = new Material(Shader.Find("Standard"));
        pickupMat.color = new Color(0.3f, 0.8f, 1f, 0.8f);
        pickupMat.EnableKeyword("_EMISSION");
        pickupMat.SetColor("_EmissionColor", new Color(0, 0.5f, 1f));
        pickup.GetComponent<MeshRenderer>().material = pickupMat;

        // Trigger collider
        SphereCollider collider = pickup.GetComponent<SphereCollider>();
        collider.isTrigger = true;

        // Script
        pickup.AddComponent<WaterPickup>();

        // Position at scene view camera
        if (SceneView.lastActiveSceneView != null)
        {
            pickup.transform.position = SceneView.lastActiveSceneView.camera.transform.position;
        }

        Selection.activeGameObject = pickup;
        Debug.Log("Water Pickup created! Position it in your level.");
    }

    [MenuItem("Last Drop/Test All Systems")]
    public static void TestAllSystems()
    {
        Debug.Log("=== TESTING LAST DROP SYSTEMS ===");

        // Test GameManager
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            Debug.Log("✅ GameManager found");
            if (gm.waterDrop != null) Debug.Log("✅ WaterDrop reference OK");
            if (gm.sunController != null) Debug.Log("✅ SunController reference OK");
            if (gm.uiManager != null) Debug.Log("✅ UI_Manager reference OK");
        }
        else Debug.LogError("❌ GameManager not found!");

        // Test ShadowProjectors
        ShadowProjector[] shadows = FindObjectsOfType<ShadowProjector>();
        Debug.Log($"✅ Found {shadows.Length} Shadow Projectors");

        // Test UI
        UnityEngine.UI.Slider waterBar = FindObjectOfType<UnityEngine.UI.Slider>();
        if (waterBar != null) Debug.Log("✅ Water Bar UI found");

        Debug.Log("=== TEST COMPLETE ===");
        Debug.Log("Ready to play! Press Play button to test your game.");
    }
#endif
}
