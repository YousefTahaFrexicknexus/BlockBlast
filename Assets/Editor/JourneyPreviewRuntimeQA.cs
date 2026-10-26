using System;
using System.IO;
using BlockBlastClone;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class JourneyPreviewRuntimeQA
{
    private const string Pending = "MW.Journey.M11.PreviewPending.v1";
    private const string AttemptKey = "MW.Journey.Attempt.v1.50";
    private const string SelectedKey = "MW.Journey.Selected.v1";
    private const string UnlockKey = "MW.Journey.Unlocked.v1";
    private const string StarsKey = "MW.Journey.Stars.v1.50";

    static JourneyPreviewRuntimeQA()
    {
        EditorApplication.update += Monitor;
        EditorApplication.playModeStateChanged += StateChanged;
    }

    [MenuItem("Tools/Magical Worlds/Journey/Run Isolated Preview Test")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().isDirty ||
            SceneManager.GetActiveScene().path != "Assets/Scenes/JourneyMap.unity")
        {
            Debug.LogWarning("[Journey Preview QA] Open the saved JourneyMap scene in Edit Mode first.");
            return;
        }
        Preserve(AttemptKey, "attempt", true);
        Preserve(SelectedKey, "selected", false);
        Preserve(UnlockKey, "unlocked", false);
        Preserve(StarsKey, "stars", false);
        EditorPrefs.SetBool(Pending, true);
        JourneyPlaytestTelemetry.AutomatedTest = true;
        EditorPrefs.SetBool(Pending + ".checked", false);
        JourneyPlaytestContext.SetPreview(50);
        EditorSceneManager.OpenScene("Assets/Scenes/JourneyGameplay.unity");
        EditorApplication.isPlaying = true;
    }

    private static void Monitor()
    {
        if (!EditorPrefs.GetBool(Pending, false) || !EditorApplication.isPlaying ||
            EditorPrefs.GetBool(Pending + ".checked", false) ||
            SceneManager.GetActiveScene().name != "JourneyGameplay") return;

        EditorPrefs.SetBool(Pending + ".checked", true);
        try
        {
            JourneySession session = UnityEngine.Object.FindFirstObjectByType<JourneySession>();
            BlockBlastPrototype prototype = UnityEngine.Object.FindFirstObjectByType<BlockBlastPrototype>();
            Check(session != null && prototype != null && prototype.CurrentModel != null,
                "Preview scene controllers missing");
            Check(JourneyPlaytestContext.IsPreview &&
                JourneyProgress.SelectedLevel == 50 && session.Level.levelNumber == 50,
                "Preview did not select locked Level 50");
            Check(!session.HasResumedAttempt && prototype.CurrentModel.Moves == 0,
                "Preview improperly resumed a player's unfinished attempt");

            BlockBlastModel model = prototype.CurrentModel;
            bool placed = false;
            for (int slot = 0; slot < model.Hand.Length && !placed; slot++)
            {
                BlockPiece piece = model.Hand[slot];
                if (piece == null) continue;
                for (int row = 0; row <= 8 - piece.Height && !placed; row++)
                for (int col = 0; col <= 8 - piece.Width && !placed; col++)
                {
                    if (model.TryPlace(slot, row, col, out PlacementResult result))
                    {
                        session.OnPlaced(result, model);
                        placed = true;
                    }
                }
            }
            Check(placed, "No legal preview placement");
            CheckUnchanged(AttemptKey, "attempt", true);
            CheckUnchanged(SelectedKey, "selected", false);
            CheckUnchanged(UnlockKey, "unlocked", false);
            CheckUnchanged(StarsKey, "stars", false);

            string report = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                JourneyPlaytestTelemetry.RelativeReport));
            Check(File.Exists(report), "Playtest telemetry log not written");
            Debug.Log("[Journey Preview Runtime QA] PASSED: locked Level 50 playable without changing " +
                "player saves, and editor-only telemetry recorded.");
        }
        catch (Exception e)
        {
            Debug.LogError("[Journey Preview Runtime QA] FAILED: " + e);
        }
        finally
        {
            EditorApplication.isPlaying = false;
        }
    }

    private static void StateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode ||
            !EditorPrefs.GetBool(Pending, false)) return;
        JourneyPlaytestContext.Clear();
        EditorPrefs.SetBool(Pending, false);
        JourneyPlaytestTelemetry.AutomatedTest = false;
        EditorPrefs.SetBool(Pending + ".checked", false);
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/JourneyMap.unity")
            EditorSceneManager.OpenScene("Assets/Scenes/JourneyMap.unity");
    }

    private static void Preserve(string key, string label, bool isString)
    {
        EditorPrefs.SetBool(Pending + "." + label + ".exists", PlayerPrefs.HasKey(key));
        if (isString)
            EditorPrefs.SetString(Pending + "." + label + ".value", PlayerPrefs.GetString(key, ""));
        else
            EditorPrefs.SetInt(Pending + "." + label + ".value", PlayerPrefs.GetInt(key));
    }

    private static void CheckUnchanged(string key, string label, bool isString)
    {
        Check(EditorPrefs.GetBool(Pending + "." + label + ".exists") == PlayerPrefs.HasKey(key),
            "Preview changed key existence: " + key);
        if (isString)
            Check(EditorPrefs.GetString(Pending + "." + label + ".value") == PlayerPrefs.GetString(key, ""),
                "Preview changed attempt value: " + key);
        else
            Check(EditorPrefs.GetInt(Pending + "." + label + ".value") == PlayerPrefs.GetInt(key),
                "Preview changed progress value: " + key);
    }

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
}
