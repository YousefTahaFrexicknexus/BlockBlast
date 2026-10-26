using System;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class JourneyEconomyMenuQA
{
    const string Once = "MW.M13.MenuShopOnce.v2";
    const string Pending = "MW.M12.MenuPending.v1";
    const string MapScene = "Assets/Scenes/JourneyMap.unity";
    const string MenuScene = "Assets/Scenes/MainMenu.unity";
    static double entered;

    static JourneyEconomyMenuQA()
    {
        EditorApplication.delayCall += RunOnce;
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += OnMode;
    }

    static void RunOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += RunOnce;
            return;
        }
        if (EditorPrefs.GetBool(Once, false) ||
            SceneManager.GetActiveScene().path != MapScene ||
            SceneManager.GetActiveScene().isDirty) return;
        EditorPrefs.SetBool(Once, true);
        Run();
    }

    [MenuItem("Tools/Magical Worlds/Economy/Test Main Menu Wallet Labels")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != MapScene ||
            SceneManager.GetActiveScene().isDirty)
        {
            Debug.LogWarning("[Economy Menu QA] Open the saved JourneyMap in Edit Mode before testing.");
            return;
        }
        string key = JourneyWallet.SaveKey;
        EditorPrefs.SetBool(Pending + ".hadWallet", PlayerPrefs.HasKey(key));
        EditorPrefs.SetString(Pending + ".previous", PlayerPrefs.GetString(key, ""));
        JourneyWalletData test = new JourneyWalletData
        {
            coins = 4321,
            gems = 17,
            energy = 3,
            rewardedStars = new int[50]
        };
        PlayerPrefs.SetString(key, JsonUtility.ToJson(test));
        PlayerPrefs.Save();

        EditorPrefs.SetBool(Pending, true);
        EditorPrefs.SetBool(Pending + ".checked", false);
        EditorSceneManager.OpenScene(MenuScene);
        entered = EditorApplication.timeSinceStartup;
        EditorApplication.isPlaying = true;
    }

    private static void Poll()
    {
        if (!EditorPrefs.GetBool(Pending, false) ||
            !EditorApplication.isPlaying ||
            EditorPrefs.GetBool(Pending + ".checked", false) ||
            SceneManager.GetActiveScene().name != "MainMenu") return;

        EditorPrefs.SetBool(Pending + ".checked", true);
        try
        {
            MagicalWorldsMenuController menu =
                UnityEngine.Object.FindFirstObjectByType<MagicalWorldsMenuController>();
            Require(menu != null, "Missing authored Main Menu controller");
            Text coins = Private<Text>(menu, "coinsValue");
            Text gems = Private<Text>(menu, "gemsValue");
            Text energy = Private<Text>(menu, "energyValue");
            Require(coins != null && coins.text == 4321.ToString("N0"), "Coins label does not read wallet");
            Require(gems != null && gems.text == "17", "Gems label does not read wallet");
            JourneyWalletView values = JourneyWallet.GetView();
            Require(energy != null && energy.text == "3/" + values.MaximumEnergy,
                "Energy label does not read wallet");
            menu.ShowShop();
            MagicalWorldsShopUI shop = Private<MagicalWorldsShopUI>(menu, "shopUI");
            Require(shop != null && shop.IsOpen, "Authored Shop panel did not open");
            Text balance = PrivateShop<Text>(shop, "walletLabel");
            Require(balance != null && balance.text.Contains("4,321"),
                "Shop balance did not match saved coins");
            Button[] purchaseButtons = PrivateShop<Button[]>(shop, "paletteButtons");
            Require(purchaseButtons != null && purchaseButtons.Length == 4,
                "Expected four editable Shop purchase buttons");
            int before = JourneyWallet.GetView().Coins;
            BoardPaletteItem moonlight = JourneyShopCatalog.Load().Get(1);
            purchaseButtons[1].onClick.Invoke();
            Require(JourneyWallet.EquippedPaletteId == moonlight.id &&
                JourneyWallet.GetView().Coins == before - moonlight.coinPrice,
                "Buying Moonlight did not debit coins and equip the cosmetic");
            Text[] prices = PrivateShop<Text[]>(shop, "palettePrices");
            Require(prices[1].text == "EQUIPPED", "Shop selection label did not refresh");
            Require(!purchaseButtons[1].interactable, "Equipped palette button should be disabled");
            menu.ClosePopup();
            Require(!shop.IsOpen && coins.text == (before - moonlight.coinPrice).ToString("N0"),
                "Closing Shop did not refresh the menu wallet");
            Debug.Log("[Economy Menu QA] PASSED: live Main Menu wallet, Shop purchase " +
                "button, correct coin debit, equipped palette and refreshed header.");
        }
        catch (Exception error)
        {
            Debug.LogError("[Economy Menu QA] FAILED: " + error);
        }
        finally
        {
            EditorApplication.isPlaying = false;
        }
    }

    private static void OnMode(PlayModeStateChange mode)
    {
        if (mode != PlayModeStateChange.EnteredEditMode ||
            !EditorPrefs.GetBool(Pending, false)) return;
        string key = JourneyWallet.SaveKey;
        if (EditorPrefs.GetBool(Pending + ".hadWallet", false))
            PlayerPrefs.SetString(key, EditorPrefs.GetString(Pending + ".previous", ""));
        else PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        EditorPrefs.SetBool(Pending, false);
        EditorPrefs.SetBool(Pending + ".checked", false);
        if (SceneManager.GetActiveScene().path != MapScene)
            EditorSceneManager.OpenScene(MapScene);
    }

    private static T Private<T>(MagicalWorldsMenuController menu, string field)
    {
        return (T)typeof(MagicalWorldsMenuController).GetField(field,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic).GetValue(menu);
    }

    private static T PrivateShop<T>(MagicalWorldsShopUI shop, string field)
    {
        return (T)typeof(MagicalWorldsShopUI).GetField(field,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic).GetValue(shop);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
