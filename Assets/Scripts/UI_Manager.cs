using UnityEngine;
using UnityEngine.UI;

public class UI_Manager : MonoBehaviour
{
    [Header("UI References")]
    public Slider waterBar;
    public Text timerText;
    public GameObject winScreen;
    public GameObject loseScreen;
    public Button restartButton;
    public Image sunIcon;
    public RectTransform sunPath;

    void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(() => FindObjectOfType<GameManager>().RestartLevel());

        winScreen?.SetActive(false);
        loseScreen?.SetActive(false);
    }

    public void UpdateWaterBar(float waterPercentage)
    {
        if (waterBar != null)
            waterBar.value = waterPercentage;
    }

    public void UpdateTimer(float timeLeft)
    {
        if (timerText != null)
            timerText.text = Mathf.Ceil(timeLeft).ToString();
    }

    public void UpdateSunPosition(float angle)
    {
        if (sunIcon != null && sunPath != null)
        {
            float normalizedAngle = (angle + 60f) / 120f; // Convert to 0-1 range
            Vector3 pathPosition = Vector3.Lerp(
                new Vector3(-sunPath.rect.width / 2, 0, 0),
                new Vector3(sunPath.rect.width / 2, 0, 0),
                normalizedAngle
            );
            sunIcon.transform.localPosition = pathPosition;
        }
    }

    public void ShowWinScreen()
    {
        winScreen?.SetActive(true);
    }

    public void ShowLoseScreen()
    {
        loseScreen?.SetActive(true);
    }
}
