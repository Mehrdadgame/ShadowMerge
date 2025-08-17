using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public float waterDecayRate = 2f;
    public float sunlightDecayRate = 10f;
    public float levelTime = 40f;
    public int currentLevel = 1;

    [Header("References")]
    public WaterDrop waterDrop;
    public SunController sunController;
    public UI_Manager uiManager;
    public Transform levelEndPoint;

    private float currentWater = 100f;
    private float currentTime;
    private bool gameActive = true;
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

        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        bool wasInShadowLastFrame = waterDrop.IsInShadow();

        while (gameActive && currentTime > 0 && currentWater > 0)
        {
            currentTime -= Time.deltaTime;

            // Water decay based on if player is in shadow
            bool inShadowNow = waterDrop.IsInShadow();
            float decayRate = inShadowNow ? waterDecayRate : sunlightDecayRate;
            currentWater -= decayRate * Time.deltaTime;

            // افکت تبخیر وقتی از سایه خارج می‌شود
            if (wasInShadowLastFrame && !inShadowNow)
            {
                ParticleManager.Instance?.PlayEvaporation(waterDrop.transform.position);
                AudioManager.Instance?.PlayEvaporation();
            }

            wasInShadowLastFrame = inShadowNow;

            // Update UI
            uiManager.UpdateWaterBar(currentWater / 100f);
            uiManager.UpdateTimer(currentTime);
            uiManager.UpdateSunPosition(sunController.GetCurrentAngle());

            // Check win condition
            if (Vector3.Distance(waterDrop.transform.position, levelEndPoint.position) < 1f)
            {
                WinLevel();
                yield break;
            }

            yield return null;
        }

        if (currentWater <= 0)
        {
            LoseLevel("no_water");
        }
        else if (currentTime <= 0)
        {
            LoseLevel("time_out");
        }
    }

    void WinLevel()
    {
        gameActive = false;
        float completionTime = Time.time - levelStartTime;

        // Effects & Audio
        ParticleManager.Instance?.PlayWinEffect(waterDrop.transform.position);
        AudioManager.Instance?.PlayWin();

        // Progress tracking
        LevelProgressTracker.Instance?.CompleteLevel(completionTime, currentWater);

        uiManager.ShowWinScreen();
        Debug.Log($"Level Won in {completionTime:F1}s with {currentWater:F0}% water!");
    }

    void LoseLevel(string reason)
    {
        gameActive = false;

        AudioManager.Instance?.PlayLose();
        AnalyticsTracker.TrackLevelFail(currentLevel, reason);

        uiManager.ShowLoseScreen();
        Debug.Log($"Level Lost: {reason}");
    }

    public void AddWater(float amount)
    {
        currentWater = Mathf.Min(100f, currentWater + amount);

        // Visual feedback
        CameraController cam = FindObjectOfType<CameraController>();
        cam?.ShakeCamera();

        ParticleManager.Instance?.PlayWaterCollect(waterDrop.transform.position);
        AudioManager.Instance?.PlayWaterCollect();

        Debug.Log($"Water collected! Current: {currentWater:F0}%");
    }

    public void RestartLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public float GetWaterPercentage() => currentWater / 100f;
    public float GetTimeRemaining() => currentTime;
}