using UnityEngine;

public class LevelProgressTracker : MonoBehaviour
{
    [Header("Progress Tracking")]
    public int currentLevel = 1;
    public float bestTime = 0f;
    public int totalStars = 0;

    public static LevelProgressTracker Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CompleteLevel(float timeUsed, float waterRemaining)
    {
        // محاسبه ستاره بر اساس عملکرد
        int stars = CalculateStars(timeUsed, waterRemaining);

        // ذخیره بهترین رکورد
        string levelKey = "Level_" + currentLevel + "_BestTime";
        if (PlayerPrefs.GetFloat(levelKey, float.MaxValue) > timeUsed)
        {
            PlayerPrefs.SetFloat(levelKey, timeUsed);
            bestTime = timeUsed;
        }

        // ذخیره ستاره‌ها
        string starKey = "Level_" + currentLevel + "_Stars";
        int currentStars = PlayerPrefs.GetInt(starKey, 0);
        if (stars > currentStars)
        {
            PlayerPrefs.SetInt(starKey, stars);
            totalStars += (stars - currentStars);
        }

        SaveProgress();

        // آنالیتیکس
        AnalyticsTracker.TrackLevelComplete(currentLevel, stars, timeUsed);
    }

    int CalculateStars(float timeUsed, float waterRemaining)
    {
        // 3 ستاره: زمان کم + آب زیاد
        // 2 ستاره: متوسط
        // 1 ستاره: تکمیل شده

        int stars = 1; // حداقل یک ستاره برای تکمیل

        if (timeUsed < 25f) stars++; // سرعت خوب
        if (waterRemaining > 50f) stars++; // آب زیاد باقی‌مانده

        return Mathf.Clamp(stars, 1, 3);
    }

    void LoadProgress()
    {
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        totalStars = PlayerPrefs.GetInt("TotalStars", 0);
    }

    void SaveProgress()
    {
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.SetInt("TotalStars", totalStars);
        PlayerPrefs.Save();
    }
}