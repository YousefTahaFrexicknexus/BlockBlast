using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class JourneyFiftyBalanceRefinement
{
    static JourneyFiftyBalanceRefinement() { EditorApplication.delayCall += Apply; }

    [MenuItem("Tools/Magical Worlds/Journey/Apply Calibrated 50-Level Difficulty")]
    public static void Apply()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += Apply;
            return;
        }
        JourneyLevelCatalog catalog = JourneyLevelCatalog.Load();
        if (catalog == null || catalog.Count != 50) return;
        bool changed = false;
        changed |= Update(catalog, 11,305,360,4,JourneyGoalType.ScoreAndLines,
            306,360,4,JourneyGoalType.ScoreAndLines);
        changed |= Update(catalog, 12,334,385,4,JourneyGoalType.ScoreAndLines,
            335,560,5,JourneyGoalType.ScoreAndLines);
        changed |= Update(catalog, 17,479,520,5,JourneyGoalType.ScoreAndLines,
            492,520,5,JourneyGoalType.ScoreAndLines);
        changed |= Update(catalog, 24,682,710,7,JourneyGoalType.ScoreAndLines,
            729,710,7,JourneyGoalType.ScoreAndLines);
        changed |= Update(catalog, 34,972,980,9,JourneyGoalType.ScoreAndLines,
            973,980,9,JourneyGoalType.ScoreAndLines);
        changed |= Update(catalog, 41,1175,1170,10,JourneyGoalType.ScoreAndLines,
            1182,1170,10,JourneyGoalType.ScoreAndLines);
        changed |= Update(catalog, 45,1291,1670,0,JourneyGoalType.ReachScore,
            1291,3000,0,JourneyGoalType.ReachScore);
        changed |= Update(catalog, 48,1378,1,11,JourneyGoalType.ClearLines,
            1409,1,11,JourneyGoalType.ClearLines);
        changed |= Update(catalog, 50,1436,1855,0,JourneyGoalType.ReachScore,
            1436,2650,11,JourneyGoalType.ScoreAndLines);
        if (changed)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("[Journey 50 Refinement] Calibrated nine uneven levels, with original six untouched.");
        }
    }

    static bool Update(JourneyLevelCatalog catalog, int number,
        int originalSeed, int originalScore, int originalLines, JourneyGoalType originalGoal,
        int seed, int score, int lines, JourneyGoalType goal)
    {
        JourneyLevelDefinition level = catalog.Get(number);
        if (level == null) return false;
        if (level.randomSeed != originalSeed || level.targetScore != originalScore ||
            level.targetLines != originalLines || level.goal != originalGoal)
            return false;
        if (PlayerPrefs.HasKey("MW.Journey.Attempt.v1." + number))
        {
            Debug.LogWarning("[Journey 50 Refinement] Preserving existing saved attempt for Level " + number);
            return false;
        }
        level.randomSeed = seed;
        level.targetScore = score;
        level.targetLines = lines;
        level.goal = goal;
        EditorUtility.SetDirty(level);
        return true;
    }
}
