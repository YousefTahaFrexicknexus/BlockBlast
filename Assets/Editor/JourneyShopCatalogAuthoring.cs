using System;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class JourneyShopCatalogAuthoring
{
    public const string AssetPath = "Assets/Resources/MagicalWorlds/Economy/JourneyShopCatalog.asset";
    static JourneyShopCatalogAuthoring() { EditorApplication.delayCall += Ensure; }

    [MenuItem("Tools/Magical Worlds/Shop/Ensure Palette Catalog")]
    public static void Ensure()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += Ensure;
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<JourneyShopCatalog>(AssetPath) != null) return;

        var catalog = ScriptableObject.CreateInstance<JourneyShopCatalog>();
        catalog.palettes = new[]
        {
            New("original", "Enchanted Garden", 0, "#2C3E78", "#22336D", "#34519A"),
            New("moonlight", "Moonlight", 350, "#355F92", "#24446C", "#447CC2"),
            New("rose", "Rose Quartz", 600, "#704678", "#603B72", "#AD63B0"),
            New("emerald", "Emerald Glade", 850, "#2D716B", "#24524C", "#3B9880")
        };
        AssetDatabase.CreateAsset(catalog, AssetPath);
        AssetDatabase.SaveAssets();
        Debug.Log("[Shop] Created editable four-palette cosmetic catalog.");
    }

    private static BoardPaletteItem New(string id, string title, int price,
        string empty, string tray, string selected)
    {
        ColorUtility.TryParseHtmlString(empty, out Color a);
        ColorUtility.TryParseHtmlString(tray, out Color b);
        ColorUtility.TryParseHtmlString(selected, out Color c);
        return new BoardPaletteItem
        {
            id = id, displayName = title, coinPrice = price,
            emptyCellColor = a, trayColor = b, selectedTrayColor = c
        };
    }
}