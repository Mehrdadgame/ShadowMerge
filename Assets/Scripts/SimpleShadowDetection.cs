using System.Collections;
using UnityEngine;

public class SimpleShadowDetection : MonoBehaviour
{
    [Header("تنظیمات ساده")]
    public bool debugMode = true;
    public float checkDistance = 2f;

    private bool inShadow = false;
    private ShadowProjector currentShadow = null;

    void Update()
    {
        CheckShadowSimple();
    }

    void CheckShadowSimple()
    {
        bool wasInShadow = inShadow;
        inShadow = false;
        currentShadow = null;

        // روش ساده: همه ShadowProjector ها را پیدا کن و تست کن
        ShadowProjector[] allShadows = FindObjectsOfType<ShadowProjector>();

        if (debugMode)
        {
            Debug.Log($"🔍 بررسی {allShadows.Length} سایه برای Player در {transform.position}");
        }

        foreach (ShadowProjector shadow in allShadows)
        {
            if (shadow == null) continue;

            // تست فاصله اول
            float distance = Vector3.Distance(transform.position, shadow.transform.position);

            if (debugMode)
            {
                Debug.Log($"  سایه {shadow.name}: فاصله = {distance:F2}");
            }

            if (distance > checkDistance)
            {
                if (debugMode) Debug.Log($"    ❌ خیلی دور است (>{checkDistance})");
                continue;
            }

            // تست دقیق
            bool inThisShadow = shadow.IsPointInShadow(transform.position);

            if (debugMode)
            {
                Debug.Log($"    IsPointInShadow: {inThisShadow}");
            }

            if (inThisShadow)
            {
                inShadow = true;
                currentShadow = shadow;
                if (debugMode) Debug.Log($"    ✅ در این سایه هستیم!");
                break; // اولین سایه پیدا شد
            }
        }

        // Debug فقط وقتی تغییر کند
        if (wasInShadow != inShadow && debugMode)
        {
            if (inShadow)
            {
                Debug.Log($"🛡️ وارد سایه شد: {currentShadow.name}");
            }
            else
            {
                Debug.Log($"☀️ از سایه خارج شد");
            }
        }
    }

    // Public methods برای GameManager
    public bool IsInShadow() => inShadow;
    public ShadowProjector GetCurrentShadow() => currentShadow;

    void OnDrawGizmosSelected()
    {
        // نمایش وضعیت
        Gizmos.color = inShadow ? Color.blue : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // نمایش محدوده بررسی
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, checkDistance);

        // اتصال به سایه فعلی
        if (inShadow && currentShadow != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentShadow.transform.position);
        }
    }
}