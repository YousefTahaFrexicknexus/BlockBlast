using System;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class JourneyEconomyQA
{
    const string SampleStars = "MW.Journey.Stars.v1.49";
    const string SampleStars2 = "MW.Journey.Stars.v1.50";

    static JourneyEconomyQA() { EditorApplication.delayCall += RunIfReady; }

    static void RunIfReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += RunIfReady;
            return;
        }
        try { Validate(); }
        catch (Exception error) { Debug.LogError("[Journey Economy QA] FAILED: " + error); }
    }

    [MenuItem("Tools/Magical Worlds/Economy/Validate Wallet and Rewards")]
    public static void Validate()
    {
        JourneyEconomyAssetAuthoring.Ensure();
        JourneyEconomyConfig config = AssetDatabase.LoadAssetAtPath<JourneyEconomyConfig>(
            JourneyEconomyAssetAuthoring.AssetPath);
        Check(config != null, "Missing editor economy config.");
        Check(config.checkpointEveryLevels >= 1 && config.checkpointBonusCoins >= 0,
            "Checkpoint settings must have a positive interval.");
        int checkpoint = Mathf.Min(50, config.checkpointEveryLevels);
        int expectedCheckpoint = config.firstClearCoins +
            checkpoint * config.coinsPerLevel + config.coinsPerNewStar +
            (checkpoint % config.checkpointEveryLevels == 0 ? config.checkpointBonusCoins : 0);
        Check(config.CalculateNewCoins(checkpoint, 0, 1) == expectedCheckpoint,
            "First clear checkpoint reward is not calculated from Inspector settings.");
        if (config.checkpointEveryLevels > 1)
        {
            Check(config.CalculateNewCoins(1, 0, 1) ==
                config.firstClearCoins + config.coinsPerLevel + config.coinsPerNewStar,
                "A non-checkpoint level received an extra bonus.");
        }
        Check(config.CalculateNewCoins(checkpoint, 1, 3) == 2 * config.coinsPerNewStar,
            "Improving stars must never re-award the checkpoint bonus.");
        bool hadWallet = PlayerPrefs.HasKey(JourneyWallet.SaveKey);
        string beforeWallet = PlayerPrefs.GetString(JourneyWallet.SaveKey, "");
        bool had49 = PlayerPrefs.HasKey(SampleStars);
        bool had50 = PlayerPrefs.HasKey(SampleStars2);
        int before49 = PlayerPrefs.GetInt(SampleStars);
        int before50 = PlayerPrefs.GetInt(SampleStars2);
        try
        {
            PlayerPrefs.DeleteKey(JourneyWallet.SaveKey);
            PlayerPrefs.SetInt(SampleStars, 2);
            PlayerPrefs.SetInt(SampleStars2, 0);
            JourneyWalletView initial = JourneyWallet.GetView();
            Check(initial.Coins == config.startingCoins &&
                initial.Gems == config.startingGems &&
                initial.Energy == config.maximumEnergy,
                "Starting wallet does not match Inspector defaults.");
            Check(JourneyWallet.AwardJourneyWin(49, 2) == 0,
                "Existing pre-economy best stars were rewarded again.");
            int expected = config.CalculateNewCoins(50, 0, 1);
            Check(JourneyWallet.AwardJourneyWin(50, 1) == expected,
                "First-clear coin calculation is incorrect.");
            Check(JourneyWallet.AwardJourneyWin(50, 1) == 0,
                "Repeated completion incorrectly pays again.");
            Check(JourneyWallet.AwardJourneyWin(50, 0) == 0,
                "Invalid star reward incorrectly pays.");
            int starUpgrade = config.CalculateNewCoins(50, 1, 3);
            Check(JourneyWallet.AwardJourneyWin(50, 3) == starUpgrade,
                "Improved stars must pay only the difference.");
            Check(JourneyWallet.AwardJourneyWin(50, 2) == 0,
                "Lower-star replay must not pay.");
            JourneyWalletView after = JourneyWallet.GetView();
            Check(after.Coins == initial.Coins + expected + starUpgrade &&
                after.Gems == initial.Gems && after.Energy == initial.Energy,
                "Wallet balances or non-coin currencies changed unexpectedly.");

            JourneyWalletData saved = JsonUtility.FromJson<JourneyWalletData>(
                PlayerPrefs.GetString(JourneyWallet.SaveKey));
            Check(saved != null && saved.rewardedStars[48] == 2 &&
                saved.rewardedStars[49] == 3,
                "Wallet ledger was not saved atomically with balances.");
            Check(JourneyWallet.AwardJourneyWin(51, 3) == 0,
                "Out-of-bounds level should never grant coins.");
            Check(config.CalculateNewCoins(50, 3, 3) == 0,
                "Replaying a completed level should never award more coins.");

            Debug.Log("[Journey Economy QA] PASSED: initial wallet, legacy migration, " +
                "one-time clear, upgrade-only star coins, repeated/lower-star replays, " +
                "bounds and durable ledger. Previous player wallet restored.");
        }
        finally
        {
            if (hadWallet) PlayerPrefs.SetString(JourneyWallet.SaveKey, beforeWallet);
            else PlayerPrefs.DeleteKey(JourneyWallet.SaveKey);
            if (had49) PlayerPrefs.SetInt(SampleStars, before49);
            else PlayerPrefs.DeleteKey(SampleStars);
            if (had50) PlayerPrefs.SetInt(SampleStars2, before50);
            else PlayerPrefs.DeleteKey(SampleStars2);
            PlayerPrefs.Save();
        }
    }

    private static void Check(bool success, string description)
    {
        if (!success) throw new InvalidOperationException(description);
    }
}
