using System.Collections;
using UnityEngine;

// اسکریپت موقت برای تشخیص مشکل
public class DebugShadowDetection : MonoBehaviour
{
    [Header("تست تنظیمات")]
    public LayerMask shadowLayerMask = -1; // همه Layer ها
    public float checkRadius = 1f;

    void Update()
    {
        TestAllMethods();
    }

    void TestAllMethods()
    {
        Vector3 pos = transform.position;

        Debug.Log("=== SHADOW DETECTION DEBUG ===");

        // روش 1: Physics.OverlapSphere
        Collider[] colliders = Physics.OverlapSphere(pos, checkRadius, shadowLayerMask);
        Debug.Log($"روش 1 - OverlapSphere: {colliders.Length} collider پیدا شد");

        for (int i = 0; i < colliders.Length; i++)
        {
            Debug.Log($"  - Collider {i}: {colliders[i].name} (Layer: {colliders[i].gameObject.layer})");

            ShadowProjector shadow = colliders[i].GetComponentInParent<ShadowProjector>();
            if (shadow != null)
            {
                bool inShadow = shadow.IsPointInShadow(pos);
                Debug.Log($"    -> ShadowProjector found! IsPointInShadow: {inShadow}");
            }
        }

        // روش 2: مستقیم از همه ShadowProjector ها
        ShadowProjector[] allShadows = FindObjectsOfType<ShadowProjector>();
        Debug.Log($"روش 2 - FindObjectsOfType: {allShadows.Length} سایه پیدا شد");

        for (int i = 0; i < allShadows.Length; i++)
        {
            bool inShadow = allShadows[i].IsPointInShadow(pos);
            float distance = Vector3.Distance(pos, allShadows[i].transform.position);
            Debug.Log($"  - Shadow {i}: {allShadows[i].name} - InShadow: {inShadow} - Distance: {distance:F1}");
        }

        // روش 3: تست Manual
        TestManualDetection();
    }

    void TestManualDetection()
    {
        // تست کردن تشخیص دستی سایه
        Collider[] allColliders = FindObjectsOfType<Collider>();
        int shadowColliderCount = 0;

        foreach (var col in allColliders)
        {
            if (col.gameObject.layer == 8) // Shadow layer
            {
                shadowColliderCount++;
                Debug.Log($"Shadow Collider: {col.name} - isTrigger: {col.isTrigger} - bounds: {col.bounds}");

                if (col.bounds.Contains(transform.position))
                {
                    Debug.Log($"  ✅ Player داخل bounds این collider است!");
                }
            }
        }

        Debug.Log($"تعداد کل Shadow Collider ها: {shadowColliderCount}");
    }

    void OnDrawGizmos()
    {
        // نمایش محدوده تشخیص
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, checkRadius);

        // نمایش همه Shadow Collider ها
        Collider[] shadowColliders = FindObjectsOfType<Collider>();
        foreach (var col in shadowColliders)
        {
            if (col.gameObject.layer == 8) // Shadow layer
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}