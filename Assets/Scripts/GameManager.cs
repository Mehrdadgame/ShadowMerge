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
    public float startDelay = 3f; // تاخیر شروع بازی
    public bool showCountdown = true;

    [Header("References")]
    public WaterDrop waterDrop;
    public SunController sunController;
    public UI_Manager uiManager;
    public Transform levelEndPoint;

    private float currentWater = 100f;
    private float currentTime;
    private bool gameActive = false; // شروع با false
    private bool gameStarted = false;
    private float levelStartTime;

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

        // آنالیتیکس شروع مرحله
        AnalyticsTracker.TrackLevelStart(currentLevel);

        StartCoroutine(StartGameSequence());
    }

    IEnumerator StartGameSequence()
    {
        // نمایش شمارش معکوس
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

        // شروع بازی
        gameActive = true;
        gameStarted = true;

        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        bool wasInShadowLastFrame = waterDrop != null ? waterDrop.IsInShadow() : false;

        while (gameActive && currentTime > 0 && currentWater > 0)
        {
            if (gameStarted) // فقط وقتی بازی شروع شده باشه
            {
                currentTime -= Time.deltaTime;

                // بررسی وضعیت سایه - FIXED
                bool inShadowNow = false;

                // اگر از DropPathFollower استفاده می‌کنید:
                DropPathFollower pathFollower = waterDrop?.GetComponent<DropPathFollower>();
                if (pathFollower != null)
                {
                    inShadowNow = pathFollower.IsInShadow();
                }
                // یا اگر هنوز از WaterDrop استفاده می‌کنید:
                else if (waterDrop != null)
                {
                    inShadowNow = waterDrop.IsInShadow();
                }

                // محاسبه Decay - منطق اصلاح شده
                if (inShadowNow)
                {
                    // در سایه: decay کمتر یا صفر
                    float shadowProtection = GetShadowStrength();
                    float protectedDecayRate = waterDecayRate * (1f - shadowProtection);
                    currentWater -= protectedDecayRate * Time.deltaTime;

                    if (Time.time % 1f < 0.1f) // Debug هر 1 ثانیه
                    {
                        Debug.Log($"🛡️ IN SHADOW - Protection: {shadowProtection * 100:F0}% - Decay: {protectedDecayRate:F1}/s");
                    }
                }
                else
                {
                    // در نور خورشید: decay سریع
                    currentWater -= sunlightDecayRate * Time.deltaTime;

                    if (Time.time % 1f < 0.1f) // Debug هر 1 ثانیه
                    {
                        Debug.Log($"☀️ IN SUNLIGHT - Fast Decay: {sunlightDecayRate:F1}/s");
                    }
                }

                // افکت تبخیر وقتی از سایه خارج می‌شود
                if (wasInShadowLastFrame && !inShadowNow)
                {
                    if (waterDrop != null)
                    {
                        ParticleManager.Instance?.PlayEvaporation(waterDrop.transform.position);
                        AudioManager.Instance?.PlayEvaporation();

                        // کمی لرزش دوربین
                        CameraController cam = FindObjectOfType<CameraController>();
                        cam?.ShakeCamera();
                    }
                }

                wasInShadowLastFrame = inShadowNow;

                // بروزرسانی UI
                if (uiManager != null)
                {
                    uiManager.UpdateWaterBar(currentWater / 100f);
                    uiManager.UpdateTimer(currentTime);
                    if (sunController != null)
                    {
                        uiManager.UpdateSunPosition(sunController.GetCurrentAngle());
                    }
                }

                // بررسی شرایط برد - اگر از Path System استفاده می‌کنید
                DropPathFollower follower = waterDrop?.GetComponent<DropPathFollower>();
                if (follower != null && !follower.IsMoving())
                {
                    // قطره به پایان مسیر رسیده
                    WinLevel();
                    yield break;
                }
                // یا روش قدیمی:
                else if (waterDrop != null && levelEndPoint != null)
                {
                    if (Vector3.Distance(waterDrop.transform.position, levelEndPoint.position) < 1f)
                    {
                        WinLevel();
                        yield break;
                    }
                }
            }

            yield return null;
        }

        // بررسی شرایط باخت
        if (currentWater <= 0)
        {
            LoseLevel("no_water");
        }
        else if (currentTime <= 0)
        {
            LoseLevel("time_out");
        }
    }

    // محاسبه قدرت سایه برای کم کردن تدریجی
    float GetShadowStrength()
    {
        if (waterDrop == null) return 0f;

        // اگر از DropPathFollower استفاده می‌کنید:
        DropPathFollower pathFollower = waterDrop.GetComponent<DropPathFollower>();
        if (pathFollower != null)
        {
            ShadowProjector currenttShadow = pathFollower.GetCurrentShadow();
            if (currenttShadow == null) return 0f;
            return currenttShadow.GetShadowStrength(waterDrop.transform.position);
        }

        // روش قدیمی:
        ShadowProjector currentShadow = waterDrop.GetCurrentShadow();
        if (currentShadow == null) return 0f;

        return currentShadow.GetShadowStrength(waterDrop.transform.position);
    }

    public void WinLevel()
    {
        gameActive = false;
        float completionTime = Time.time - levelStartTime;

        // Effects & Audio
        if (waterDrop != null)
        {
            ParticleManager.Instance?.PlayWinEffect(waterDrop.transform.position);
        }
        AudioManager.Instance?.PlayWin();

        // Progress tracking
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

        // Visual feedback
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

    // جمع آوری آب خودکار (ایده جدید)
    public void CollectNearbyWater()
    {
        if (waterDrop == null) return;

        WaterPickup[] pickups = FindObjectsOfType<WaterPickup>();
        foreach (var pickup in pickups)
        {
            float distance = Vector3.Distance(waterDrop.transform.position, pickup.transform.position);
            if (distance < 2f) // جمع آوری خودکار در فاصله 2 متری
            {
                AddWater(pickup.waterAmount * 0.5f); // نصف مقدار
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
        // جمع آوری خودکار آب (هر 2 ثانیه یکبار)
        if (gameActive && Time.time % 2f < 0.1f)
        {
            CollectNearbyWater();
        }

        // بررسی تغییر سایه برای کومبو
        CheckShadowJumps();
    }

    private ShadowProjector lastShadow;
    void CheckShadowJumps()
    {
        if (waterDrop == null) return;

        ShadowProjector currentShadow = waterDrop.GetCurrentShadow();

        // اگر از سایه‌ای به سایه دیگر پرید
        if (currentShadow != null && lastShadow != null &&
            currentShadow != lastShadow && waterDrop.IsInShadow())
        {
            ComboSystem.Instance?.AddShadowJump();
        }

        lastShadow = currentShadow;
    }
}