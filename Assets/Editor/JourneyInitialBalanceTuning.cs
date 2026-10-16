using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class JourneyInitialBalanceTuning
{
    static JourneyInitialBalanceTuning()
    {
        EditorApplication.delayCall += ApplyOriginalDefaultsOnly;
    }

    [MenuItem("Tools/Magical Worlds/Journey/Apply Initial Level Balance")]
    public static void ApplyOriginalDefaultsOnly()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += ApplyOriginalDefaultsOnly;
            return;
        }

        JourneyLevelCatalog catalog = AssetDatabase.LoadAssetAtPath<JourneyLevelCatalog>(
            "Assets/Resources/MagicalWorlds/Journey/LevelCatalog.asset");
        if (catalog == null || catalog.Count < 6) return;

        bool changed = false;
        JourneyLevelDefinition fourth = catalog.Get(4);
        JourneyLevelDefinition sixth = catalog.Get(6);
        if (fourth != null && fourth.goal == JourneyGoalType.ReachScore &&
            fourth.targetScore == 80 && fourth.moveLimit == 13)
        {
            if (PlayerPrefs.HasKey("MW.Journey.Attempt.v1.4"))
                Debug.LogWarning("[Journey Balance] Preserved active Level 4 attempt; skipped its tuning.");
            else
            {
                fourth.targetScore = 125;
                EditorUtility.SetDirty(fourth);
                changed = true;
            }
        }
        if (sixth != null && sixth.goal == JourneyGoalType.ScoreAndLines &&
            sixth.targetScore == 105 && sixth.targetLines == 1 && sixth.moveLimit == 18)
        {
            if (PlayerPrefs.HasKey("MW.Journey.Attempt.v1.6"))
                Debug.LogWarning("[Journey Balance] Preserved active Level 6 attempt; skipped its tuning.");
            else
            {
                sixth.targetScore = 225;
                sixth.targetLines = 3;
                EditorUtility.SetDirty(sixth);
                changed = true;
            }
        }
        if (changed)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("[Journey Balance] Tuned original Level 4/6 defaults without altering user-edited levels.");
        }
    }
}
