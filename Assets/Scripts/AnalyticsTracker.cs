using UnityEngine;

public class AnalyticsTracker : MonoBehaviour
{
    public static void TrackLevelStart(int level)
    {
        Debug.Log($"Analytics: Level {level} Started");
        // Integration with Voodoo SDK
        // VoodooAnalytics.TrackEvent("level_start", new Dictionary<string, object> { {"level", level} });
    }

    public static void TrackLevelComplete(int level, int stars, float time)
    {
        Debug.Log($"Analytics: Level {level} Complete - {stars} stars in {time:F1}s");
        // VoodooAnalytics.TrackEvent("level_complete", new Dictionary<string, object> 
        // { 
        //     {"level", level}, 
        //     {"stars", stars}, 
        //     {"time", time} 
        // });
    }

    public static void TrackLevelFail(int level, string reason)
    {
        Debug.Log($"Analytics: Level {level} Failed - {reason}");
        // VoodooAnalytics.TrackEvent("level_fail", new Dictionary<string, object> 
        // { 
        //     {"level", level}, 
        //     {"reason", reason} 
        // });
    }

    public static void TrackShadowMerge(int level, int mergeCount)
    {
        Debug.Log($"Analytics: Shadow merge #{mergeCount} in level {level}");
        // Custom event for unique mechanic
    }
}
