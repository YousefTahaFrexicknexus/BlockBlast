using System;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MagicalWorldsShopPrefabAuthoring
{
    private const string MenuPath = "Assets/MagicalWorlds/Prefabs/MainMenu/MainMenu.prefab";
    static MagicalWorldsShopPrefabAuthoring()
    {
        EditorApplication.delayCall += Ensure;
    }

    [MenuItem("Tools/Magical Worlds/Shop/Build Editable Shop Panel")]
    public static void Ensure()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += Ensure;
            return;
        }

        JourneyShopCatalogAuthoring.Ensure();
        JourneyShopCatalog catalog = AssetDatabase.LoadAssetAtPath<JourneyShopCatalog>(
            JourneyShopCatalogAuthoring.AssetPath);
        if (catalog == null || catalog.Count != 4)
            throw new InvalidOperationException("Four-item Shop catalog missing.");

        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
        if (asset == null) throw new InvalidOperationException("MainMenu prefab missing.");
        if (asset.GetComponent<MagicalWorldsShopUI>() != null) return;

        GameObject root = PrefabUtility.LoadPrefabContents(MenuPath);
        try
        {
            Transform canvas = Find(root.transform, "Canvas - Main Menu 1080x1920");
            Transform parent = Find(canvas, "06 - Popup Container");
            if (parent == null) throw new InvalidOperationException("Popup container missing.");
            MagicalWorldsShopUI shop = root.AddComponent<MagicalWorldsShopUI>();
            RectTransform overlay = Rect("Shop Overlay", parent,
                new Vector2(1080, 1920), Vector2.zero);
            Image veil = overlay.gameObject.AddComponent<Image>();
            veil.color = new Color(.03f, .04f, .15f, .93f);
            veil.raycastTarget = true;

            RectTransform frame = Panel("Shop - Frame", overlay, new Vector2(930, 1230),
                Vector2.zero, Hex("#243365"));
            Outline edge = frame.gameObject.AddComponent<Outline>();
            edge.effectColor = Hex("#E8C3FF");
            edge.effectDistance = new Vector2(5, -5);

            Label("Shop - Title", frame, "THE ENCHANTED SHOP",
                new Vector2(790, 90), new Vector2(0, 520), 53, Hex("#FFDB76"));
            Label("Shop - Description", frame, "BOARD AURAS - COSMETIC ONLY",
                new Vector2(820, 65), new Vector2(0, 446), 32, Color.white);
            Text balance = Label("Shop - Wallet Balance", frame, "YOUR COINS  0",
                new Vector2(780, 80), new Vector2(0, 373), 40, Hex("#FFDB76"));
            Text feedback = Label("Shop - Feedback", frame, "UNLOCK A BOARD AURA WITH COINS",
                new Vector2(820, 66), new Vector2(0, 307), 29, Hex("#C6FFE7"));

            Button[] buttons = new Button[4];
            Text[] names = new Text[4];
            Text[] prices = new Text[4];
            Image[] swatches = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                BoardPaletteItem item = catalog.Get(i);
                float y = 193f - i * 170f;
                RectTransform row = Panel("Shop - " + item.displayName, frame,
                    new Vector2(810, 147), new Vector2(0, y), Hex("#304775"));
                RectTransform sample = Panel("Board Aura Swatch", row,
                    new Vector2(98, 94), new Vector2(-341, 0), item.emptyCellColor);
                swatches[i] = sample.GetComponent<Image>();
                names[i] = Label("Palette Name", row, item.displayName.ToUpperInvariant(),
                    new Vector2(365, 95), new Vector2(-66, 0), 30, Color.white);
                RectTransform action = Panel("Palette Purchase Button", row,
                    new Vector2(216, 90), new Vector2(282, 0), Hex("#5C4BC6"));
                Button button = action.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;
                prices[i] = Label("Purchase Price", action,
                    i == 0 ? "EQUIPPED" : item.coinPrice + " COINS",
                    new Vector2(198, 72), Vector2.zero, 26, Color.white);
                buttons[i] = button;
            }

            RectTransform close = Panel("Shop - Back Button", frame,
                new Vector2(410, 100), new Vector2(0, -543), Hex("#5C4BC6"));
            Button back = close.gameObject.AddComponent<Button>();
            Label("Shop - Back Label", close, "BACK", new Vector2(390, 85),
                Vector2.zero, 39, Color.white);

            SerializedObject serialized = new SerializedObject(shop);
            serialized.FindProperty("shopOverlay").objectReferenceValue = overlay.gameObject;
            serialized.FindProperty("closeButton").objectReferenceValue = back;
            serialized.FindProperty("walletLabel").objectReferenceValue = balance;
            serialized.FindProperty("feedbackLabel").objectReferenceValue = feedback;
            SetRefs(serialized.FindProperty("paletteButtons"), buttons);
            SetRefs(serialized.FindProperty("paletteNames"), names);
            SetRefs(serialized.FindProperty("palettePrices"), prices);
            SetRefs(serialized.FindProperty("paletteSwatches"), swatches);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            MagicalWorldsMenuController menu = root.GetComponent<MagicalWorldsMenuController>();
            SerializedObject controller = new SerializedObject(menu);
            controller.FindProperty("shopUI").objectReferenceValue = shop;
            controller.ApplyModifiedPropertiesWithoutUndo();
            overlay.gameObject.SetActive(false);
            if (PrefabUtility.SaveAsPrefabAsset(root, MenuPath) == null)
                throw new InvalidOperationException("Unable to save authored Shop prefab.");
            Debug.Log("[Shop] Saved editable four-card Shop within original MainMenu.prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void SetRefs<T>(SerializedProperty property, T[] values) where T : UnityEngine.Object
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 pos)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        return rect;
    }

    private static RectTransform Panel(string name, Transform parent, Vector2 size,
        Vector2 pos, Color color)
    {
        RectTransform rect = Rect(name, parent, size, pos);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return rect;
    }

    private static Text Label(string name, Transform parent, string content,
        Vector2 size, Vector2 pos, int fontSize, Color color)
    {
        RectTransform rect = Rect(name, parent, size, pos);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 20;
        text.resizeTextMaxSize = fontSize;
        return text;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color result);
        return result;
    }

    private static Transform Find(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform candidate = Find(child, name);
            if (candidate != null) return candidate;
        }
        return null;
    }
}