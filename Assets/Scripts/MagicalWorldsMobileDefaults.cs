using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Mobile-only defaults shared by Menu, Classic and Journey entry scenes.
/// Editor and desktop input/render settings remain unchanged.
/// </summary>
public static class MagicalWorldsMobileDefaults
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Configure()
    {
#if UNITY_ANDROID || UNITY_IOS
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.Portrait;
        SceneManager.sceneLoaded -= ConfigureTouchInput;
        SceneManager.sceneLoaded += ConfigureTouchInput;
#endif
    }

    // Resolution-based threshold reduces accidental drags on high-DPI screens.
    // Does not modify input actions or the pointer ID/drag state machine.
    public static int RecommendedDragThreshold(int width, int height)
    {
        int shorter = Mathf.Min(width, height);
        if (shorter <= 0) return 12;
        return Mathf.RoundToInt(Mathf.Clamp(shorter * 0.014f, 12f, 24f));
    }

#if UNITY_ANDROID || UNITY_IOS
    private static void ConfigureTouchInput(Scene scene, LoadSceneMode mode)
    {
        int threshold = RecommendedDragThreshold(Screen.width, Screen.height);
        EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        foreach (EventSystem system in systems)
        {
            system.pixelDragThreshold = threshold;
        }
    }
#endif
}
