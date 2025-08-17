using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    [Header("Countdown UI")]
    public GameObject countdownPanel;
    public Text countdownText;

    // [Header("Visual Effects")]
    // public Animator waterBarAnimator;
    // public ParticleSystem uiParticles;

    private Coroutine countdownCoroutine;

    void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(() => FindObjectOfType<GameManager>().RestartLevel());

        winScreen?.SetActive(false);
        loseScreen?.SetActive(false);

        // ایجاد UI های اضافی اگر وجود ندارند
        CreateCountdownUI();
    }

    void CreateCountdownUI()
    {
        if (countdownPanel == null)
        {
            // ایجاد پنل شمارش معکوس
            GameObject canvas = GetComponentInParent<Canvas>().gameObject;

            countdownPanel = new GameObject("CountdownPanel");
            countdownPanel.transform.SetParent(canvas.transform, false);

            Image panelImage = countdownPanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.5f);

            RectTransform panelRect = countdownPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // متن شمارش معکوس
            GameObject countdownTextObj = new GameObject("CountdownText");
            countdownTextObj.transform.SetParent(countdownPanel.transform, false);

            countdownText = countdownTextObj.AddComponent<Text>();
            countdownText.text = "3";
            countdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countdownText.fontSize = 120;
            countdownText.color = Color.white;
            countdownText.alignment = TextAnchor.MiddleCenter;

            // سایه
            Shadow shadow = countdownTextObj.AddComponent<Shadow>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = new Vector2(3, -3);

            RectTransform textRect = countdownTextObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            countdownPanel.SetActive(false);
        }
    }

    public void UpdateWaterBar(float waterPercentage)
    {
        if (waterBar != null)
        {
            waterBar.value = Mathf.Lerp(waterBar.value, waterPercentage, Time.deltaTime * 5f);

            // تغییر رنگ بر اساس میزان آب
            Image fillImage = waterBar.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                Color waterColor = Color.Lerp(Color.red, Color.cyan, waterPercentage);
                fillImage.color = waterColor;
            }

            // انیمیشن هشدار وقتی آب کم است
            // if (waterPercentage < 0.3f && waterBarAnimator != null)
            // {
            //     waterBarAnimator.SetBool("LowWater", true);
            // }
            // else if (waterBarAnimator != null)
            // {
            //     waterBarAnimator.SetBool("LowWater", false);
            // }
        }
    }

    public void UpdateTimer(float timeLeft)
    {
        if (timerText != null)
        {
            timerText.text = Mathf.Ceil(timeLeft).ToString();

            // تغییر رنگ وقتی زمان کم است
            if (timeLeft < 10f)
            {
                timerText.color = Color.Lerp(Color.red, Color.white,
                    Mathf.PingPong(Time.time * 2f, 1f));
            }
            else
            {
                timerText.color = Color.white;
            }
        }
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

            // چرخش آیکون خورشید
            sunIcon.transform.Rotate(0, 0, 30f * Time.deltaTime);
        }
    }

    public void ShowCountdown(string text)
    {
        if (countdownPanel != null && countdownText != null)
        {
            countdownPanel.SetActive(true);
            countdownText.text = text;

            // انیمیشن متن
            if (countdownCoroutine != null)
                StopCoroutine(countdownCoroutine);

            countdownCoroutine = StartCoroutine(CountdownAnimation());
        }
    }

    IEnumerator CountdownAnimation()
    {
        if (countdownText == null) yield break;

        // انیمیشن بزرگ شدن
        countdownText.transform.localScale = Vector3.zero;
        float time = 0f;

        while (time < 0.5f)
        {
            time += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 1.2f, time / 0.5f);
            countdownText.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        // برگشت به سایز عادی
        time = 0f;
        while (time < 0.3f)
        {
            time += Time.deltaTime;
            float scale = Mathf.Lerp(1.2f, 1f, time / 0.3f);
            countdownText.transform.localScale = Vector3.one * scale;
            yield return null;
        }
    }

    public void HideCountdown()
    {
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }
    }

    public void ShowWinScreen()
    {
        winScreen?.SetActive(true);

        // // افکت پارتیکل برد
        // if (uiParticles != null)
        // {
        //     uiParticles.Play();
        // }
    }

    public void ShowLoseScreen()
    {
        loseScreen?.SetActive(true);
    }

    // نمایش راهنمای بازی (ایده جدید)
    public void ShowTutorialHint(string hint, float duration = 3f)
    {
        StartCoroutine(ShowHintCoroutine(hint, duration));
    }

    IEnumerator ShowHintCoroutine(string hint, float duration)
    {
        GameObject hintObj = new GameObject("TutorialHint");
        hintObj.transform.SetParent(transform, false);

        Text hintText = hintObj.AddComponent<Text>();
        hintText.text = hint;
        hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintText.fontSize = 28;
        hintText.color = new Color(1f, 1f, 0.3f, 0.9f); // زرد روشن
        hintText.alignment = TextAnchor.MiddleCenter;

        RectTransform hintRect = hintObj.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.1f, 0.7f);
        hintRect.anchorMax = new Vector2(0.9f, 0.8f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;

        // انیمیشن ظاهر شدن
        hintText.color = new Color(1f, 1f, 0.3f, 0f);
        float fadeTime = 0f;
        while (fadeTime < 0.5f)
        {
            fadeTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 0.9f, fadeTime / 0.5f);
            hintText.color = new Color(1f, 1f, 0.3f, alpha);
            yield return null;
        }

        yield return new WaitForSeconds(duration);

        // انیمیشن ناپدید شدن
        fadeTime = 0f;
        while (fadeTime < 0.5f)
        {
            fadeTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0.9f, 0f, fadeTime / 0.5f);
            hintText.color = new Color(1f, 1f, 0.3f, alpha);
            yield return null;
        }

        Destroy(hintObj);
    }

    void Update()
    {
        // نمایش راهنمای اولیه
        if (GameManager.Instance != null && !GameManager.Instance.IsGameStarted())
        {
            if (Time.time > 1f && Time.time < 1.1f) // یک بار در ثانیه اول
            {
                ShowTutorialHint("🌞 خورشید را حرکت دهید تا سایه تغییر کند", 2f);
            }
        }
    }
}