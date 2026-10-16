using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class JourneyGameplayReview
{
    static JourneyGameplayReview() { EditorApplication.delayCall += OpenOnce; }

    private static void OpenOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += OpenOnce;
            return;
        }

        if (EditorPrefs.GetBool("MW.Journey.Polish.GameplayOpened.v1", false)) return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/JourneyMap.unity" || scene.isDirty) return;

        EditorPrefs.SetBool("MW.Journey.Polish.GameplayOpened.v1", true);
        EditorSceneManager.OpenScene("Assets/Scenes/JourneyGameplay.unity");
        Debug.Log("[Journey Gameplay Review] Opened the editable Journey gameplay scene for layout inspection.");
    }
}
