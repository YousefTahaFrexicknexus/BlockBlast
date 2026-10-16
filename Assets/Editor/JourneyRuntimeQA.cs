using System;
using BlockBlastClone;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class JourneyRuntimeQA
{
    private const string Pending = "MW.Journey.Runtime.Pending.v10";
    private static string[] keys = {
        "MW.Journey.Selected.v1", "MW.Journey.Unlocked.v1", "MW.Journey.Stars.v1.1", "MW.Journey.Stars.v1.2"
    };
    private static int[] previous = new int[4];
    private static bool[] existed = new bool[4];
    private static bool running;
    private static bool resumed;
    private static double started;

    static JourneyRuntimeQA()
    {
        EditorApplication.playModeStateChanged += StateChanged;

        EditorApplication.update += ResumePending;
    }

    private static void ResumePending()
    {
        if (resumed || !EditorPrefs.GetBool(Pending, false) ||
            !EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "JourneyMap")
            return;
        resumed = true;
        running = true;
        started = EditorApplication.timeSinceStartup;
        Application.runInBackground = true;
        EditorApplication.delayCall += OpenLevel;
    }
    [MenuItem("Tools/Magical Worlds/Journey/Run Gameplay Smoke Test %&j")]
    public static void RunManually()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != MagicalWorldsJourneyBuilder.MapScene ||
            SceneManager.GetActiveScene().isDirty)
        {
            Debug.LogWarning("[Journey Runtime QA] Stop Play Mode and open the saved JourneyMap scene first.");
            return;
        }
        BeginOnce();
    }
    private static void BeginOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += BeginOnce;
            return;
        }
        for (int i = 0; i < keys.Length; i++)
        {
            existed[i] = PlayerPrefs.HasKey(keys[i]);
            previous[i] = PlayerPrefs.GetInt(keys[i]);
            EditorPrefs.SetBool(Pending + ".exists." + i, existed[i]);
            EditorPrefs.SetInt(Pending + ".value." + i, previous[i]);
        }
        // Preserve live attempts while smoke tests manipulate Level 1 and Level 2.
        for (int levelNumber = 1; levelNumber <= 2; levelNumber++)
        {
            string key = "MW.Journey.Attempt.v1." + levelNumber;
            EditorPrefs.SetBool(Pending + ".attemptExists." + levelNumber, PlayerPrefs.HasKey(key));
            EditorPrefs.SetString(Pending + ".attemptValue." + levelNumber, PlayerPrefs.GetString(key, ""));
            PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.DeleteKey("MW.Journey.Stars.v1.2");
        PlayerPrefs.Save();
        EditorPrefs.SetBool(Pending, true);
        running = true;
        started = EditorApplication.timeSinceStartup;
        if (EditorApplication.isPlaying) EditorApplication.delayCall += OpenLevel;
        else EditorApplication.isPlaying = true;
    }

    private static void StateChanged(PlayModeStateChange state)
    {
        if (!EditorPrefs.GetBool(Pending, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode && !resumed)
        {
            resumed = true;
            running = true;
            started = EditorApplication.timeSinceStartup;
            EditorApplication.delayCall += OpenLevel;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= WaitForGameplay;
            EditorApplication.update -= WaitForResult;
            EditorApplication.update -= WaitForLevelTwo;
            EditorApplication.update -= WaitForLoss;
            EditorApplication.update -= WaitForRetry;
            Restore();
            EditorPrefs.SetBool(Pending, false);
            running = false;
        }
    }
    private static void OpenLevel()
    {
        JourneyMapController map = UnityEngine.Object.FindFirstObjectByType<JourneyMapController>();
        if (map == null) { Fail("JourneyMap has no controller"); return; }
        Application.runInBackground = true;
        map.ShowPage(8);
        int lastVisible = 0;
        foreach (JourneyLevelNode n in map.GetComponentsInChildren<JourneyLevelNode>(true))
            if (n.gameObject.activeSelf) lastVisible++;
        if (map.TotalPages != 9 || lastVisible != 2)
        {
            Fail("Page nine should display only levels 49 and 50");
            return;
        }
        map.ShowPage(0);
        Debug.Log("[Journey Runtime QA] Paged map 1/9 and 9/9 navigation passed.");
        JourneyLevelNode first = null;
        foreach (JourneyLevelNode node in map.GetComponentsInChildren<JourneyLevelNode>(true))
        {
            if (node.LevelNumber == 1) first = node;
        }
        if (first == null || !first.GetComponent<UnityEngine.UI.Button>().interactable)
        {
            Fail("Level 1 button missing or locked");
            return;
        }
        first.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        EditorApplication.update += WaitForGameplay;
    }

    private static void WaitForGameplay()
    {
        if (Timeout()) { Fail("JourneyGameplay load timed out"); return; }
        if (SceneManager.GetActiveScene().name != "JourneyGameplay") return;
        EditorApplication.update -= WaitForGameplay;
        try
        {
            JourneySession session = UnityEngine.Object.FindFirstObjectByType<JourneySession>();
            BlockBlastPrototype classic = UnityEngine.Object.FindFirstObjectByType<BlockBlastPrototype>();
            if (session == null || classic == null || session.Level == null ||
                session.Level.levelNumber != 1) throw new Exception("Level 1 session missing");
            BlockBlastUIReferences view = classic.GetComponentInChildren<BlockBlastUIReferences>(true);
            if (view == null || !view.IsComplete) throw new Exception("Shared UI references missing.");
            view.PauseButton.onClick.Invoke();
            GameObject header = Private<GameObject>(session, "hudRoot");
            if (header == null || header.activeSelf || !view.DialogPanel.activeSelf)
                throw new Exception("Journey HUD was not hidden beneath Pause.");
            view.DialogPrimary.onClick.Invoke();
            if (!header.activeSelf || view.DialogPanel.activeSelf)
                throw new Exception("Continuing from Pause failed to restore Journey HUD.");
            Debug.Log("[Journey Runtime QA] Pause/Continue header visibility passed.");            BlockBlastModel model = classic.CurrentModel;
            int placed = 0;
            while (!session.Level.IsComplete(model.Score, 0) && placed < session.Level.moveLimit)
            {
                bool found = false;
                for (int slot = 0; slot < model.Hand.Length && !found; slot++)
                {
                    BlockPiece piece = model.Hand[slot];
                    if (piece == null) continue;
                    for (int row = 0; row < 8 && !found; row++)
                    for (int col = 0; col < 8 && !found; col++)
                    {
                        if (!model.CanPlace(piece, row, col)) continue;
                        if (!model.TryPlace(slot, row, col, out PlacementResult result))
                            throw new Exception("Model rejected legal Journey placement");
                        session.OnPlaced(result, model);
                        placed++;
                        found = true;
                    }
                }
                if (!found) throw new Exception("No legal placements in test");
            }
            if (JourneyProgress.Stars(1) < 1 || JourneyProgress.HighestUnlocked < 2)
                throw new Exception("Victory failed to award stars and unlock Level 2");
            Debug.Log("[Journey Runtime QA] Level 1 objective, moves, unlock and stars passed.");
            started = EditorApplication.timeSinceStartup;
            EditorApplication.update += WaitForResult;
        }
        catch (Exception ex) { Fail(ex.Message); }
    }

    private static void WaitForResult()
    {
        if (Timeout()) { Fail("Result overlay timed out"); return; }
        JourneySession session = UnityEngine.Object.FindFirstObjectByType<JourneySession>();
        if (session == null) return;
        var property = typeof(JourneySession).GetField("resultPanel",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        GameObject overlay = (GameObject)property.GetValue(session);
        if (overlay == null || !overlay.activeSelf) return;
        EditorApplication.update -= WaitForResult;
        Private<UnityEngine.UI.Button>(session, "nextButton").onClick.Invoke();
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update += WaitForLevelTwo;
    }

    private static void WaitForLevelTwo()
    {
        if (Timeout()) { Fail("Loading Level 2 timed out"); return; }
        if (SceneManager.GetActiveScene().name != "JourneyGameplay") return;
        JourneySession session = UnityEngine.Object.FindFirstObjectByType<JourneySession>();
        if (session == null || session.Level == null || session.Level.levelNumber != 2) return;
        EditorApplication.update -= WaitForLevelTwo;
        BlockBlastPrototype classic = UnityEngine.Object.FindFirstObjectByType<BlockBlastPrototype>();
        if (classic == null) { Fail("Classic engine missing in Level 2"); return; }
        session.OnPlaced(new PlacementResult { GameOver = true }, classic.CurrentModel);
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update += WaitForLoss;
    }

    private static void WaitForLoss()
    {
        if (Timeout()) { Fail("Out-of-space result timed out"); return; }
        JourneySession session = UnityEngine.Object.FindFirstObjectByType<JourneySession>();
        if (session == null) return;
        GameObject overlay = Private<GameObject>(session, "resultPanel");
        if (overlay == null || !overlay.activeSelf) return;
        EditorApplication.update -= WaitForLoss;
        Debug.Log("[Journey Runtime QA] Level2 loss assertion values: title=" +
            Private<UnityEngine.UI.Text>(session, "resultTitle").text +
            ", nextActive=" + Private<UnityEngine.UI.Button>(session, "nextButton").gameObject.activeSelf +
            ", rewardInteractable=" + Private<UnityEngine.UI.Button>(session, "rewardedContinueButton").interactable +
            ", Level2Stars=" + JourneyProgress.Stars(2));        if (Private<UnityEngine.UI.Text>(session, "resultTitle").text != "NO SPACE LEFT" ||
            Private<UnityEngine.UI.Button>(session, "nextButton").gameObject.activeSelf ||
            Private<UnityEngine.UI.Button>(session, "rewardedContinueButton").interactable ||
            JourneyProgress.Stars(2) != 0)
        {
            Fail("Loss overlay, lock state or disabled reward button is incorrect");
            return;
        }
        Private<UnityEngine.UI.Button>(session, "retryButton").onClick.Invoke();
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update += WaitForRetry;
    }

    private static void WaitForRetry()
    {
        if (Timeout()) { Fail("Retry Level 2 timed out"); return; }
        if (SceneManager.GetActiveScene().name != "JourneyGameplay") return;
        JourneySession session = UnityEngine.Object.FindFirstObjectByType<JourneySession>();
        if (session == null || session.Level == null || session.Level.levelNumber != 2) return;
        EditorApplication.update -= WaitForRetry;
        Private<UnityEngine.UI.Button>(session, "returnButton").onClick.Invoke();
        Debug.Log("[Journey Runtime QA] PASSED: Level 1 victory/stars/unlock, Level 2 loss, " +
            "disabled rewarded continuation and prefab button callbacks for retry/return; progress restored.");
        Finish();
    }

    private static T Private<T>(JourneySession session, string field)
    {
        return (T)typeof(JourneySession).GetField(field,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic).GetValue(session);
    }
    private static bool Timeout() => EditorApplication.timeSinceStartup - started > 20.0;

    private static void Restore()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if (EditorPrefs.GetBool(Pending + ".exists." + i, false))
                PlayerPrefs.SetInt(keys[i], EditorPrefs.GetInt(Pending + ".value." + i));
            else PlayerPrefs.DeleteKey(keys[i]);
        }
        for (int levelNumber = 1; levelNumber <= 2; levelNumber++)
        {
            string key = "MW.Journey.Attempt.v1." + levelNumber;
            if (EditorPrefs.GetBool(Pending + ".attemptExists." + levelNumber, false))
                PlayerPrefs.SetString(key, EditorPrefs.GetString(Pending + ".attemptValue." + levelNumber, ""));
            else PlayerPrefs.DeleteKey(key);
        }        PlayerPrefs.Save();
    }
    private static void Fail(string reason)
    {
        Debug.LogError("[Journey Runtime QA] FAILED: " + reason);
        Finish();
    }

    private static void Finish()
    {
        EditorApplication.update -= WaitForGameplay;
        EditorApplication.update -= WaitForResult;
        EditorApplication.update -= WaitForLevelTwo;
        EditorApplication.update -= WaitForLoss;
        EditorApplication.update -= WaitForRetry;
        Restore();
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
    }
}
