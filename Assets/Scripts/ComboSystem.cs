using UnityEngine;
using System.Collections;

public class ComboSystem : MonoBehaviour
{
    [Header("Combo Settings")]
    public float comboWindow = 5f; // زمان برای حفظ کومبو
    public float shadowJumpBonus = 10f; // امتیاز پرش بین سایه‌ها
    public float mergeBonus = 25f; // امتیاز ادغام سایه‌ها

    [Header("Visual Effects")]
    public Color[] comboColors = { Color.white, Color.yellow, Color.orange, Color.red };
    public ParticleSystem comboEffect;

    private int currentCombo = 0;
    private float lastComboTime = 0f;
    private bool showingCombo = false;

    public static ComboSystem Instance { get; private set; }

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

    void Update()
    {
        // کاهش کومبو اگر زمان گذشته
        if (Time.time - lastComboTime > comboWindow && currentCombo > 0)
        {
            ResetCombo();
        }
    }

    public void AddShadowJump()
    {
        currentCombo++;
        lastComboTime = Time.time;

        // محاسبه امتیاز بونوس
        float bonusWater = shadowJumpBonus + (currentCombo * 2f);
        GameManager.Instance?.AddWater(bonusWater);

        // افکت بصری
        ShowComboEffect();

        // صدا
        AudioManager.Instance?.PlayShadowMerge();

        Debug.Log($"🔥 Shadow Jump Combo x{currentCombo}! +{bonusWater} آب");
    }

    public void AddShadowMerge()
    {
        currentCombo += 2; // ادغام سایه ارزش بیشتری داره
        lastComboTime = Time.time;

        float bonusWater = mergeBonus + (currentCombo * 3f);
        GameManager.Instance?.AddWater(bonusWater);

        ShowComboEffect();
        AudioManager.Instance?.PlayWin(); // صدای خاص‌تر

        Debug.Log($"⚡ Shadow Merge Combo x{currentCombo}! +{bonusWater} آب");
    }

    void ShowComboEffect()
    {
        if (showingCombo) return;

        StartCoroutine(ComboVisualEffect());
    }

    IEnumerator ComboVisualEffect()
    {
        showingCombo = true;

        // انتخاب رنگ بر اساس سطح کومبو
        int colorIndex = Mathf.Min(currentCombo - 1, comboColors.Length - 1);
        Color comboColor = comboColors[colorIndex];

        // نمایش متن کومبو
        UI_Manager uiManager = FindObjectOfType<UI_Manager>();
        if (uiManager != null)
        {
            string comboText = $"COMBO x{currentCombo}!";
            if (currentCombo >= 5)
                comboText = $"🔥 AMAZING x{currentCombo}! 🔥";
            else if (currentCombo >= 3)
                comboText = $"⚡ GREAT x{currentCombo}! ⚡";

            uiManager.ShowTutorialHint(comboText, 1.5f);
        }

        // پارتیکل
        if (comboEffect != null && GameManager.Instance?.waterDrop != null)
        {
            ParticleSystem effect = Instantiate(comboEffect,
                GameManager.Instance.waterDrop.transform.position + Vector3.up,
                Quaternion.identity);

            var main = effect.main;
            main.startColor = comboColor;

            Destroy(effect.gameObject, 2f);
        }

        // لرزش دوربین
        CameraController cam = FindObjectOfType<CameraController>();
        if (cam != null)
        {
            for (int i = 0; i < currentCombo && i < 5; i++)
            {
                cam.ShakeCamera();
                yield return new WaitForSeconds(0.1f);
            }
        }

        showingCombo = false;
    }

    void ResetCombo()
    {
        if (currentCombo > 0)
        {
            Debug.Log($"💔 Combo Reset! تمام شد: {currentCombo}");
            currentCombo = 0;
        }
    }

    public int GetCurrentCombo() => currentCombo;
    public float GetComboMultiplier() => 1f + (currentCombo * 0.1f);
}