// QuickSetup.cs - اسکریپت راه‌اندازی کامل Last Drop
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
        Debug.Log("🚀 شروع راه‌اندازی Last Drop...");

        // پاک کردن صحنه
        ClearScene();

        // راه‌اندازی دوربین
        SetupCamera();

        // ایجاد محیط
        CreateEnvironment();

        // ایجاد بازیکن
        CreatePlayer();

        // ایجاد منیجرها
        CreateManagers();

        // ایجاد UI
        CreateUI();

        Debug.Log("🎉 Last Drop راه‌اندازی کامل شد!");
        Debug.Log("✨ ویژگی‌ها: Combo System, Dynamic Obstacles, Enhanced UI");
        Debug.Log("🎮 برای تست دکمه Play را بزنید!");
    }

    static void ClearScene()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj == null || obj.Equals(null)) continue;
            if (obj != Camera.main && obj.name != "Directional Light")
            {
                DestroyImmediate(obj);
            }
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

        cam.transform.position = new Vector3(0, 12, -10);
        cam.transform.rotation = Quaternion.Euler(45, 0, 0);
        cam.orthographic = true;
        cam.orthographicSize = 10;
        cam.backgroundColor = new Color(0.9f, 0.6f, 0.3f); // آسمان نارنجی

        // اضافه کردن Camera Controller
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

    static void CreateEnvironment()
    {
        // زمین
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = Vector3.one * 3f;
        ground.transform.position = Vector3.zero;

        Material groundMat = new Material(Shader.Find("Standard"));
        groundMat.color = new Color(0.85f, 0.65f, 0.4f); // رنگ شنی
        groundMat.SetFloat("_Glossiness", 0.1f);
        ground.GetComponent<MeshRenderer>().material = groundMat;

        // موانع ثابت
        CreateStaticObstacle("Obstacle_1", new Vector3(-4, 1, -1));
        CreateStaticObstacle("Obstacle_2", new Vector3(2, 1, 1));
        CreateStaticObstacle("Obstacle_3", new Vector3(-1, 1, 3));

        // موانع پویا
        CreateMovingObstacle("MovingObstacle_1", new Vector3(3, 1, -2));
        CreatePulsingObstacle("PulsingObstacle_1", new Vector3(-3, 1, 4));

        // نقطه پایان
        CreateEndPoint();

        // آب‌های قابل جمع‌آوری
        CreateWaterPickups();

        Debug.Log("🌍 محیط ایجاد شد");
    }

    static void CreateStaticObstacle(string name, Vector3 position)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = name;
        obstacle.transform.position = position;
        obstacle.transform.localScale = new Vector3(1.2f, 2.5f, 1.2f);

        // متریال
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.5f, 0.3f, 0.2f); // قهوه‌ای
        mat.SetFloat("_Glossiness", 0.3f);
        obstacle.GetComponent<MeshRenderer>().material = mat;

        // ShadowProjector
        ShadowProjector shadowProj = obstacle.AddComponent<ShadowProjector>();
        shadowProj.shadowMaterial = CreateShadowMaterial();
        shadowProj.shadowLength = 7f;
        shadowProj.shadowWidth = 2.5f;

        obstacle.tag = "ShadowCaster";
    }

    static void CreateMovingObstacle(string name, Vector3 position)
    {
        // ایجاد مانع پایه
        CreateStaticObstacle(name, position);
        GameObject obstacle = GameObject.Find(name);

        // اضافه کردن حرکت
        DynamicObstacle dynamic = obstacle.AddComponent<DynamicObstacle>();
        dynamic.isMoving = true;
        dynamic.moveSpeed = 2f;
        dynamic.waitTime = 1.5f;

        // تعریف مسیر مربعی
        Vector3[] waypoints = new Vector3[4];
        waypoints[0] = position;
        waypoints[1] = position + new Vector3(3, 0, 0);
        waypoints[2] = position + new Vector3(3, 0, 3);
        waypoints[3] = position + new Vector3(0, 0, 3);
        dynamic.waypoints = waypoints;

        // رنگ متفاوت برای تشخیص
        MeshRenderer renderer = obstacle.GetComponent<MeshRenderer>();
        Material mat = renderer.material;
        mat.color = new Color(0.7f, 0.3f, 0.7f); // بنفش
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.7f, 0.3f, 0.7f) * 0.2f);
    }

    static void CreatePulsingObstacle(string name, Vector3 position)
    {
        CreateStaticObstacle(name, position);
        GameObject obstacle = GameObject.Find(name);

        DynamicObstacle dynamic = obstacle.AddComponent<DynamicObstacle>();
        dynamic.isPulsing = true;
        dynamic.pulseSpeed = 2f;
        dynamic.pulseMin = 0.6f;
        dynamic.pulseMax = 1.4f;
        dynamic.givesWaterWhenHidden = true;
        dynamic.waterReward = 25f;

        // رنگ آبی برای پالسی
        MeshRenderer renderer = obstacle.GetComponent<MeshRenderer>();
        Material mat = renderer.material;
        mat.color = new Color(0.2f, 0.5f, 0.8f); // آبی
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.2f, 0.5f, 0.8f) * 0.3f);
    }

    static void CreateEndPoint()
    {
        GameObject endPoint = new GameObject("EndPoint");
        endPoint.transform.position = new Vector3(0, 0.5f, 8);
        endPoint.tag = "Finish";

        // نشانگر بصری
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        indicator.transform.parent = endPoint.transform;
        indicator.transform.localPosition = Vector3.zero;
        indicator.transform.localScale = new Vector3(1.5f, 0.1f, 1.5f);

        Material endMat = new Material(Shader.Find("Standard"));
        endMat.color = Color.green;
        endMat.SetFloat("_Metallic", 0.9f);
        endMat.EnableKeyword("_EMISSION");
        endMat.SetColor("_EmissionColor", Color.green * 0.4f);
        indicator.GetComponent<MeshRenderer>().material = endMat;

        // افکت چرخش
        indicator.AddComponent<RotationEffect>();
    }

    static void CreateWaterPickups()
    {
        Vector3[] positions = {
            new Vector3(-2, 0.6f, 0),
            new Vector3(4, 0.6f, 3),
            new Vector3(-1, 0.6f, 5),
            new Vector3(2, 0.6f, -3),
            new Vector3(-4, 0.6f, 2)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            CreateWaterPickupAt(positions[i], i + 1);
        }
    }

    static void CreateWaterPickupAt(Vector3 position, int id = 0)
    {
        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pickup.name = $"WaterPickup_{id:00}";
        pickup.transform.position = position;
        pickup.transform.localScale = Vector3.one * 0.3f;
        pickup.tag = "WaterPickup";

        // متریال شفاف آبی
        Material pickupMat = new Material(Shader.Find("Standard"));
        pickupMat.SetFloat("_Mode", 3); // Transparent
        pickupMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        pickupMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        pickupMat.SetInt("_ZWrite", 0);
        pickupMat.EnableKeyword("_ALPHABLEND_ON");
        pickupMat.renderQueue = 3000;
        pickupMat.color = new Color(0.3f, 0.8f, 1f, 0.8f);
        pickupMat.SetFloat("_Metallic", 0.8f);
        pickupMat.SetFloat("_Smoothness", 0.9f);
        pickupMat.EnableKeyword("_EMISSION");
        pickupMat.SetColor("_EmissionColor", new Color(0.2f, 0.6f, 1f));
        pickup.GetComponent<MeshRenderer>().material = pickupMat;

        // Collider
        SphereCollider collider = pickup.GetComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 1.2f; // بزرگ‌تر برای راحتی جمع‌آوری

        // Script
        WaterPickup waterScript = pickup.AddComponent<WaterPickup>();
        waterScript.waterAmount = 30f;
        waterScript.bobSpeed = 2.5f;
        waterScript.bobHeight = 0.5f;
    }

    static Material CreateShadowMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mat.color = new Color(0.05f, 0.05f, 0.05f, 0.8f); // سایه تیره
        return mat;
    }

    static void CreatePlayer()
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "WaterDrop";
        player.transform.position = new Vector3(0, 0.6f, -6);
        player.transform.localScale = Vector3.one * 0.5f;
        player.tag = "Player";

        // متریال آب
        Material waterMat = new Material(Shader.Find("Standard"));
        waterMat.SetFloat("_Mode", 3); // Transparent
        waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        waterMat.SetInt("_ZWrite", 0);
        waterMat.EnableKeyword("_ALPHABLEND_ON");
        waterMat.renderQueue = 3000;
        waterMat.color = new Color(0.1f, 0.6f, 1f, 0.9f);
        waterMat.SetFloat("_Metallic", 0.9f);
        waterMat.SetFloat("_Smoothness", 0.95f);
        waterMat.EnableKeyword("_EMISSION");
        waterMat.SetColor("_EmissionColor", new Color(0.05f, 0.3f, 0.6f));
        player.GetComponent<MeshRenderer>().material = waterMat;

        // فیزیک
        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.mass = 0.2f;
        rb.linearDamping = 10f;
        rb.angularDamping = 15f;

        // اسکریپت
        WaterDrop waterDropScript = player.AddComponent<WaterDrop>();
        waterDropScript.moveSpeed = 5f;
        waterDropScript.rotationSpeed = 10f;

        Debug.Log("💧 بازیکن ایجاد شد");
    }

    static void CreateManagers()
    {
        // Game Manager
        GameObject gameManagerObj = new GameObject("GameManager");
        GameManager gm = gameManagerObj.AddComponent<GameManager>();
        gm.waterDecayRate = 1f; // کم شدن آب در سایه
        gm.sunlightDecayRate = 12f; // کم شدن آب در نور خورشید
        gm.levelTime = 60f; // 1 دقیقه
        gm.startDelay = 3f; // تاخیر شروع
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
        light.intensity = 2f;
        light.shadows = LightShadows.Soft;
        sun.sunLight = light;

        // Particle Manager
        GameObject particleManagerObj = new GameObject("ParticleManager");
        particleManagerObj.AddComponent<ParticleManager>();

        // Audio Manager
        GameObject audioManagerObj = new GameObject("AudioManager");
        AudioManager am = audioManagerObj.AddComponent<AudioManager>();
        am.musicSource = audioManagerObj.AddComponent<AudioSource>();
        am.effectsSource = audioManagerObj.AddComponent<AudioSource>();

        // Analytics
        GameObject analyticsObj = new GameObject("AnalyticsTracker");
        analyticsObj.AddComponent<AnalyticsTracker>();

        // Level Progress Tracker
        GameObject progressObj = new GameObject("LevelProgressTracker");
        progressObj.AddComponent<LevelProgressTracker>();

        // Combo System (جدید!)
        GameObject comboObj = new GameObject("ComboSystem");
        ComboSystem combo = comboObj.AddComponent<ComboSystem>();
        combo.comboWindow = 7f;
        combo.shadowJumpBonus = 20f;
        combo.mergeBonus = 40f;

        // Developer Helper
        GameObject devObj = new GameObject("DeveloperHelper");
        devObj.AddComponent<DeveloperHelper>();

        // Touch Effects
        GameObject touchObj = new GameObject("TouchEffects");
        touchObj.AddComponent<TouchEffects>();

        // اتصال ارجاعات
        ConnectManagerReferences(gm, sun);

        Debug.Log("🎮 منیجرها ایجاد شدند");
    }

    static void ConnectManagerReferences(GameManager gm, SunController sun)
    {
        WaterDrop waterDrop = FindObjectOfType<WaterDrop>();
        Transform endPoint = GameObject.Find("EndPoint")?.transform;
        CameraController cam = FindObjectOfType<CameraController>();

        if (waterDrop != null)
        {
            gm.waterDrop = waterDrop;
            if (cam != null) cam.target = waterDrop.transform;
        }

        gm.sunController = sun;
        gm.levelEndPoint = endPoint;
    }

    static void CreateUI()
    {
        // Canvas اصلی
        GameObject canvasObj = new GameObject("UI Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // ایجاد عناصر UI
        CreateWaterBarUI(canvasObj);
        CreateTimerUI(canvasObj);
        CreateSunUI(canvasObj);
        CreateGameOverScreens(canvasObj);
        CreateCountdownUI(canvasObj);

        // UI Manager
        GameObject uiManagerObj = new GameObject("UI_Manager");
        uiManagerObj.transform.SetParent(canvasObj.transform, false);
        UI_Manager uim = uiManagerObj.AddComponent<UI_Manager>();

        // اتصال ارجاعات UI
        ConnectUIReferences(uim, canvasObj);

        // اتصال به Game Manager
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.uiManager = uim;

        Debug.Log("🖼️ رابط کاربری ایجاد شد");
    }

    static void CreateWaterBarUI(GameObject canvas)
    {
        GameObject waterBarObj = new GameObject("WaterBar", typeof(RectTransform));
        waterBarObj.transform.SetParent(canvas.transform, false);

        UnityEngine.UI.Slider slider = waterBarObj.AddComponent<UnityEngine.UI.Slider>();
        RectTransform rect = waterBarObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f, 0.05f);
        rect.anchorMax = new Vector2(0.5f, 0.15f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // پس‌زمینه
        GameObject bgObj = new GameObject("Background", typeof(RectTransform));
        bgObj.transform.SetParent(waterBarObj.transform, false);
        UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0, 0, 0, 0.6f);
        SetFullRect(bgObj.GetComponent<RectTransform>());

        // Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaObj.transform.SetParent(waterBarObj.transform, false);
        SetFullRect(fillAreaObj.GetComponent<RectTransform>());

        // Fill
        GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        UnityEngine.UI.Image fillImage = fillObj.AddComponent<UnityEngine.UI.Image>();
        fillImage.color = new Color(0.1f, 0.7f, 1f, 0.9f);
        SetFullRect(fillObj.GetComponent<RectTransform>());

        slider.fillRect = fillObj.GetComponent<RectTransform>();
        slider.value = 1f;

        // برچسب
        CreateUILabel(waterBarObj, "💧 آب", new Vector2(0, 25));
    }

    static void CreateTimerUI(GameObject canvas)
    {
        GameObject timerObj = new GameObject("TimerText");
        timerObj.transform.SetParent(canvas.transform, false);

        UnityEngine.UI.Text text = timerObj.AddComponent<UnityEngine.UI.Text>();
        text.text = "60";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 80;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;

        // حاشیه
        UnityEngine.UI.Outline outline = timerObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(4, -4);

        RectTransform rect = timerObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.35f, 0.8f);
        rect.anchorMax = new Vector2(0.65f, 0.95f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // آیکون تایمر
        CreateUILabel(timerObj, "⏰", new Vector2(-80, 0), 50, Color.yellow);
    }

    static void CreateSunUI(GameObject canvas)
    {
        GameObject sunSystemObj = new GameObject("SunSystem");
        sunSystemObj.transform.SetParent(canvas.transform, false);

        RectTransform sunRect = sunSystemObj.GetComponent<RectTransform>();
        sunRect.anchorMin = new Vector2(0.55f, 0.8f);
        sunRect.anchorMax = new Vector2(0.95f, 0.95f);
        sunRect.offsetMin = Vector2.zero;
        sunRect.offsetMax = Vector2.zero;

        // مسیر خورشید
        GameObject pathObj = new GameObject("SunPath");
        pathObj.transform.SetParent(sunSystemObj.transform, false);
        UnityEngine.UI.Image pathImage = pathObj.AddComponent<UnityEngine.UI.Image>();
        pathImage.color = new Color(1f, 1f, 0.3f, 0.4f);

        RectTransform pathRect = pathObj.GetComponent<RectTransform>();
        pathRect.anchorMin = new Vector2(0, 0.3f);
        pathRect.anchorMax = new Vector2(1, 0.7f);
        pathRect.offsetMin = Vector2.zero;
        pathRect.offsetMax = Vector2.zero;

        // آیکون خورشید
        GameObject sunIconObj = new GameObject("SunIcon");
        sunIconObj.transform.SetParent(sunSystemObj.transform, false);

        UnityEngine.UI.Text sunText = sunIconObj.AddComponent<UnityEngine.UI.Text>();
        sunText.text = "☀️";
        sunText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sunText.fontSize = 60;
        sunText.alignment = TextAnchor.MiddleCenter;

        RectTransform iconRect = sunIconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(80, 80);
    }

    static void CreateGameOverScreens(GameObject canvas)
    {
        CreateGameOverScreen(canvas, "WinScreen", "🎉 برنده شدی! 🎉", Color.green);
        CreateGameOverScreen(canvas, "LoseScreen", "😞 دوباره تلاش کن 😞", Color.red);
    }

    static void CreateGameOverScreen(GameObject canvas, string screenName, string message, Color color)
    {
        GameObject screenObj = new GameObject(screenName);
        screenObj.transform.SetParent(canvas.transform, false);

        UnityEngine.UI.Image screenImage = screenObj.AddComponent<UnityEngine.UI.Image>();
        screenImage.color = new Color(0, 0, 0, 0.85f);

        SetFullRect(screenObj.GetComponent<RectTransform>());

        // متن اصلی
        GameObject textObj = new GameObject("MessageText");
        textObj.transform.SetParent(screenObj.transform, false);

        UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = message;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 100;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;

        UnityEngine.UI.Outline outline = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(5, -5);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.5f);
        textRect.anchorMax = new Vector2(0.9f, 0.8f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // دکمه ریستارت
        CreateRestartButton(screenObj);

        screenObj.SetActive(false);
    }

    static void CreateRestartButton(GameObject parent)
    {
        GameObject buttonObj = new GameObject("RestartButton");
        buttonObj.transform.SetParent(parent.transform, false);

        UnityEngine.UI.Image buttonImage = buttonObj.AddComponent<UnityEngine.UI.Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();

        // رنگ‌های دکمه
        UnityEngine.UI.ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        button.colors = colors;

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.3f, 0.2f);
        buttonRect.anchorMax = new Vector2(0.7f, 0.4f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        // متن دکمه
        GameObject buttonTextObj = new GameObject("ButtonText");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);

        UnityEngine.UI.Text btnText = buttonTextObj.AddComponent<UnityEngine.UI.Text>();
        btnText.text = "🔄 شروع مجدد";
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 48;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.fontStyle = FontStyle.Bold;

        SetFullRect(buttonTextObj.GetComponent<RectTransform>());
    }

    static void CreateCountdownUI(GameObject canvas)
    {
        GameObject countdownObj = new GameObject("CountdownPanel");
        countdownObj.transform.SetParent(canvas.transform, false);

        UnityEngine.UI.Image panelImage = countdownObj.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);

        SetFullRect(countdownObj.GetComponent<RectTransform>());

        // متن شمارش معکوس
        GameObject countdownTextObj = new GameObject("CountdownText");
        countdownTextObj.transform.SetParent(countdownObj.transform, false);

        UnityEngine.UI.Text countdownText = countdownTextObj.AddComponent<UnityEngine.UI.Text>();
        countdownText.text = "3";
        countdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countdownText.fontSize = 200;
        countdownText.color = Color.white;
        countdownText.alignment = TextAnchor.MiddleCenter;
        countdownText.fontStyle = FontStyle.Bold;

        UnityEngine.UI.Outline outline = countdownTextObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(8, -8);

        SetFullRect(countdownTextObj.GetComponent<RectTransform>());

        countdownObj.SetActive(false);
    }

    static void CreateUILabel(GameObject parent, string text, Vector2 offset, int fontSize = 32, Color? color = null)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(parent.transform, false);

        UnityEngine.UI.Text labelText = labelObj.AddComponent<UnityEngine.UI.Text>();
        labelText.text = text;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = fontSize;
        labelText.color = color ?? Color.white;
        labelText.alignment = TextAnchor.MiddleCenter;

        UnityEngine.UI.Shadow shadow = labelObj.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(2, -2);

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchoredPosition = offset;
        labelRect.sizeDelta = new Vector2(fontSize * 3, fontSize);
    }

    static void ConnectUIReferences(UI_Manager uim, GameObject canvas)
    {
        // اتصال اجزای UI به UI Manager
        uim.waterBar = canvas.GetComponentInChildren<UnityEngine.UI.Slider>();

        Transform timerTransform = FindChildRecursive(canvas.transform, "TimerText");
        if (timerTransform != null)
            uim.timerText = timerTransform.GetComponent<UnityEngine.UI.Text>();

        Transform sunIconTransform = FindChildRecursive(canvas.transform, "SunIcon");
        if (sunIconTransform != null)
        {
            uim.sunIcon = sunIconTransform.GetComponent<UnityEngine.UI.Image>();
            uim.sunPath = sunIconTransform.parent.GetComponent<RectTransform>();
        }

        Transform winTransform = FindChildRecursive(canvas.transform, "WinScreen");
        if (winTransform != null)
            uim.winScreen = winTransform.gameObject;

        Transform loseTransform = FindChildRecursive(canvas.transform, "LoseScreen");
        if (loseTransform != null)
            uim.loseScreen = loseTransform.gameObject;

        Transform countdownTransform = FindChildRecursive(canvas.transform, "CountdownPanel");
        if (countdownTransform != null)
        {
            uim.countdownPanel = countdownTransform.gameObject;
            Transform countdownTextTransform = FindChildRecursive(countdownTransform, "CountdownText");
            if (countdownTextTransform != null)
                uim.countdownText = countdownTextTransform.GetComponent<UnityEngine.UI.Text>();
        }

        // پیدا کردن دکمه ریستارت
        UnityEngine.UI.Button[] buttons = canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.name == "RestartButton")
            {
                uim.restartButton = btn;
                break;
            }
        }
    }

    static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    static void SetFullRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // کلاس کمکی برای چرخش نقطه پایان
    public class RotationEffect : MonoBehaviour
    {
        void Update()
        {
            transform.Rotate(0, 60f * Time.deltaTime, 0);
        }
    }

    // ==== MENU TOOLS ====

    [MenuItem("Last Drop/Create Water Pickup")]
    public static void CreateWaterPickup()
    {
        Vector3 position = Vector3.zero;
        if (SceneView.lastActiveSceneView != null)
        {
            position = SceneView.lastActiveSceneView.camera.transform.position;
            position.y = 0.6f;
        }

        int id = Random.Range(100, 999);
        CreateWaterPickupAt(position, id);

        GameObject pickup = GameObject.Find($"WaterPickup_{id:00}");
        if (pickup != null)
        {
            Selection.activeGameObject = pickup;
        }

        Debug.Log("💧 Water Pickup ایجاد شد! آن را در سطح قرار دهید.");
    }

    [MenuItem("Last Drop/Create Moving Obstacle")]
    public static void CreateMovingObstacle()
    {
        Vector3 position = Vector3.zero;
        if (SceneView.lastActiveSceneView != null)
        {
            position = SceneView.lastActiveSceneView.camera.transform.position;
            position.y = 1f;
        }

        string name = "MovingObstacle_" + Random.Range(100, 999);
        CreateMovingObstacle(name, position);

        GameObject obstacle = GameObject.Find(name);
        if (obstacle != null)
        {
            Selection.activeGameObject = obstacle;
        }

        Debug.Log("⚡ Moving Obstacle ایجاد شد! waypoint ها را در inspector تنظیم کنید.");
    }

    [MenuItem("Last Drop/Create Pulsing Obstacle")]
    public static void CreatePulsingObstacle()
    {
        Vector3 position = Vector3.zero;
        if (SceneView.lastActiveSceneView != null)
        {
            position = SceneView.lastActiveSceneView.camera.transform.position;
            position.y = 1f;
        }

        string name = "PulsingObstacle_" + Random.Range(100, 999);
        CreatePulsingObstacle(name, position);

        GameObject obstacle = GameObject.Find(name);
        if (obstacle != null)
        {
            Selection.activeGameObject = obstacle;
        }

        Debug.Log("💓 Pulsing Obstacle ایجاد شد! در سایه‌اش آب جایزه می‌دهد.");
    }

    [MenuItem("Last Drop/Test All Systems")]
    public static void TestAllSystems()
    {
        Debug.Log("=== تست سیستم‌های Last Drop ===");

        // تست GameManager
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            Debug.Log("✅ GameManager یافت شد");
            Debug.Log($"   - WaterDrop: {(gm.waterDrop != null ? "✅" : "❌")}");
            Debug.Log($"   - SunController: {(gm.sunController != null ? "✅" : "❌")}");
            Debug.Log($"   - UI Manager: {(gm.uiManager != null ? "✅" : "❌")}");
            Debug.Log($"   - Start Delay: {gm.startDelay}s");
        }
        else Debug.LogError("❌ GameManager یافت نشد!");

        // تست Combo System
        ComboSystem combo = FindObjectOfType<ComboSystem>();
        if (combo != null)
        {
            Debug.Log("✅ ComboSystem یافت شد");
            Debug.Log($"   - Combo Window: {combo.comboWindow}s");
            Debug.Log($"   - Shadow Jump Bonus: {combo.shadowJumpBonus}");
            Debug.Log($"   - Merge Bonus: {combo.mergeBonus}");
        }
        else Debug.LogWarning("⚠️ ComboSystem یافت نشد - در play mode ایجاد می‌شود");

        // تست موانع پویا
        DynamicObstacle[] dynamicObstacles = FindObjectsOfType<DynamicObstacle>();
        Debug.Log($"✅ {dynamicObstacles.Length} مانع پویا یافت شد");
        foreach (var obs in dynamicObstacles)
        {
            if (obs.isMoving)
                Debug.Log($"   🚚 {obs.name} - متحرک با {obs.waypoints?.Length ?? 0} waypoint");
            if (obs.isPulsing)
                Debug.Log($"   💓 {obs.name} - پالسی با جایزه آب");
        }

        // تست ShadowProjectors
        ShadowProjector[] shadows = FindObjectsOfType<ShadowProjector>();
        Debug.Log($"✅ {shadows.Length} Shadow Projector یافت شد");
        int shadowsWithMaterial = 0;
        foreach (var shadow in shadows)
        {
            if (shadow.shadowMaterial != null)
                shadowsWithMaterial++;
        }
        Debug.Log($"   - {shadowsWithMaterial}/{shadows.Length} سایه دارای material");

        // تست UI
        UI_Manager uiManager = FindObjectOfType<UI_Manager>();
        if (uiManager != null)
        {
            Debug.Log("✅ UI_Manager یافت شد");
            Debug.Log($"   - Water Bar: {(uiManager.waterBar != null ? "✅" : "❌")}");
            Debug.Log($"   - Timer: {(uiManager.timerText != null ? "✅" : "❌")}");
            Debug.Log($"   - Sun Icon: {(uiManager.sunIcon != null ? "✅" : "❌")}");
            Debug.Log($"   - Countdown: {(uiManager.countdownPanel != null ? "✅" : "❌")}");
        }
        else Debug.LogError("❌ UI_Manager یافت نشد!");

        // تست Water Pickups
        WaterPickup[] pickups = FindObjectsOfType<WaterPickup>();
        Debug.Log($"✅ {pickups.Length} Water Pickup یافت شد");

        // تست Camera Controller
        CameraController cam = FindObjectOfType<CameraController>();
        if (cam != null)
        {
            Debug.Log("✅ CameraController یافت شد");
            Debug.Log($"   - Follow Target: {cam.followTarget}");
            Debug.Log($"   - Target Set: {(cam.target != null ? "✅" : "❌")}");
        }

        // آمار کارایی
        Debug.Log("=== آمار کارایی ===");
        Debug.Log($"🎯 کل GameObjects: {FindObjectsOfType<GameObject>().Length}");
        Debug.Log($"🎯 کل Renderers: {FindObjectsOfType<Renderer>().Length}");
        Debug.Log($"🎯 کل Colliders: {FindObjectsOfType<Collider>().Length}");

        // ویژگی‌های جدید
        Debug.Log("=== ویژگی‌های جدید ===");
        Debug.Log("🔥 Combo System: آماده برای پرش و ادغام سایه");
        Debug.Log("⚡ Dynamic Obstacles: موانع متحرک و پالسی فعال");
        Debug.Log("⏰ Game Start Timer: شمارش معکوس 3 ثانیه‌ای");
        Debug.Log("💧 Enhanced Water System: کم شدن تدریجی آب در سایه");
        Debug.Log("🎨 Improved UI: بصری‌سازی و بازخورد بهتر");

        Debug.Log("=== تست کامل شد ===");
        Debug.Log("🚀 آماده بازی! دکمه Play را بزنید");
        Debug.Log("💡 خورشید را بکشید تا سایه‌ها حرکت کنند و کومبو بگیرید!");
    }

    [MenuItem("Last Drop/Quick Debug Info")]
    public static void ShowDebugInfo()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ برای Debug Info باید بازی در حال اجرا باشد");
            return;
        }

        Debug.Log("=== اطلاعات Debug سریع ===");

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            Debug.Log($"🎮 Game Active: {gm.IsGameActive()}");
            Debug.Log($"🎮 Game Started: {gm.IsGameStarted()}");
            Debug.Log($"💧 Water: {gm.GetWaterPercentage() * 100f:F0}%");
            Debug.Log($"⏰ Time: {gm.GetTimeRemaining():F1}s");
        }

        WaterDrop player = FindObjectOfType<WaterDrop>();
        if (player != null)
        {
            Debug.Log($"🧊 Player In Shadow: {player.IsInShadow()}");
            ShadowProjector shadow = player.GetCurrentShadow();
            Debug.Log($"🧊 Current Shadow: {(shadow != null ? shadow.name : "هیچ")}");
            Debug.Log($"📍 Player Position: {player.transform.position}");
        }

        ComboSystem combo = FindObjectOfType<ComboSystem>();
        if (combo != null)
        {
            Debug.Log($"🔥 Current Combo: x{combo.GetCurrentCombo()}");
            Debug.Log($"🔥 Combo Multiplier: {combo.GetComboMultiplier():F1}x");
        }

        SunController sun = FindObjectOfType<SunController>();
        if (sun != null)
        {
            Debug.Log($"☀️ Sun Angle: {sun.GetCurrentAngle():F0}°");
        }
    }

    [MenuItem("Last Drop/Clear Scene")]
    public static void ClearSceneOnly()
    {
        if (EditorUtility.DisplayDialog("پاک کردن صحنه",
            "آیا مطمئن هستید که می‌خواهید همه اجسام را پاک کنید؟",
            "بله", "خیر"))
        {
            ClearScene();
            Debug.Log("🧹 صحنه پاک شد");
        }
    }

    [MenuItem("Last Drop/Setup Layers & Tags")]
    public static void SetupLayersAndTags()
    {
        Debug.Log("🏷️ راه‌اندازی Layers و Tags...");

        Debug.Log("لطفاً این Tags را دستی ایجاد کنید:");
        Debug.Log("- Player");
        Debug.Log("- WaterPickup");
        Debug.Log("- ShadowCaster");
        Debug.Log("- Finish");

        Debug.Log("و این Layers را:");
        Debug.Log("- Shadow (Layer 8)");
        Debug.Log("- Water (Layer 9)");
        Debug.Log("- UI (Layer 10)");

        Debug.Log("💡 Window > Layers and Tags برای تنظیم");
    }

    [MenuItem("Last Drop/Performance Check")]
    public static void PerformanceCheck()
    {
        Debug.Log("🚀 بررسی کارایی...");

        int totalObjects = FindObjectsOfType<GameObject>().Length;
        int totalRenderers = FindObjectsOfType<Renderer>().Length;
        int totalColliders = FindObjectsOfType<Collider>().Length;
        int totalLights = FindObjectsOfType<Light>().Length;

        Debug.Log($"📊 آمار صحنه:");
        Debug.Log($"   - اجسام: {totalObjects}");
        Debug.Log($"   - Renderers: {totalRenderers}");
        Debug.Log($"   - Colliders: {totalColliders}");
        Debug.Log($"   - نورها: {totalLights}");

        if (totalObjects > 100)
            Debug.LogWarning("⚠️ تعداد اجسام زیاد - ممکن است کارایی کاهش یابد");

        if (totalLights > 3)
            Debug.LogWarning("⚠️ نورهای زیاد - بهتر است تعداد را کم کنید");

        Debug.Log("✅ بررسی کارایی تکمیل شد");
    }

    [MenuItem("Last Drop/Export Level")]
    public static void ExportLevel()
    {
        string levelData = GenerateLevelData();
        string path = EditorUtility.SaveFilePanel("Export Level", "", "LastDrop_Level", "txt");

        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, levelData);
            Debug.Log($"💾 سطح ذخیره شد: {path}");
        }
    }

    static string GenerateLevelData()
    {
        string data = "=== LAST DROP LEVEL DATA ===\n\n";

        // اطلاعات بازیکن
        WaterDrop player = FindObjectOfType<WaterDrop>();
        if (player != null)
        {
            data += $"PLAYER_START: {player.transform.position.x:F2}, {player.transform.position.y:F2}, {player.transform.position.z:F2}\n";
        }

        // موانع
        ShadowProjector[] obstacles = FindObjectsOfType<ShadowProjector>();
        data += $"\nOBSTACLES: {obstacles.Length}\n";
        foreach (var obs in obstacles)
        {
            Vector3 pos = obs.transform.position;
            Vector3 scale = obs.transform.localScale;
            data += $"OBSTACLE: {pos.x:F2}, {pos.y:F2}, {pos.z:F2}, {scale.x:F2}, {scale.y:F2}, {scale.z:F2}\n";
        }

        // موانع پویا
        DynamicObstacle[] dynamicObs = FindObjectsOfType<DynamicObstacle>();
        data += $"\nDYNAMIC_OBSTACLES: {dynamicObs.Length}\n";
        foreach (var dyn in dynamicObs)
        {
            Vector3 pos = dyn.transform.position;
            data += $"DYNAMIC: {pos.x:F2}, {pos.y:F2}, {pos.z:F2}, Moving={dyn.isMoving}, Pulsing={dyn.isPulsing}\n";
        }

        // Water Pickups
        WaterPickup[] pickups = FindObjectsOfType<WaterPickup>();
        data += $"\nWATER_PICKUPS: {pickups.Length}\n";
        foreach (var pickup in pickups)
        {
            Vector3 pos = pickup.transform.position;
            data += $"PICKUP: {pos.x:F2}, {pos.y:F2}, {pos.z:F2}, Water={pickup.waterAmount}\n";
        }

        // نقطه پایان
        Transform endPoint = GameObject.Find("EndPoint")?.transform;
        if (endPoint != null)
        {
            Vector3 pos = endPoint.position;
            data += $"\nEND_POINT: {pos.x:F2}, {pos.y:F2}, {pos.z:F2}\n";
        }

        return data;
    }
#endif
}