using System;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class JourneyFiftyProgressionQA
{
    const string Once = "MW.Journey.FiftyMigrationQA.v1";
    static JourneyFiftyProgressionQA() { EditorApplication.delayCall += RunOnce; }

    static void RunOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += RunOnce;
            return;
        }
        if (EditorPrefs.GetBool(Once, false)) return;
        if (JourneyLevelCatalog.Load()?.Count != 50) return;
        EditorPrefs.SetBool(Once, true);
        Validate();
    }

    [MenuItem("Tools/Magical Worlds/Journey/Verify Legacy Progress Unlock")]
    public static void Validate()
    {
        string unlock = "MW.Journey.Unlocked.v1";
        string stars = "MW.Journey.Stars.v1.6";
        bool hadUnlock = PlayerPrefs.HasKey(unlock);
        bool hadStars = PlayerPrefs.HasKey(stars);
        int oldUnlock = PlayerPrefs.GetInt(unlock);
        int oldStars = PlayerPrefs.GetInt(stars);

        try
        {
            PlayerPrefs.SetInt(unlock, 6);
            PlayerPrefs.SetInt(stars, 1);
            if (JourneyProgress.HighestUnlocked < 7)
                throw new InvalidOperationException("Legacy Level 6 completion must unlock Level 7.");

            PlayerPrefs.SetInt(stars, 0);
            if (JourneyProgress.HighestUnlocked != 6)
                throw new InvalidOperationException("Incomplete Level 6 must not unlock Level 7.");

            Debug.Log("[Journey 50 Progress QA] PASSED: old six-level saves unlock Level 7 " +
                "only when Level 6 is complete; previous player progression restored.");
        }
        catch (Exception e)
        {
            Debug.LogError("[Journey 50 Progress QA] FAILED: " + e);
        }
        finally
        {
            if (hadUnlock) PlayerPrefs.SetInt(unlock, oldUnlock);
            else PlayerPrefs.DeleteKey(unlock);
            if (hadStars) PlayerPrefs.SetInt(stars, oldStars);
            else PlayerPrefs.DeleteKey(stars);
            PlayerPrefs.Save();
        }
    }
}
