using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;

public static class JourneyCatalogCreator
{
    public static JourneyLevelCatalog Ensure()
    {
        string catalogPath = MagicalWorldsJourneyBuilder.CatalogPath;
        JourneyLevelCatalog catalog = AssetDatabase.LoadAssetAtPath<JourneyLevelCatalog>(catalogPath);
        if (catalog != null) return catalog;

        catalog = ScriptableObject.CreateInstance<JourneyLevelCatalog>();
        catalog.levels = new JourneyLevelDefinition[6];
        string[] names = {"First Steps", "Moonlit Clearing", "Blooming Path",
            "Crystal Cascade", "Starlight Bridge", "Garden Guardian"};
        int[] moveLimits = {8,10,12,13,15,18};
        int[] scores = {12,30,1,125,1,225};
        int[] lines = {0,0,1,0,2,3};
        JourneyGoalType[] goals = {JourneyGoalType.ReachScore, JourneyGoalType.ReachScore,
            JourneyGoalType.ClearLines, JourneyGoalType.ReachScore,
            JourneyGoalType.ClearLines, JourneyGoalType.ScoreAndLines};

        for (int i = 0; i < catalog.Count; i++)
        {
            JourneyLevelDefinition level = ScriptableObject.CreateInstance<JourneyLevelDefinition>();
            level.levelNumber = i + 1;
            level.levelName = names[i];
            level.moveLimit = moveLimits[i];
            level.goal = goals[i];
            level.targetScore = scores[i];
            level.targetLines = lines[i];
            level.randomSeed = 105 + 11 * i;
            AssetDatabase.CreateAsset(level, MagicalWorldsJourneyBuilder.ResourcesFolder +
                "/Level_" + (i + 1).ToString("00") + ".asset");
            catalog.levels[i] = level;
        }
        AssetDatabase.CreateAsset(catalog, catalogPath);
        return catalog;
    }
}
