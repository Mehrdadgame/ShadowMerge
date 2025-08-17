// QuickSetup.cs - اسکریپت راه‌اندازی کامل Last Drop (URP Compatible)
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class QuickSetupFixed : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Last Drop/Fixed Quick Setup Scene")]
    public static void SetupScene()
    {
        Debug.Log("🚀 شروع راه‌اندازی Last Drop (URP Version)...");

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

        // ایجاد UI کامل
        CreateCompleteUI();

        Debug.Log("🎉 Last Drop راه‌اندازی کامل شد!");
        Debug.Log("✨ ویژگی‌ها: URP Shaders, Complete UI, Enhanced Systems");
        Debug.Log("🎮 برای تست دکمه Play را بزنید!");
    }

    static void ClearScene()
    {
        // حذف همه objects به جز دوربین اصلی و نور
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;

            // حفظ دوربین اصلی و نور directional
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

        cam.transform.position = new Vector3(0, 12, -10);
        cam.transform.rotation = Quaternion.Euler(45, 0, 0);
        cam.orthographic = true;
        cam.orthographicSize = 10;
        cam.backgroundColor = new Color(0.9f, 0.6f, 0.3f);

        // Camera Controller
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

        // متریال URP برای زمین
        Material groundMat = CreateURPMaterial("GroundMaterial");
        groundMat.color = new Color(0.85f, 0.65f, 0.4f);
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

    static Material CreateURPMaterial(string name)
    {
        // تلاش برای استفاده از URP Lit shader
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null)
        {
            // اگر URP موجود نباشد، از Built-in استفاده کن
            urpShader = Shader.Find("Standard");
        }

        Material mat = new Material(urpShader);
        mat.name = name;
        return mat;
    }

    static Material CreateTransparentURPMaterial(string name, Color color)
    {
        Material mat = CreateURPMaterial(name);

        // تنظیمات شفافیت برای URP
        if (mat.shader.name.Contains("Universal"))
        {
            // URP Transparent settings
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0); // Alpha
            mat.SetFloat("_AlphaCutoff", 0);
            mat.SetFloat("_SrcBlend", 5); // SrcAlpha
            mat.SetFloat("_DstBlend", 10); // OneMinusSrcAlpha
            mat.SetFloat("_ZWrite", 0);
            mat.renderQueue = 3000;

            // Enable transparency keywords for URP
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        else
        {
            // Built-in transparency settings
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
        }

        mat.color = color;
        return mat;
    }

    static void CreateStaticObstacle(string name, Vector3 position)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = name;
        obstacle.transform.position = position;
        obstacle.transform.localScale = new Vector3(1.2f, 2.5f, 1.2f);

        // متریال URP
        Material mat = CreateURPMaterial("ObstacleMaterial");
        mat.color = new Color(0.5f, 0.3f, 0.2f);
        obstacle.GetComponent<MeshRenderer>().material = mat;

        // ShadowProjector
        ShadowProjector shadowProj = obstacle.AddComponent<ShadowProjector>();
        shadowProj.shadowMaterial = CreateShadowMaterial();
        shadowProj.shadowLength = 7f;
        shadowProj.shadowWidth = 2.5f;

        obstacle.tag = "ShadowCaster";
    }

    static Material CreateShadowMaterial()
    {
        return CreateTransparentURPMaterial("ShadowMaterial", new Color(0.05f, 0.05f, 0.05f, 0.8f));
    }

    static void CreateMovingObstacle(string name, Vector3 position)
    {
        CreateStaticObstacle(name, position);
        GameObject obstacle = GameObject.Find(name);

        DynamicObstacle dynamic = obstacle.AddComponent<DynamicObstacle>();
        dynamic.isMoving = true;
        dynamic.moveSpeed = 2f;
        dynamic.waitTime = 1.5f;

        Vector3[] waypoints = new Vector3[4];
        waypoints[0] = position;
        waypoints[1] = position + new Vector3(3, 0, 0);
        waypoints[2] = position + new Vector3(3, 0, 3);
        waypoints[3] = position + new Vector3(0, 0, 3);
        dynamic.waypoints = waypoints;

        MeshRenderer renderer = obstacle.GetComponent<MeshRenderer>();
        Material mat = CreateURPMaterial("MovingObstacleMaterial");
        mat.color = new Color(0.7f, 0.3f, 0.7f);
        renderer.material = mat;
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

        MeshRenderer renderer = obstacle.GetComponent<MeshRenderer>();
        Material mat = CreateURPMaterial("PulsingObstacleMaterial");
        mat.color = new Color(0.2f, 0.5f, 0.8f);
        renderer.material = mat;
    }

    static void CreateEndPoint()
    {
        GameObject endPoint = new GameObject("EndPoint");
        endPoint.transform.position = new Vector3(0, 0.5f, 8);
        endPoint.tag = "Finish";

        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        indicator.transform.parent = endPoint.transform;
        indicator.transform.localPosition = Vector3.zero;
        indicator.transform.localScale = new Vector3(1.5f, 0.1f, 1.5f);

        Material endMat = CreateURPMaterial("EndPointMaterial");
        endMat.color = Color.green;
        indicator.GetComponent<MeshRenderer>().material = endMat;

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

        // متریال شفاف آبی URP
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

    static void CreatePlayer()
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "WaterDrop";
        player.transform.position = new Vector3(0, 0.6f, -6);
        player.transform.localScale = Vector3.one * 0.5f;
        player.tag = "Player";

        // متریال آب URP
        Material waterMat = CreateTransparentURPMaterial("WaterDropMaterial", new Color(0.1f, 0.6f, 1f, 0.9f));
        player.GetComponent<MeshRenderer>().material = waterMat;

        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.mass = 0.2f;
        rb.linearDamping = 10f;
        rb.angularDamping = 15f;

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
        gm.waterDecayRate = 1f;
        gm.sunlightDecayRate = 12f;
        gm.levelTime = 60f;
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
        light.intensity = 1f; // کاهش برای URP
        light.shadows = LightShadows.Soft;
        sun.sunLight = light;

        // سایر منیجرها
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

    static void CreateCompleteUI()
    {
        Debug.Log("🖼️ شروع ایجاد UI کامل...");

        // EventSystem (ضروری برای UI)
        CreateEventSystem();

        // Canvas اصلی
        GameObject canvasObj = CreateMainCanvas();

        // ایجاد همه عناصر UI
        GameObject waterBarObj = CreateWaterBarUI(canvasObj);
        GameObject timerObj = CreateTimerUI(canvasObj);
        GameObject sunSystemObj = CreateSunUI(canvasObj);
        GameObject winScreenObj = CreateWinScreen(canvasObj);
        GameObject loseScreenObj = CreateLoseScreen(canvasObj);
        GameObject countdownObj = CreateCountdownUI(canvasObj);

        // UI Manager
        GameObject uiManagerObj = new GameObject("UI_Manager");
        uiManagerObj.transform.SetParent(canvasObj.transform, false);
        UI_Manager uiManager = uiManagerObj.AddComponent<UI_Manager>();

        // اتصال ارجاعات
        ConnectUIReferences(uiManager, canvasObj, waterBarObj, timerObj, sunSystemObj,
                          winScreenObj, loseScreenObj, countdownObj);

        // اتصال به Game Manager
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.uiManager = uiManager;

        Debug.Log("✅ UI کامل ایجاد شد");
    }

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

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        return canvasObj;
    }

    static GameObject CreateWaterBarUI(GameObject canvas)
    {
        // پنل پس‌زمینه
        GameObject panelObj = new GameObject("WaterBarPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.3f);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.02f, 0.02f);
        panelRect.anchorMax = new Vector2(0.52f, 0.18f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // برچسب آب
        GameObject labelObj = new GameObject("WaterLabel");
        labelObj.transform.SetParent(panelObj.transform, false);

        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = "💧 آب";
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 32;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.05f, 0.7f);
        labelRect.anchorMax = new Vector2(0.95f, 0.95f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        // Slider آب
        GameObject sliderObj = new GameObject("WaterSlider");
        sliderObj.transform.SetParent(panelObj.transform, false);

        Slider slider = sliderObj.AddComponent<Slider>();
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.05f, 0.15f);
        sliderRect.anchorMax = new Vector2(0.95f, 0.65f);
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        // پس‌زمینه Slider
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        SetFullRect(bgObj.GetComponent<RectTransform>());

        // Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        SetFullRect(fillAreaObj.GetComponent<RectTransform>());

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.1f, 0.7f, 1f, 0.9f);
        SetFullRect(fillObj.GetComponent<RectTransform>());

        slider.fillRect = fillObj.GetComponent<RectTransform>();
        slider.value = 1f;

        return panelObj;
    }

    static GameObject CreateTimerUI(GameObject canvas)
    {
        // پنل تایمر
        GameObject timerPanelObj = new GameObject("TimerPanel");
        timerPanelObj.transform.SetParent(canvas.transform, false);

        Image panelImage = timerPanelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.4f);

        RectTransform panelRect = timerPanelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.35f, 0.8f);
        panelRect.anchorMax = new Vector2(0.65f, 0.98f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // آیکون ساعت
        GameObject iconObj = new GameObject("TimerIcon");
        iconObj.transform.SetParent(timerPanelObj.transform, false);

        Text iconText = iconObj.AddComponent<Text>();
        iconText.text = "⏰";
        iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        iconText.fontSize = 40;
        iconText.color = Color.yellow;
        iconText.alignment = TextAnchor.MiddleCenter;

        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.05f, 0.1f);
        iconRect.anchorMax = new Vector2(0.35f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        // متن تایمر
        GameObject timerTextObj = new GameObject("TimerText");
        timerTextObj.transform.SetParent(timerPanelObj.transform, false);

        Text timerText = timerTextObj.AddComponent<Text>();
        timerText.text = "60";
        timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timerText.fontSize = 60;
        timerText.color = Color.white;
        timerText.alignment = TextAnchor.MiddleCenter;
        timerText.fontStyle = FontStyle.Bold;

        Outline outline = timerTextObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3, -3);

        RectTransform textRect = timerTextObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.4f, 0.1f);
        textRect.anchorMax = new Vector2(0.95f, 0.9f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return timerTextObj; // برگرداندن Text object برای ارجاع
    }

    static GameObject CreateSunUI(GameObject canvas)
    {
        GameObject sunSystemObj = new GameObject("SunSystem");
        sunSystemObj.transform.SetParent(canvas.transform, false);

        RectTransform sunRect = sunSystemObj.GetComponent<RectTransform>();
        sunRect.anchorMin = new Vector2(0.55f, 0.8f);
        sunRect.anchorMax = new Vector2(0.98f, 0.98f);
        sunRect.offsetMin = Vector2.zero;
        sunRect.offsetMax = Vector2.zero;

        // پس‌زمینه مسیر
        GameObject pathBgObj = new GameObject("PathBackground");
        pathBgObj.transform.SetParent(sunSystemObj.transform, false);
        Image pathBgImage = pathBgObj.AddComponent<Image>();
        pathBgImage.color = new Color(0, 0, 0, 0.3f);
        SetFullRect(pathBgObj.GetComponent<RectTransform>());

        // مسیر خورشید
        GameObject pathObj = new GameObject("SunPath");
        pathObj.transform.SetParent(sunSystemObj.transform, false);
        Image pathImage = pathObj.AddComponent<Image>();
        pathImage.color = new Color(1f, 1f, 0.3f, 0.6f);

        RectTransform pathRect = pathObj.GetComponent<RectTransform>();
        pathRect.anchorMin = new Vector2(0.05f, 0.3f);
        pathRect.anchorMax = new Vector2(0.95f, 0.7f);
        pathRect.offsetMin = Vector2.zero;
        pathRect.offsetMax = Vector2.zero;

        // آیکون خورشید
        GameObject sunIconObj = new GameObject("SunIcon");
        sunIconObj.transform.SetParent(sunSystemObj.transform, false);

        Text sunText = sunIconObj.AddComponent<Text>();
        sunText.text = "☀️";
        sunText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sunText.fontSize = 50;
        sunText.alignment = TextAnchor.MiddleCenter;

        RectTransform iconRect = sunIconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.45f, 0.4f);
        iconRect.anchorMax = new Vector2(0.55f, 0.6f);
        iconRect.sizeDelta = new Vector2(80, 80);

        // برچسب
        GameObject labelObj = new GameObject("SunLabel");
        labelObj.transform.SetParent(sunSystemObj.transform, false);

        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = "☀️ موقعیت خورشید";
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 24;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleCenter;

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.05f);
        labelRect.anchorMax = new Vector2(1, 0.25f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return sunSystemObj;
    }

    static GameObject CreateWinScreen(GameObject canvas)
    {
        GameObject winScreenObj = new GameObject("WinScreen");
        winScreenObj.transform.SetParent(canvas.transform, false);

        Image screenImage = winScreenObj.AddComponent<Image>();
        screenImage.color = new Color(0, 0.5f, 0, 0.9f);
        SetFullRect(winScreenObj.GetComponent<RectTransform>());

        // متن برد
        GameObject winTextObj = new GameObject("WinText");
        winTextObj.transform.SetParent(winScreenObj.transform, false);

        Text winText = winTextObj.AddComponent<Text>();
        winText.text = "🎉 برنده شدی! 🎉";
        winText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        winText.fontSize = 80;
        winText.color = Color.white;
        winText.alignment = TextAnchor.MiddleCenter;
        winText.fontStyle = FontStyle.Bold;

        Outline outline = winTextObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(5, -5);

        RectTransform winTextRect = winTextObj.GetComponent<RectTransform>();
        winTextRect.anchorMin = new Vector2(0.1f, 0.6f);
        winTextRect.anchorMax = new Vector2(0.9f, 0.9f);
        winTextRect.offsetMin = Vector2.zero;
        winTextRect.offsetMax = Vector2.zero;

        // دکمه ریستارت
        CreateRestartButton(winScreenObj);

        winScreenObj.SetActive(false);
        return winScreenObj;
    }

    static GameObject CreateLoseScreen(GameObject canvas)
    {
        GameObject loseScreenObj = new GameObject("LoseScreen");
        loseScreenObj.transform.SetParent(canvas.transform, false);

        Image screenImage = loseScreenObj.AddComponent<Image>();
        screenImage.color = new Color(0.5f, 0, 0, 0.9f);
        SetFullRect(loseScreenObj.GetComponent<RectTransform>());

        // متن باخت
        GameObject loseTextObj = new GameObject("LoseText");
        loseTextObj.transform.SetParent(loseScreenObj.transform, false);

        Text loseText = loseTextObj.AddComponent<Text>();
        loseText.text = "😞 دوباره تلاش کن 😞";
        loseText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        loseText.fontSize = 80;
        loseText.color = Color.white;
        loseText.alignment = TextAnchor.MiddleCenter;
        loseText.fontStyle = FontStyle.Bold;

        Outline outline = loseTextObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(5, -5);

        RectTransform loseTextRect = loseTextObj.GetComponent<RectTransform>();
        loseTextRect.anchorMin = new Vector2(0.1f, 0.6f);
        loseTextRect.anchorMax = new Vector2(0.9f, 0.9f);
        loseTextRect.offsetMin = Vector2.zero;
        loseTextRect.offsetMax = Vector2.zero;

        // دکمه ریستارت
        CreateRestartButton(loseScreenObj);

        loseScreenObj.SetActive(false);
        return loseScreenObj;
    }

    static void CreateRestartButton(GameObject parent)
    {
        GameObject buttonObj = new GameObject("RestartButton");
        buttonObj.transform.SetParent(parent.transform, false);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        Button button = buttonObj.AddComponent<Button>();

        ColorBlock colors = button.colors;
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

        Text btnText = buttonTextObj.AddComponent<Text>();
        btnText.text = "🔄 شروع مجدد";
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 48;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.fontStyle = FontStyle.Bold;

        SetFullRect(buttonTextObj.GetComponent<RectTransform>());
    }

    static GameObject CreateCountdownUI(GameObject canvas)
    {
        GameObject countdownObj = new GameObject("CountdownPanel");
        countdownObj.transform.SetParent(canvas.transform, false);

        Image panelImage = countdownObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);
        SetFullRect(countdownObj.GetComponent<RectTransform>());

        // متن شمارش معکوس
        GameObject countdownTextObj = new GameObject("CountdownText");
        countdownTextObj.transform.SetParent(countdownObj.transform, false);

        Text countdownText = countdownTextObj.AddComponent<Text>();
        countdownText.text = "3";
        countdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countdownText.fontSize = 200;
        countdownText.color = Color.white;
        countdownText.alignment = TextAnchor.MiddleCenter;
        countdownText.fontStyle = FontStyle.Bold;

        Outline outline = countdownTextObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(8, -8);

        SetFullRect(countdownTextObj.GetComponent<RectTransform>());

        countdownObj.SetActive(false);
        return countdownObj;
    }

    static void ConnectUIReferences(UI_Manager uiManager, GameObject canvas,
                                  GameObject waterBarObj, GameObject timerObj, GameObject sunSystemObj,
                                  GameObject winScreenObj, GameObject loseScreenObj, GameObject countdownObj)
    {
        // Water Bar
        Slider waterSlider = waterBarObj.GetComponentInChildren<Slider>();
        if (waterSlider != null)
            uiManager.waterBar = waterSlider;

        // Timer Text
        if (timerObj != null)
            uiManager.timerText = timerObj.GetComponent<Text>();

        // Sun System
        if (sunSystemObj != null)
        {
            Transform sunIconTransform = FindChildRecursive(sunSystemObj.transform, "SunIcon");
            if (sunIconTransform != null)
            {
                Text sunText = sunIconTransform.GetComponent<Text>();
                if (sunText != null)
                {
                    // ایجاد Image component برای sunIcon اگر وجود ندارد
                    Image sunImage = sunIconTransform.GetComponent<Image>();
                    if (sunImage == null)
                    {
                        sunImage = sunIconTransform.gameObject.AddComponent<Image>();
                        sunImage.color = Color.clear; // شفاف تا Text نمایان باشد
                    }
                    uiManager.sunIcon = sunImage;
                }
            }
            uiManager.sunPath = sunSystemObj.GetComponent<RectTransform>();
        }

        // Screens
        uiManager.winScreen = winScreenObj;
        uiManager.loseScreen = loseScreenObj;

        // Countdown
        uiManager.countdownPanel = countdownObj;
        Transform countdownTextTransform = FindChildRecursive(countdownObj.transform, "CountdownText");
        if (countdownTextTransform != null)
            uiManager.countdownText = countdownTextTransform.GetComponent<Text>();

        // Restart Button
        Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.name == "RestartButton")
            {
                uiManager.restartButton = btn;
                break;
            }
        }

        Debug.Log("🔗 ارجاعات UI متصل شدند");
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

        Debug.Log("💧 Water Pickup ایجاد شد!");
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

        Debug.Log("⚡ Moving Obstacle ایجاد شد!");
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

        Debug.Log("💓 Pulsing Obstacle ایجاد شد!");
    }

    [MenuItem("Last Drop/Test All Systems")]
    public static void TestAllSystems()
    {
        Debug.Log("=== تست سیستم‌های Last Drop ===");

        // تست GameManager
        GameManager gm = FindObjectOfType<GameManager>();
        Debug.Log($"GameManager: {(gm != null ? "✅" : "❌")}");
        if (gm != null)
        {
            Debug.Log($"   - WaterDrop: {(gm.waterDrop != null ? "✅" : "❌")}");
            Debug.Log($"   - SunController: {(gm.sunController != null ? "✅" : "❌")}");
            Debug.Log($"   - UI Manager: {(gm.uiManager != null ? "✅" : "❌")}");
        }

        // تست UI Manager
        UI_Manager ui = FindObjectOfType<UI_Manager>();
        Debug.Log($"UI_Manager: {(ui != null ? "✅" : "❌")}");
        if (ui != null)
        {
            Debug.Log($"   - Water Bar: {(ui.waterBar != null ? "✅" : "❌")}");
            Debug.Log($"   - Timer Text: {(ui.timerText != null ? "✅" : "❌")}");
            Debug.Log($"   - Sun Icon: {(ui.sunIcon != null ? "✅" : "❌")}");
            Debug.Log($"   - Win Screen: {(ui.winScreen != null ? "✅" : "❌")}");
            Debug.Log($"   - Lose Screen: {(ui.loseScreen != null ? "✅" : "❌")}");
            Debug.Log($"   - Countdown Panel: {(ui.countdownPanel != null ? "✅" : "❌")}");
            Debug.Log($"   - Restart Button: {(ui.restartButton != null ? "✅" : "❌")}");
        }

        // تست EventSystem
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        Debug.Log($"EventSystem: {(eventSystem != null ? "✅" : "❌")}");

        // تست Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        Debug.Log($"Canvas: {(canvas != null ? "✅" : "❌")}");

        // تست موانع و سایه‌ها
        ShadowProjector[] shadows = FindObjectsOfType<ShadowProjector>();
        Debug.Log($"Shadow Projectors: {shadows.Length} ✅");

        DynamicObstacle[] dynamicObstacles = FindObjectsOfType<DynamicObstacle>();
        Debug.Log($"Dynamic Obstacles: {dynamicObstacles.Length} ✅");

        WaterPickup[] pickups = FindObjectsOfType<WaterPickup>();
        Debug.Log($"Water Pickups: {pickups.Length} ✅");

        Debug.Log("=== تست کامل شد ===");
        Debug.Log("🚀 آماده بازی! دکمه Play را بزنید");
    }

    [MenuItem("Last Drop/Check URP Compatibility")]
    public static void CheckURPCompatibility()
    {
        Debug.Log("=== بررسی سازگاری URP ===");

        // بررسی شیدرها
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Debug.Log($"URP Lit Shader: {(urpLit != null ? "✅ یافت شد" : "❌ یافت نشد - از Built-in استفاده می‌شود")}");

        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        Debug.Log($"URP Unlit Shader: {(urpUnlit != null ? "✅ یافت شد" : "❌ یافت نشد")}");

        // بررسی متریال‌ها
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        int urpMaterials = 0;
        int builtinMaterials = 0;

        foreach (var renderer in renderers)
        {
            if (renderer.material != null)
            {
                if (renderer.material.shader.name.Contains("Universal"))
                    urpMaterials++;
                else
                    builtinMaterials++;
            }
        }

        Debug.Log($"متریال‌های URP: {urpMaterials}");
        Debug.Log($"متریال‌های Built-in: {builtinMaterials}");

        if (urpLit != null)
            Debug.Log("✅ پروژه شما با URP سازگار است");
        else
            Debug.LogWarning("⚠️ URP تشخیص داده نشد - متریال‌ها با Built-in shader ایجاد شدند");

        Debug.Log("=== بررسی تکمیل شد ===");
    }

    [MenuItem("Last Drop/Fix Missing Components")]
    public static void FixMissingComponents()
    {
        Debug.Log("🔧 شروع اصلاح اجزای ناقص...");

        // اصلاح UI Manager
        UI_Manager uiManager = FindObjectOfType<UI_Manager>();
        if (uiManager == null)
        {
            GameObject uiManagerObj = GameObject.Find("UI_Manager");
            if (uiManagerObj == null)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    uiManagerObj = new GameObject("UI_Manager");
                    uiManagerObj.transform.SetParent(canvas.transform, false);
                    uiManager = uiManagerObj.AddComponent<UI_Manager>();
                    Debug.Log("✅ UI_Manager اضافه شد");
                }
            }
        }

        // اصلاح EventSystem
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            CreateEventSystem();
            Debug.Log("✅ EventSystem اضافه شد");
        }

        // اصلاح ارجاعات GameManager
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null && uiManager != null && gm.uiManager == null)
        {
            gm.uiManager = uiManager;
            Debug.Log("✅ ارجاع UI Manager به GameManager اضافه شد");
        }

        Debug.Log("🔧 اصلاح تکمیل شد");
    }

    [MenuItem("Last Drop/Performance Tips")]
    public static void ShowPerformanceTips()
    {
        Debug.Log("=== نکات کارایی ===");
        Debug.Log("💡 برای بهبود کارایی:");
        Debug.Log("   1. از URP استفاده کنید");
        Debug.Log("   2. تعداد نورها را کم نگه دارید (حداکثر 3-4)");
        Debug.Log("   3. از Object Pooling برای پارتیکل‌ها استفاده کنید");
        Debug.Log("   4. Texture ها را کمپرس کنید");
        Debug.Log("   5. از LOD برای مدل‌های پیچیده استفاده کنید");
        Debug.Log("======================");
    }

    [MenuItem("Last Drop/Clear Scene Only")]
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

    [MenuItem("Last Drop/Create Complete Scene")]
    public static void CreateCompleteScene()
    {
        if (EditorUtility.DisplayDialog("ایجاد صحنه کامل",
            "آیا می‌خواهید یک صحنه کاملاً جدید ایجاد کنید؟\n(این عمل صحنه فعلی را پاک می‌کند)",
            "ایجاد صحنه جدید", "لغو"))
        {
            SetupScene();
        }
    }

#endif
}