using UnityEngine;

/// <summary>
/// Utility class for providing lighter, more controlled haptic feedback on mobile devices
/// </summary>
public static class HapticFeedback
{
    private static bool isInitialized = false;
    
#if UNITY_ANDROID
    private static AndroidJavaObject vibrator;
#endif

    /// <summary>
    /// Trigger a light haptic feedback
    /// </summary>
    public static void Light()
    {
        Trigger(10);
    }

    /// <summary>
    /// Trigger a medium haptic feedback
    /// </summary>
    public static void Medium()
    {
        Trigger(20);
    }

    /// <summary>
    /// Trigger a heavy haptic feedback
    /// </summary>
    public static void Heavy()
    {
        Trigger(40);
    }

    /// <summary>
    /// Trigger haptic feedback with custom duration in milliseconds
    /// </summary>
    public static void Trigger(long durationMs = 15)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        InitializeAndroid();
        if (vibrator != null)
        {
            try
            {
                vibrator.Call("vibrate", durationMs);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to trigger Android haptic: {e.Message}");
            }
        }
#elif UNITY_IOS && !UNITY_EDITOR
        // iOS uses a fixed-duration haptic with Handheld.Vibrate()
        // For more control, you would need to use iOS-specific plugins
        Handheld.Vibrate();
#endif
    }

#if UNITY_ANDROID
    private static void InitializeAndroid()
    {
        if (isInitialized) return;
        
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    isInitialized = true;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to initialize Android vibrator: {e.Message}");
            isInitialized = true; // Mark as initialized to avoid repeated attempts
        }
    }
#endif
}
