using System;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class JourneyDataQA
{
    static JourneyDataQA() { EditorApplication.delayCall += CheckWhenReady; }
    private static void CheckWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += CheckWhenReady;
            return;
        }
        try { Validate(); }
        catch (Exception ex) { Debug.LogError("[Journey Data QA] FAILED: " + ex); }
    }

    [MenuItem("Tools/Magical Worlds/Journey/Validate Level Data")]
    public static void Validate()
    {
        JourneyLevelCatalog catalog = JourneyLevelCatalog.Load();
        if (catalog == null || catalog.Count != 50) throw new Exception("Catalog must contain fifty levels");
        for (int i = 1; i <= 50; i++)
        {
            JourneyLevelDefinition level = catalog.Get(i);
            if (level == null || level.levelNumber != i || level.moveLimit < 1 ||
                level.IsComplete(0, 0) || !level.IsComplete(level.targetScore, level.targetLines))
                throw new Exception("Invalid goal data for level " + i);
            if (level.IsFailed(0, 0, 0, false) ||
                !level.IsFailed(0, 0, level.moveLimit, false) ||
                !level.IsFailed(0, 0, 0, true) ||
                level.IsFailed(level.targetScore, level.targetLines, level.moveLimit, true) ||
                level.IsFailed(0, 0, level.moveLimit, false, 3))
                throw new Exception("Win/loss priority or optional extra moves failed for level " + i);
        }
        GameObject map = AssetDatabase.LoadAssetAtPath<GameObject>(
            MagicalWorldsJourneyBuilder.MapPrefab);
        if (map == null) throw new Exception("Journey Map prefab missing");
        bool[] found = new bool[50];
        foreach (JourneyLevelNode node in map.GetComponentsInChildren<JourneyLevelNode>(true))
        {
            int level = node.LevelNumber;
            if (level < 1 || level > 50 || found[level - 1])
                throw new Exception("Duplicated / invalid map level node " + level);
            found[level - 1] = true;
        }
        foreach (bool value in found)
            if (!value) throw new Exception("Map node missing.");
        Debug.Log("[Journey Data QA] PASSED: goals, moves/loss limits, win priority, extra-move rules and 50 unique authored level numbers.");
    }
}
