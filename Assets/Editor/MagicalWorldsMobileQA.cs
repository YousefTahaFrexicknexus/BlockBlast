using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MagicalWorldsMobileQA
{
    private const string Base = "Assets/MagicalWorlds/Prefabs/";
    static MagicalWorldsMobileQA()
    {
        EditorApplication.delayCall += RunWhenReady;
    }

    private static void RunWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += RunWhenReady;
            return;
        }
        try
        {
            MagicalWorldsSafeAreaPrefabUpgrade.Apply();
            Validate();
        }
        catch (Exception e) { Debug.LogError("[Mobile Layout QA] FAILED: " + e); }
    }

    [MenuItem("Tools/Magical Worlds/Mobile/Validate Safe Area and Touch Targets")]
    public static void Validate()
    {
        // These use virtual phones: no device simulator dependency, no prefab mutations.
        Rect full = new Rect(0, 0, 1080, 1920);
        Rect cutout = new Rect(36, 90, 1000, 1730); // top 100, bottom 90, left 36, right 44
        ValidateShift(MobileSafeAreaDock.Edge.Top, full, 0, 1, 150);
        ValidateShift(MobileSafeAreaDock.Edge.Bottom, full, 0, 1, 150);
        ValidateShift(MobileSafeAreaDock.Edge.Top, cutout, 100, 1, 150);
        ValidateShift(MobileSafeAreaDock.Edge.Bottom, cutout, 90, 1, 150);
        ValidateShift(MobileSafeAreaDock.Edge.Left, cutout, 36, 1, 150);
        ValidateShift(MobileSafeAreaDock.Edge.Right, cutout, 44, 1, 150);
        ValidateShift(MobileSafeAreaDock.Edge.Top, cutout, 50, 2, 150);
        ValidateShift(MobileSafeAreaDock.Edge.Top, cutout, 70, 1, 70);

        Require(MagicalWorldsMobileDefaults.RecommendedDragThreshold(0, 0) == 12,
            "Invalid resolution drag threshold should be safe.");
        Require(MagicalWorldsMobileDefaults.RecommendedDragThreshold(400, 800) == 12,
            "Small screens should use minimum touch drag threshold.");
        Require(MagicalWorldsMobileDefaults.RecommendedDragThreshold(1080, 1920) == 15,
            "1080x1920 touch drag threshold should be 15px.");
        Require(MagicalWorldsMobileDefaults.RecommendedDragThreshold(1440, 3040) == 20,
            "Large phones should use proportionally higher drag threshold.");
        Require(MagicalWorldsMobileDefaults.RecommendedDragThreshold(2160, 3840) == 24,
            "Drag threshold must have an upper bound.");
        Require(Mathf.Approximately(MobilePortraitCanvasFit.CalculateMatch(
            new Vector2(1080, 1920), new Vector2(1080, 1920), 5f), .5f),
            "1080x1920 must preserve the approved baseline match value.");
        Require(Mathf.Approximately(MobilePortraitCanvasFit.CalculateMatch(
            new Vector2(1080, 2400), new Vector2(1080, 1920), 5f), 0f),
            "Tall displays must favor width to keep the board visible.");
        Require(Mathf.Approximately(MobilePortraitCanvasFit.CalculateMatch(
            new Vector2(1080, 1600), new Vector2(1080, 1920), 5f), 1f),
            "Short displays must favor height to protect buttons.");
        Require(Mathf.Approximately(MobilePortraitCanvasFit.CalculateMatch(
            new Vector2(720, 1280), new Vector2(1080, 1920), 5f), .5f),
            "Proportional 720x1280 must preserve the approved baseline.");
        Require(MobilePortraitCanvasFit.CalculateMatch(Vector2.zero,
            new Vector2(1080, 1920), 5f) == .5f, "Zero dimensions must be safe.");

        Rect tall = new Rect(0, 60, 1080, 2280);
        Vector2 topOffset = MobileSafeAreaDock.CalculateViewportOffset(
            MobileSafeAreaDock.Edge.Top, tall, new Vector2(1080,2400), 1f,
            150f, 2400f, 1920f);
        Vector2 bottomOffset = MobileSafeAreaDock.CalculateViewportOffset(
            MobileSafeAreaDock.Edge.Bottom, tall, new Vector2(1080,2400), 1f,
            150f, 2400f, 1920f);
        Require(Mathf.Abs(topOffset.y - 180f) < .001f &&
            Mathf.Abs(bottomOffset.y + 180f) < .001f,
            "Tall phone header/footer edge following is incorrect.");
        Rect baselineCutout = new Rect(0, 90, 1080, 1730);
        Vector2 baselineTop = MobileSafeAreaDock.CalculateViewportOffset(
            MobileSafeAreaDock.Edge.Top, baselineCutout, new Vector2(1080,1920),
            1f, 150f, 1920f, 1920f);
        Require(Mathf.Abs(baselineTop.y + 100f) < .001f,
            "Notch must push top header into safe bounds.");
        ValidatePrefab("MainMenu/MainMenu.prefab", "Canvas - Main Menu 1080x1920",
            new[] { "02 - Currency and Settings Bar", "05 - Bottom Navigation" },
            new[] { MobileSafeAreaDock.Edge.Top, MobileSafeAreaDock.Edge.Bottom });
        ValidatePrefab("Journey/JourneyMapRoot.prefab", "Journey Map Canvas",
            new[] { "02 - Map Header", "04 - Navigation" },
            new[] { MobileSafeAreaDock.Edge.Top, MobileSafeAreaDock.Edge.Bottom });
        ValidatePrefab("Journey/JourneyGameplayRoot.prefab", "Journey HUD Canvas",
            new[] { "01 - Level Objective and Moves" },
            new[] { MobileSafeAreaDock.Edge.Top });
        ValidatePrefab("Classic/ClassicGame.prefab", "Block Blast Canvas",
            new[] { "Title", "Subtitle", "Sound Toggle", "Haptics Toggle" },
            new[] { MobileSafeAreaDock.Edge.Top, MobileSafeAreaDock.Edge.Top,
                MobileSafeAreaDock.Edge.Top, MobileSafeAreaDock.Edge.Top });

        ValidateTouch("MainMenu/MainMenu.prefab",
            new[] { "Button_Play", "Button_Journey", "Plus - Coins", "Plus - Gems",
                "Plus - Energy", "Navigation_Shop", "Navigation_Settings" });
        ValidateTouch("Journey/JourneyMapRoot.prefab", new[] { "Back to Main Menu" });
        ValidateTouch("Journey/JourneyGameplayRoot.prefab", new[] { "Level Map Button" });
        ValidateTouch("Classic/ClassicGame.prefab",
            new[] { "Sound Toggle", "Haptics Toggle", "Pause" });

        Debug.Log("[Mobile Layout QA] PASSED: 8 inset cases, 7 aspect/layout cases, 5 drag cases, 9 editable docks " +
            "across 4 prefabs, untouched fullscreen artwork/board, and essential touch targets.");
    }

    private static void ValidateShift(MobileSafeAreaDock.Edge edge, Rect safe,
        float expected, float scale, float cap)
    {
        float actual = MobileSafeAreaDock.CalculateShift(edge, safe,
            new Vector2(1080, 1920), scale, cap);
        Require(Mathf.Abs(actual - expected) <= .001f,
            "Incorrect " + edge + " safe inset: " + actual + " vs " + expected);
    }

    private static void ValidatePrefab(string relative, string canvasName,
        string[] names, MobileSafeAreaDock.Edge[] edges)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Base + relative);
        Require(prefab != null, "Missing " + relative);
        Transform canvas = FindDeep(prefab.transform, canvasName);
        Require(canvas != null && canvas.GetComponent<CanvasScaler>() != null,
            "Canvas or reference scaler missing: " + relative);
        Require(canvas.GetComponent<MobilePortraitCanvasFit>() != null,
            "Mobile CanvasScaler fitting missing on " + relative);
        Require(canvas.GetComponent<CanvasScaler>().referenceResolution == new Vector2(1080,1920),
            "Reference resolution changed: " + relative);
        for (int i = 0; i < names.Length; i++)
        {
            Transform selected = FindDeep(canvas, names[i]);
            Require(selected != null, "Missing mobile dock target: " + names[i]);
            MobileSafeAreaDock dock = selected.GetComponent<MobileSafeAreaDock>();
            Require(dock != null && dock.DockEdge == edges[i] && dock.MaximumShift > 0f,
                "Missing or incorrect editable mobile safe area component on " + names[i]);
        }
        Transform backdrop = FindDeep(canvas, "01 - Enchanted Garden Background");
        if (backdrop != null) Require(backdrop.GetComponent<MobileSafeAreaDock>() == null,
            "Fullscreen scenery must not move with safe area.");
        Transform board = FindDeep(canvas, "8 x 8 Grid");
        if (board != null) Require(board.GetComponent<MobileSafeAreaDock>() == null,
            "8x8 board must remain independently positioned.");
    }

    private static void ValidateTouch(string relative, string[] names)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Base + relative);
        Require(prefab != null, "Missing prefab: " + relative);
        foreach (string name in names)
        {
            Transform target = FindDeep(prefab.transform, name);
            Require(target != null && target.GetComponent<Button>() != null,
                "Touch button missing: " + name + " in " + relative);
            RectTransform rect = target.GetComponent<RectTransform>();
            Require(rect != null && rect.rect.width >= 88f && rect.rect.height >= 75f,
                "Touch target may be too small in 1080x1920 reference units: " + name +
                " (" + rect.rect.size + ")");
        }
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }
}
