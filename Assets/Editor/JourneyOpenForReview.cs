using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class JourneyOpenForReview
{
    static JourneyOpenForReview()
    {
        EditorApplication.delayCall += OpenOnce;
    }

    private static void OpenOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += OpenOnce;
            return;
        }
        if (EditorPrefs.GetBool("MW.Journey.MapFirstOpened.v1", false)) return;
        Scene current = SceneManager.GetActiveScene();
        if (current.path != "Assets/Scenes/MainMenu.unity" || current.isDirty) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MagicalWorldsJourneyBuilder.MapScene) == null) return;
        EditorPrefs.SetBool("MW.Journey.MapFirstOpened.v1", true);
        EditorSceneManager.OpenScene(MagicalWorldsJourneyBuilder.MapScene);
        Debug.Log("[Journey] Opened editable Journey Map scene for visual review.");
    }
}
