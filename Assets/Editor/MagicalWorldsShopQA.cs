using System;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MagicalWorldsShopQA
{
    static MagicalWorldsShopQA() { EditorApplication.delayCall += RunWhenReady; }

    private static void RunWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += RunWhenReady;
            return;
        }
        try { Validate(); }
        catch (Exception e) { Debug.LogError("[Shop QA] FAILED: " + e); }
    }

    [MenuItem("Tools/Magical Worlds/Shop/Validate Purchases and Prefab")]
    public static void Validate()
    {
        JourneyShopCatalogAuthoring.Ensure();
        MagicalWorldsShopPrefabAuthoring.Ensure();
        JourneyShopCatalog catalog = JourneyShopCatalog.Load();
        Check(catalog != null && catalog.Count == 4, "Expected four catalog palettes");
        Check(catalog.Get(0).id == "original" && catalog.Get(0).coinPrice == 0,
            "Original palette must remain free");
        for (int i = 0; i < catalog.Count; i++)
        {
            BoardPaletteItem item = catalog.Get(i);
            Check(item != null && !string.IsNullOrWhiteSpace(item.id) &&
                item.coinPrice >= 0, "Missing or invalid palette " + i);
            for (int j = i + 1; j < catalog.Count; j++)
                Check(item.id != catalog.Get(j).id, "Duplicate palette IDs");
        }

        GameObject menu = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/MagicalWorlds/Prefabs/MainMenu/MainMenu.prefab");
        Check(menu != null && menu.GetComponent<MagicalWorldsShopUI>() != null,
            "Shop UI missing from editable MainMenu prefab");
        MagicalWorldsShopUI ui = menu.GetComponent<MagicalWorldsShopUI>();
        SerializedObject serialized = new SerializedObject(ui);
        Check(serialized.FindProperty("shopOverlay").objectReferenceValue != null,
            "Authored Shop overlay missing");
        Check(serialized.FindProperty("paletteButtons").arraySize == catalog.Count,
            "Shop buttons do not match catalog");

        bool had = PlayerPrefs.HasKey(JourneyWallet.SaveKey);
        string oldWallet = PlayerPrefs.GetString(JourneyWallet.SaveKey, "");
        try
        {
            JourneyWalletData test = new JourneyWalletData
            {
                coins = 1200, gems = 23, energy = 5,
                rewardedStars = new int[50],
                ownedPalettes = new string[0], equippedPaletteId = "original"
            };
            PlayerPrefs.SetString(JourneyWallet.SaveKey, JsonUtility.ToJson(test));
            PlayerPrefs.Save();

            Check(JourneyWallet.OwnsPalette("original"), "Free original palette unavailable");
            Check(JourneyWallet.TrySelectPalette("moonlight", out _) &&
                JourneyWallet.EquippedPaletteId == "moonlight",
                "First purchase did not equip Moonlight");
            Check(JourneyWallet.GetView().Coins == 1200 - catalog.Get(1).coinPrice,
                "Coin debit incorrect");
            int balance = JourneyWallet.GetView().Coins;
            Check(JourneyWallet.TrySelectPalette("moonlight", out _) &&
                JourneyWallet.GetView().Coins == balance, "Repurchase double-charged");
            Check(JourneyWallet.TrySelectPalette("rose", out _) &&
                JourneyWallet.OwnsPalette("rose"), "Second purchase failed");
            int afterRose = JourneyWallet.GetView().Coins;
            Check(!JourneyWallet.TrySelectPalette("emerald", out string error) &&
                error == "NOT ENOUGH COINS" &&
                JourneyWallet.GetView().Coins == afterRose &&
                !JourneyWallet.OwnsPalette("emerald"),
                "Insufficient coins must not grant cosmetics or charge");
            Check(JourneyWallet.TrySelectPalette("moonlight", out _) &&
                JourneyWallet.GetView().Coins == afterRose,
                "Equipping owned cosmetic should not cost coins");
            Check(JourneyBoardPalette.EmptyCell == catalog.Get(1).emptyCellColor &&
                JourneyBoardPalette.Tray == catalog.Get(1).trayColor,
                "Selected cosmetic is not used by board rendering");
            Check(JourneyWallet.TrySelectPalette("original", out _) &&
                JourneyWallet.GetView().Coins == afterRose,
                "Switching back to the free original should not cost coins");

            JourneyWalletData saved = JsonUtility.FromJson<JourneyWalletData>(
                PlayerPrefs.GetString(JourneyWallet.SaveKey));
            Check(saved.equippedPaletteId == "original" &&
                saved.ownedPalettes.Length == 2 && saved.coins == afterRose,
                "Purchased ownership or selection did not persist");
            int reward = JourneyWallet.AwardJourneyWin(1, 3);
            Check(reward > 0 && JourneyWallet.GetView().Coins == afterRose + reward,
                "Shop wallet lost normal Journey reward support");
            Check(!JourneyWallet.TrySelectPalette("fake-palette", out _) &&
                JourneyWallet.GetView().Coins == afterRose + reward,
                "Unknown palette should never be purchasable");

            // Migration from Milestone 12 wallets lacking cosmetic fields.
            saved = JsonUtility.FromJson<JourneyWalletData>(
                PlayerPrefs.GetString(JourneyWallet.SaveKey));
            saved.ownedPalettes = null;
            saved.equippedPaletteId = null;
            PlayerPrefs.SetString(JourneyWallet.SaveKey, JsonUtility.ToJson(saved));
            JourneyWalletView migrated = JourneyWallet.GetView();
            Check(migrated.Coins == afterRose + reward &&
                JourneyWallet.EquippedPaletteId == "original",
                "Older saved wallets lost currency during cosmetic migration");

            Debug.Log("[Shop QA] PASSED: authored four-card Shop, free starter, " +
                "purchases, insufficient coins, owned equip, saved inventory, " +
                "cosmetic board tint, migration, and Journey coin rewards.");
        }
        finally
        {
            if (had) PlayerPrefs.SetString(JourneyWallet.SaveKey, oldWallet);
            else PlayerPrefs.DeleteKey(JourneyWallet.SaveKey);
            PlayerPrefs.Save();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}