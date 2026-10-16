using System;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class JourneyFiftyLevelAuthoring
{
    public const int TotalLevels = 50;
    private const int OriginalLevels = 6;
    private const string Folder = "Assets/Resources/MagicalWorlds/Journey";

    private static readonly string[] NewNames =
    {
        "Moonpetal Glade", "Whispering Lanterns", "Silverbrook Trail",
        "Starfall Terrace", "Enchanted Crossing", "Twilight Orchard",
        "Glimmering Vale", "Luminous Grove", "Celestial Steps",
        "Sapphire Fountain", "Fae Blossom Gate", "The Dreaming Bridge",
        "Moonstone Garden", "Wishing Well", "Crystal Moonrise",
        "The Azure Stair", "Petalwind Sanctuary", "Starlit Courtyard",
        "Rosewater Falls", "Misty Spires", "Forest of Echoes",
        "The Pearl Cascade", "Aurora Promenade", "Emerald Dream",
        "The Hidden Conservatory", "Castle of Lanterns", "Midnight Meadows",
        "Cloudveil Crossing", "Astral Pavilion", "Everbloom Valley",
        "The Moonlit Archive", "Opal Horizon", "The Dreamweaver",
        "Prismflower Palace", "Celestial River", "Fairy Crown",
        "The Violet Labyrinth", "Starlace Summit", "The Silver Citadel",
        "Radiant Heartwood", "Nightbloom Keep", "The Golden Canopy",
        "The Astral Throne", "Everlasting Bloom"
    };

    static JourneyFiftyLevelAuthoring()
    {
        EditorApplication.delayCall += UpgradeOnReload;
    }

    private static void UpgradeOnReload()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += UpgradeOnReload;
            return;
        }
        try { EnsureCatalog(); }
        catch (Exception e) { Debug.LogError("[Journey 50 Authoring] FAILED: " + e); }
    }

    [MenuItem("Tools/Magical Worlds/Journey/Ensure 50 Level Assets")]
    public static void EnsureCatalog()
    {
        JourneyLevelCatalog catalog = AssetDatabase.LoadAssetAtPath<JourneyLevelCatalog>(
            MagicalWorldsJourneyBuilder.CatalogPath);
        if (catalog == null || catalog.Count < OriginalLevels)
            throw new InvalidOperationException("Original six Journey levels must exist first.");
        if (NewNames.Length != TotalLevels - OriginalLevels)
            throw new InvalidOperationException("44 unique level names required.");

        bool changed = false;
        JourneyLevelDefinition[] ordered = new JourneyLevelDefinition[TotalLevels];
        for (int i = 0; i < OriginalLevels; i++)
        {
            ordered[i] = catalog.Get(i + 1);
            if (ordered[i] == null)
                throw new InvalidOperationException("An original approved level is missing: " + (i + 1));
        }

        for (int number = OriginalLevels + 1; number <= TotalLevels; number++)
        {
            string path = Folder + "/Level_" + number.ToString("00") + ".asset";
            JourneyLevelDefinition level = AssetDatabase.LoadAssetAtPath<JourneyLevelDefinition>(path);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<JourneyLevelDefinition>();
                ConfigureNewLevel(level, number);
                AssetDatabase.CreateAsset(level, path);
                changed = true;
            }
            ordered[number - 1] = level;
        }
        if (catalog.Count != TotalLevels)
        {
            catalog.levels = ordered;
            EditorUtility.SetDirty(catalog);
            changed = true;
        }
        else
        {
            for (int index = 0; index < TotalLevels; index++)
            {
                if (catalog.levels[index] == ordered[index]) continue;
                catalog.levels[index] = ordered[index];
                changed = true;
                EditorUtility.SetDirty(catalog);
            }
        }
        if (changed)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("[Journey 50 Authoring] Created/preserved level assets; catalog now has " +
                catalog.Count + " levels. Original 1-6 and existing 7-50 assets were not modified.");
        }
    }

    private static void ConfigureNewLevel(JourneyLevelDefinition level, int number)
    {
        int step = number - OriginalLevels;
        level.levelNumber = number;
        level.levelName = NewNames[number - OriginalLevels - 1];
        level.randomSeed = 160 + step * 29;
        // Gradually give more time but ask for noticeably more careful play.
        level.moveLimit = Mathf.Min(32, 18 + (step - 1) / 3);
        level.targetLines = 3 + (step + 2) / 5;
        level.targetScore = Mathf.RoundToInt((225 + step * 27) / 5f) * 5;
        // Score only requires more points to match the risk of dual objectives.
        // Line-only targets rise steadily in proportion to the longer move budget.
        int variant = (number - 7) % 5;
        if (variant == 1)
        {
            level.goal = JourneyGoalType.ClearLines;
            level.targetLines = 3 + (step + 2) / 5;
            level.targetScore = 1; // Ignored for clear-lines-only objective.
        }
        else if (variant == 3)
        {
            level.goal = JourneyGoalType.ReachScore;
            level.targetScore = Mathf.RoundToInt((225 + step * 37) / 5f) * 5;
            level.targetLines = 0;
        }
        else
        {
            level.goal = JourneyGoalType.ScoreAndLines;
        }
        // Preset calibrations are applied only to newly authored levels.
        if (number == 11) level.randomSeed = 306;
        if (number == 12) { level.randomSeed = 335; level.targetScore = 560; level.targetLines = 5; }
        if (number == 17) level.randomSeed = 492;
        if (number == 24) level.randomSeed = 729;
        if (number == 34) level.randomSeed = 973;
        if (number == 41) level.randomSeed = 1182;
        if (number == 45) level.targetScore = 3000;
        if (number == 48) level.randomSeed = 1409;
        if (number == 50)
        {
            level.goal = JourneyGoalType.ScoreAndLines;
            level.targetScore = 2650;
            level.targetLines = 11;
        }
    }
}
