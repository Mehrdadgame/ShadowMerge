using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public float waterDecayRate = 2f;
    public float sunlightDecayRate = 10f;
    public float levelTime = 40f;
    public int currentLevel = 1;

    [Header("Game Flow")]
    public float startDelay = 3f;
    public bool showCountdown = true;

    [Header("References")]
    public WaterDrop waterDrop;
    public SunController sunController;
    public UI_Manager uiManager;
    public Transform levelEndPoint;

    [Header("Debug")]
    public bool debugShadowLogic = true;

    private float currentWater = 100f;
    private float currentTime;
    private bool gameActive = false;
    private bool gameStarted = false;
    private float levelStartTime;

    // برای تشخیص تغییر وضعیت سایه
    public bool lastShadowState = false;
    private float lastDebugTime = 0f;

    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        currentTime = levelTime;
        levelStartTime = Time.time;

        AnalyticsTracker.TrackLevelStart(currentLevel);

        StartCoroutine(StartGameSequence());
    }

    IEnumerator StartGameSequence()
    {
        if (showCountdown && uiManager != null)
        {
            for (int i = 3; i > 0; i--)
            {
                uiManager.ShowCountdown(i.ToString());
                yield return new WaitForSeconds(1f);
            }
            uiManager.ShowCountdown("GO!");
            yield return new WaitForSeconds(0.5f);
            uiManager.HideCountdown();
        }
        else
        {
            yield return new WaitForSeconds(startDelay);
        }

        gameActive = true;
        gameStarted = true;

        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        while (gameActive && currentTime > 0 && currentWater > 0)
        {
            if (gameStarted)
            {
                currentTime -= Time.deltaTime;

                // ===== تشخیص وضعیت سایه =====
                bool inShadowNow = GetPlayerShadowStatus();

                // ===== محاسبه و اعمال Decay =====
                ApplyWaterDecay(inShadowNow);

                // ===== Debug اطلاعات =====
                DebugShadowInfo(inShadowNow);

                // ===== افکت تغییر وضعیت =====
                if (lastShadowState != inShadowNow)
                {
                    OnShadowStateChanged(inShadowNow);
                    lastShadowState = inShadowNow;
                }

                // ===== بروزرسانی UI =====
                UpdateUI();

                // ===== بررسی شرایط برد =====
                CheckWinConditions();
                Debug.Log($"{inShadowNow}inShadowNow");
            }


            yield return null;
        }

        // بررسی باخت
        if (currentWater <= 0)
        {
            LoseLevel("no_water");
        }
        else if (currentTime <= 0)
        {
            LoseLevel("time_out");
        }
    }

    bool GetPlayerShadowStatus()
    {
        if (waterDrop == null) return false;

        // اولویت 1: SimpleShadowDetection (جدید)
        SimpleShadowDetection simpleDetector = waterDrop.GetComponent<SimpleShadowDetection>();
        if (simpleDetector != null)
        {
            return simpleDetector.IsInShadow();
        }

        // اولویت 2: DropPathFollower
        DropPathFollower pathFollower = Object.FindAnyObjectByType<DropPathFollower>();
        if (pathFollower != null)
        {
            return pathFollower.IsInShadow();
        }

        // اولویت 3: WaterDrop مستقیم
        return waterDrop.IsInShadow();
    }

    void ApplyWaterDecay(bool inShadow)
    {
        float decayAmount = 0f;

        if (inShadow)
        {
            // در سایه: محافظت شده
            float shadowStrength = GetShadowStrength();
            float protectionFactor = Mathf.Clamp01(shadowStrength * 0.9f); // حداکثر 90% محافظت
            float actualDecayRate = waterDecayRate * (1f - protectionFactor);

            decayAmount = actualDecayRate * Time.deltaTime;
        }
        else
        {
            // در نور: تبخیر سریع
            decayAmount = sunlightDecayRate * Time.deltaTime;
        }

        currentWater -= decayAmount;
        currentWater = Mathf.Max(0f, currentWater);
    }

    float GetShadowStrength()
    {
        if (waterDrop == null) return 0f;

        // اولویت 1: SimpleShadowDetection
        SimpleShadowDetection simpleDetector = waterDrop.GetComponent<SimpleShadowDetection>();
        if (simpleDetector != null)
        {
            ShadowProjector currenShadow = simpleDetector.GetCurrentShadow();
            if (currenShadow != null)
            {
                return currenShadow.GetShadowStrength(waterDrop.transform.position);
            }
        }

        // اولویت 2: DropPathFollower
        DropPathFollower pathFollower = waterDrop.GetComponent<DropPathFollower>();
        if (pathFollower != null)
        {
            ShadowProjector currenShadow = pathFollower.GetCurrentShadow();
            if (currenShadow != null)
            {
                return currenShadow.GetShadowStrength(waterDrop.transform.position);
            }
        }

        // اولویت 3: WaterDrop
        ShadowProjector currentShadow = waterDrop.GetCurrentShadow();
        if (currentShadow != null)
        {
            return currentShadow.GetShadowStrength(waterDrop.transform.position);
        }

        return 0f;
    }

    void OnShadowStateChanged(bool enteredShadow)
    {
        if (enteredShadow)
        {
            // وارد سایه شد
            if (debugShadowLogic)
            {
                Debug.Log("🛡️ GameManager: Player وارد سایه شد - محافظت فعال!");
            }
        }
        else
        {
            // از سایه خارج شد
            if (debugShadowLogic)
            {
                Debug.Log("☀️ GameManager: Player از سایه خارج شد - تبخیر سریع!");
            }

            // افکت تبخیر
            if (waterDrop != null)
            {
                ParticleManager.Instance?.PlayEvaporation(waterDrop.transform.position);
                AudioManager.Instance?.PlayEvaporation();

                CameraController cam = FindObjectOfType<CameraController>();
                cam?.ShakeCamera();
            }
        }
    }

    void DebugShadowInfo(bool inShadow)
    {
        if (!debugShadowLogic) return;

        // Debug هر 2 ثانیه یکبار
        if (Time.time - lastDebugTime > 2f)
        {
            float shadowStrength = GetShadowStrength();
            string currentShadowName = GetCurrentShadowName();

            if (inShadow)
            {
                float actualDecayRate = waterDecayRate * (1f - shadowStrength * 0.9f);
                Debug.Log($"🛡️ در سایه '{currentShadowName}' - قدرت: {shadowStrength * 100:F0}% - Decay: {actualDecayRate:F1}/s - آب: {currentWater:F0}%");
            }
            else
            {
                Debug.Log($"☀️ در نور - Decay: {sunlightDecayRate:F1}/s - آب: {currentWater:F0}%");
            }

            lastDebugTime = Time.time;
        }
    }

    string GetCurrentShadowName()
    {
        if (waterDrop == null) return "None";

        SimpleShadowDetection simpleDetector = waterDrop.GetComponent<SimpleShadowDetection>();
        if (simpleDetector != null && simpleDetector.GetCurrentShadow() != null)
        {
            return simpleDetector.GetCurrentShadow().name;
        }

        DropPathFollower pathFollower = waterDrop.GetComponent<DropPathFollower>();
        if (pathFollower != null && pathFollower.GetCurrentShadow() != null)
        {
            return pathFollower.GetCurrentShadow().name;
        }

        ShadowProjector currentShadow = waterDrop.GetCurrentShadow();
        return currentShadow != null ? currentShadow.name : "None";
    }

    void UpdateUI()
    {
        if (uiManager != null)
        {
            uiManager.UpdateWaterBar(currentWater / 100f);
            uiManager.UpdateTimer(currentTime);
            if (sunController != null)
            {
                uiManager.UpdateSunPosition(sunController.GetCurrentAngle());
            }
        }
    }

    void CheckWinConditions()
    {
        DropPathFollower follower = waterDrop?.GetComponent<DropPathFollower>();
        if (follower != null && !follower.IsMoving())
        {
            WinLevel();
            return;
        }

        if (waterDrop != null && levelEndPoint != null)
        {
            if (Vector3.Distance(waterDrop.transform.position, levelEndPoint.position) < 1f)
            {
                WinLevel();
                return;
            }
        }
    }

    public void WinLevel()
    {
        gameActive = false;
        float completionTime = Time.time - levelStartTime;

        if (waterDrop != null)
        {
            ParticleManager.Instance?.PlayWinEffect(waterDrop.transform.position);
        }
        AudioManager.Instance?.PlayWin();

        LevelProgressTracker.Instance?.CompleteLevel(completionTime, currentWater);

        if (uiManager != null)
        {
            uiManager.ShowWinScreen();
        }

        Debug.Log($"🎉 مرحله برنده شد در {completionTime:F1} ثانیه با {currentWater:F0}% آب!");
    }

    void LoseLevel(string reason)
    {
        gameActive = false;

        AudioManager.Instance?.PlayLose();
        AnalyticsTracker.TrackLevelFail(currentLevel, reason);

        if (uiManager != null)
        {
            uiManager.ShowLoseScreen();
        }

        string reasonText = reason == "no_water" ? "آب تمام شد" : "زمان تمام شد";
        Debug.Log($"😞 مرحله باخت: {reasonText}");
    }

    public void AddWater(float amount)
    {
        currentWater = Mathf.Min(100f, currentWater + amount);

        CameraController cam = FindObjectOfType<CameraController>();
        cam?.ShakeCamera();

        if (waterDrop != null)
        {
            ParticleManager.Instance?.PlayWaterCollect(waterDrop.transform.position);
        }
        AudioManager.Instance?.PlayWaterCollect();

        Debug.Log($"💧 آب جمع شد! فعلی: {currentWater:F0}%");
    }

    public void RestartLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public void CollectNearbyWater()
    {
        if (waterDrop == null) return;

        WaterPickup[] pickups = FindObjectsOfType<WaterPickup>();
        foreach (var pickup in pickups)
        {
            float distance = Vector3.Distance(waterDrop.transform.position, pickup.transform.position);
            if (distance < 2f)
            {
                AddWater(pickup.waterAmount * 0.5f);
                ParticleManager.Instance?.PlayWaterCollect(pickup.transform.position);
                Destroy(pickup.gameObject);
            }
        }
    }

    public float GetWaterPercentage() => currentWater / 100f;
    public float GetTimeRemaining() => currentTime;
    public bool IsGameActive() => gameActive;
    public bool IsGameStarted() => gameStarted;

    void Update()
    {
        if (gameActive && Time.time % 2f < 0.1f)
        {
            CollectNearbyWater();
        }

        CheckShadowJumps();
    }

    private ShadowProjector lastShadow;
    void CheckShadowJumps()
    {
        ShadowProjector currentShadow = null;

        SimpleShadowDetection simpleDetector = waterDrop?.GetComponent<SimpleShadowDetection>();
        if (simpleDetector != null)
        {
            currentShadow = simpleDetector.GetCurrentShadow();
        }
        else
        {
            DropPathFollower pathFollower = waterDrop?.GetComponent<DropPathFollower>();
            if (pathFollower != null)
            {
                currentShadow = pathFollower.GetCurrentShadow();
            }
            else if (waterDrop != null)
            {
                currentShadow = waterDrop.GetCurrentShadow();
            }
        }

        if (currentShadow != null && lastShadow != null &&
            currentShadow != lastShadow && GetPlayerShadowStatus())
        {
            ComboSystem.Instance?.AddShadowJump();
        }

        lastShadow = currentShadow;
    }
}