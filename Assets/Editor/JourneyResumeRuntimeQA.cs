using System;
using BlockBlastClone;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class JourneyResumeRuntimeQA
{
    private const string Pending = "MW.Journey.ResumeRuntime.Pending.v1";
    private const string AttemptKey = "MW.Journey.Attempt.v1.1";
    private const string SelectedKey = "MW.Journey.Selected.v1";
    private static bool launched;
    private static double started;
    private static int expectedScore;

    static JourneyResumeRuntimeQA()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.update += CheckPendingStart;
    }

    [MenuItem("Tools/Magical Worlds/Journey/Run Resume and Restart Test")]
    public static void StartTest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != MagicalWorldsJourneyBuilder.MapScene ||
            SceneManager.GetActiveScene().isDirty)
        {
            Debug.LogWarning("[Journey Resume Runtime QA] Open saved JourneyMap in Edit Mode first.");
            return;
        }

        EditorPrefs.SetBool(Pending + ".hasAttempt", PlayerPrefs.HasKey(AttemptKey));
        EditorPrefs.SetString(Pending + ".attempt", PlayerPrefs.GetString(AttemptKey, ""));
        EditorPrefs.SetBool(Pending + ".hasSelected", PlayerPrefs.HasKey(SelectedKey));
        EditorPrefs.SetInt(Pending + ".selected", PlayerPrefs.GetInt(SelectedKey, 1));
        PlayerPrefs.DeleteKey(AttemptKey);
        PlayerPrefs.Save();

        EditorPrefs.SetBool(Pending, true);
        JourneyPlaytestTelemetry.AutomatedTest = true;
        launched = false;
        started = EditorApplication.timeSinceStartup;
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayMode(PlayModeStateChange change)
    {
        if (!EditorPrefs.GetBool(Pending, false)) return;
        if (change == PlayModeStateChange.EnteredPlayMode) CheckPendingStart();
        else if (change == PlayModeStateChange.EnteredEditMode) Finish(false);
    }

    private static void CheckPendingStart()
    {
        if (!EditorPrefs.GetBool(Pending, false) || launched ||
            !EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "JourneyMap")
            return;
        launched = true;
        Application.runInBackground = true;
        started = EditorApplication.timeSinceStartup;
        EditorApplication.delayCall += OpenFirstLevel;
    }

    private static void OpenFirstLevel()
    {
        try
        {
            JourneyMapController map = UnityEngine.Object.FindFirstObjectByType<JourneyMapController>();
            Require(map != null, "Journey Map missing");
            Require(!JourneyAttemptStore.HasAttempt(JourneyLevelCatalog.Load().Get(1)),
                "Test must begin with a fresh Level 1");
            map.Select(1);
            EditorApplication.update += WaitForFreshGameplay;
        }
        catch (Exception ex) { Fail(ex.Message); }
    }

    private static void WaitForFreshGameplay()
    {
        if (Timeout()) { Fail("Fresh Journey level load timed out"); return; }
        if (SceneManager.GetActiveScene().name != "JourneyGameplay") return;
        EditorApplication.update -= WaitForFreshGameplay;
        try
        {
            JourneySession session = UnityEngine.Object.FindFirstObjectByType<JourneySession>();
            BlockBlastPrototype classic = UnityEngine.Object.FindFirstObjectByType<BlockBlastPrototype>();
            Require(session != null && classic != null && !session.HasResumedAttempt &&
                classic.CurrentModel.Moves == 0, "Fresh level was not reset");

            BlockBlastModel model = classic.CurrentModel;
            bool placed = false;
            for (int slot = 0; slot < model.Hand.Length && !placed; slot++)
            {
                BlockPiece piece = model.Hand[slot];
                if (piece == null) continue;
                for (int row = 0; row < 8 && !placed; row++)
                for (int col = 0; col < 8 && !placed; col++)
                {
                    if (!model.CanPlace(piece, row, col)) continue;
                    Require(model.TryPlace(slot, row, col, out PlacementResult result),
                        "Legal placement failed");
                    session.OnPlaced(result, model);
                    placed = true;
                }
            }
            Require(placed && model.Moves == 1 && JourneyAttemptStore.HasAttempt(session.Level),
                "First placement was not saved");
            expectedScore = model.Score;
            Private<Button>(session, "mapButton").onClick.Invoke();
            started = EditorApplication.timeSinceStartup;
            EditorApplication.update += WaitForContinueMap;
        }
        catch (Exception ex) { Fail(ex.Message); }
    }

    private static void WaitForContinueMap()
    {
        if (Timeout()) { Fail("Returning to map timed out"); return; }
        if (SceneManager.GetActiveScene().name != "JourneyMap") return;
        JourneyLevelNode first = null;
        foreach (JourneyLevelNode node in UnityEngine.Object.FindObjectsByType<JourneyLevelNode>(FindObjectsSortMode.None))
            if (node.LevelNumber == 1) first = node;
        if (first == null) return;

        EditorApplication.update -= WaitForContinueMap;
        try
        {
            Text state = (Text)typeof(JourneyLevelNode).GetField("stateText",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).GetValue(first);
            Require(state != null && state.text == "CONTINUE",
                "The map didn't show CONTINUE for a saved attempt");
            first.GetComponent<Button>().onClick.Invoke();
            started = EditorApplication.timeSinceStartup;
            EditorApplication.update += WaitForResumedGameplay;
        }
        catch (Exception ex) { Fail(ex.Message); }
    }

    private static void WaitForResumedGameplay()
    {
        if (Timeout()) { Fail("Resume load timed out"); return; }
        if (SceneManager.GetActiveScene().name != "JourneyGameplay") return;
        JourneySession session = UnityEngine.Object.FindFirstObjectByType<JourneySession>();
        BlockBlastPrototype classic = UnityEngine.Object.FindFirstObjectByType<BlockBlastPrototype>();
        if (session == null || classic == null) return;

        EditorApplication.update -= WaitForResumedGameplay;
        try
        {
            Require(session.HasResumedAttempt && classic.CurrentModel.Moves == 1 &&
                classic.CurrentModel.Score == expectedScore,
                "Resuming did not restore move count and score");
            BlockBlastUIReferences view = classic.GetComponentInChildren<BlockBlastUIReferences>(true);
            Require(view != null, "Pause UI references missing");
            view.PauseButton.onClick.Invoke();
            view.DialogTertiary.onClick.Invoke();
            Require(view.DialogTitle.text == "RESTART LEVEL?",
                "Journey restart confirmation has the wrong heading");
            view.DialogPrimary.onClick.Invoke();

            started = EditorApplication.timeSinceStartup;
            EditorApplication.update += WaitForRestart;
        }
        catch (Exception ex) { Fail(ex.Message); }
    }

    private static void WaitForRestart()
    {
        if (Timeout()) { Fail("Restart load timed out"); return; }
        if (SceneManager.GetActiveScene().name != "JourneyGameplay") return;
        JourneySession session = UnityEngine.Object.FindFirstObjectByType<JourneySession>();
        BlockBlastPrototype classic = UnityEngine.Object.FindFirstObjectByType<BlockBlastPrototype>();
        if (session == null || classic == null) return;
        EditorApplication.update -= WaitForRestart;
        try
        {
            Require(!session.HasResumedAttempt && classic.CurrentModel.Moves == 0 &&
                !JourneyAttemptStore.HasAttempt(session.Level),
                "Restart kept stale attempt data");
            session.ReturnToMap();
            Debug.Log("[Journey Resume Runtime QA] PASSED: actual map CONTINUE button, " +
                "restored score/moves, Pause restart confirmation, fresh board and cleared attempt.");
            Finish(true);
        }
        catch (Exception ex) { Fail(ex.Message); }
    }

    private static T Private<T>(JourneySession session, string name)
    {
        return (T)typeof(JourneySession).GetField(name,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic).GetValue(session);
    }

    private static bool Timeout()
    {
        return EditorApplication.timeSinceStartup - started > 20.0;
    }

    private static void Fail(string message)
    {
        Debug.LogError("[Journey Resume Runtime QA] FAILED: " + message);
        Finish(true);
    }

    private static void Finish(bool stopPlay)
    {
        if (!EditorPrefs.GetBool(Pending, false)) return;
        EditorApplication.update -= WaitForFreshGameplay;
        EditorApplication.update -= WaitForContinueMap;
        EditorApplication.update -= WaitForResumedGameplay;
        EditorApplication.update -= WaitForRestart;

        if (EditorPrefs.GetBool(Pending + ".hasAttempt", false))
            PlayerPrefs.SetString(AttemptKey, EditorPrefs.GetString(Pending + ".attempt", ""));
        else PlayerPrefs.DeleteKey(AttemptKey);

        if (EditorPrefs.GetBool(Pending + ".hasSelected", false))
            PlayerPrefs.SetInt(SelectedKey, EditorPrefs.GetInt(Pending + ".selected", 1));
        else PlayerPrefs.DeleteKey(SelectedKey);
        PlayerPrefs.Save();

        EditorPrefs.SetBool(Pending, false);
        JourneyPlaytestTelemetry.AutomatedTest = false;
        launched = false;
        if (stopPlay && EditorApplication.isPlaying) EditorApplication.isPlaying = false;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
