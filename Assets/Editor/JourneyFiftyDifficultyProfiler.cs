using System;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class JourneyFiftyDifficultyProfiler
{
    const string Once = "MW.Journey.FiftyBalanceProfiler.v2";
    const int Trials = 8;
    static JourneyLevelCatalog catalog;
    static int next;

    static JourneyFiftyDifficultyProfiler() { EditorApplication.delayCall += StartOnce; }

    static void StartOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += StartOnce;
            return;
        }
        if (EditorPrefs.GetBool(Once, false)) return;
        catalog = JourneyLevelCatalog.Load();
        if (catalog == null || catalog.Count != 50) return;
        EditorPrefs.SetBool(Once, true);
        next = 1;
        EditorApplication.update += Step;
        Debug.Log("[Journey 50 Balance] Starting deterministic 50-level tests.");
    }

    [MenuItem("Tools/Magical Worlds/Journey/Profile All 50 Levels")]
    public static void StartManually()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        catalog = JourneyLevelCatalog.Load();
        if (catalog == null || catalog.Count != 50) return;
        next = 1;
        EditorApplication.update -= Step;
        EditorApplication.update += Step;
    }

    static void Step()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.update -= Step;
            return;
        }
        if (next > 50)
        {
            EditorApplication.update -= Step;
            Debug.Log("[Journey 50 Balance] FINISHED profiling all 50 levels.");
            return;
        }

        try
        {
            int number = next++;
            JourneyLevelDefinition level = catalog.Get(number);
            int randomWins = 0, strategicWins = 0, strategicMoves = 0;
            for (int trial = 0; trial < Trials; trial++)
            {
                int used;
                if (JourneyBalanceQA.TryFinish(level, number * 10000 + trial,
                    false, out used)) randomWins++;
                if (JourneyBalanceQA.TryFinish(level, number * 10000 + trial,
                    true, out used))
                {
                    strategicWins++;
                    strategicMoves += used;
                }
            }
            Debug.Log("[Journey 50 Balance] LEVEL=" + number +
                " moves=" + level.moveLimit +
                " random=" + randomWins + "/" + Trials +
                " strategic=" + strategicWins + "/" + Trials +
                " avgMoves=" +
                (strategicWins > 0 ? (strategicMoves / (float)strategicWins).ToString("F1") : "n/a"));
            if (strategicWins == 0)
                Debug.LogWarning("[Journey 50 Balance] Potentially unreachable Level " + number);
        }
        catch (Exception e)
        {
            EditorApplication.update -= Step;
            Debug.LogError("[Journey 50 Balance] FAILED: " + e);
        }
    }
}
