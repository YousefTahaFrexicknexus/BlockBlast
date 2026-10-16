using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MagicalWorldsSafeAreaPrefabUpgrade
{
    private static readonly string MenuPrefab =
        "Assets/MagicalWorlds/Prefabs/MainMenu/MainMenu.prefab";
    private static readonly string ClassicPrefab =
        "Assets/MagicalWorlds/Prefabs/Classic/ClassicGame.prefab";
    private static readonly string JourneyMapPrefab =
        "Assets/MagicalWorlds/Prefabs/Journey/JourneyMapRoot.prefab";
    private static readonly string JourneyGamePrefab =
        "Assets/MagicalWorlds/Prefabs/Journey/JourneyGameplayRoot.prefab";

    static MagicalWorldsSafeAreaPrefabUpgrade()
    {
        EditorApplication.delayCall += UpgradeWhenReady;
    }

    private static void UpgradeWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += UpgradeWhenReady;
            return;
        }
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[Mobile Safe Area] FAILED: " + e); }
    }

    [MenuItem("Tools/Magical Worlds/Mobile/Apply Safe Area to UI Prefabs")]
    public static void Apply()
    {
        ApplyOne(MenuPrefab, "Canvas - Main Menu 1080x1920", new[] {
            "02 - Currency and Settings Bar", "05 - Bottom Navigation"
        }, new[] { MobileSafeAreaDock.Edge.Top, MobileSafeAreaDock.Edge.Bottom },
            new[] { 150f, 145f });

        ApplyOne(JourneyMapPrefab, "Journey Map Canvas", new[] {
            "02 - Map Header", "04 - Navigation"
        }, new[] { MobileSafeAreaDock.Edge.Top, MobileSafeAreaDock.Edge.Bottom },
            new[] { 135f, 150f });

        ApplyOne(JourneyGamePrefab, "Journey HUD Canvas", new[] {
            "01 - Level Objective and Moves"
        }, new[] { MobileSafeAreaDock.Edge.Top }, new[] { 105f });

        // Move only Classic's title and top buttons, not the score labels or
        // 8x8 board, to avoid pushing the scoreboard into the gameplay area.
        ApplyOne(ClassicPrefab, "Block Blast Canvas", new[] {
            "Title", "Subtitle", "Sound Toggle", "Haptics Toggle"
        }, new[] {
            MobileSafeAreaDock.Edge.Top, MobileSafeAreaDock.Edge.Top,
            MobileSafeAreaDock.Edge.Top, MobileSafeAreaDock.Edge.Top
        }, new[] { 135f, 115f, 150f, 150f });
    }

    private static void ApplyOne(string path, string canvasName, string[] names,
        MobileSafeAreaDock.Edge[] edges, float[] maxShifts)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            throw new InvalidOperationException("Missing prefab: " + path);
        GameObject editable = PrefabUtility.LoadPrefabContents(path);
        bool changed = false;
        try
        {
            Transform canvas = FindDeep(editable.transform, canvasName);
            if (canvas == null || canvas.GetComponent<Canvas>() == null)
                throw new InvalidOperationException("Canvas missing: " + canvasName);
            if (canvas.GetComponent<MobilePortraitCanvasFit>() == null)
            {
                canvas.gameObject.AddComponent<MobilePortraitCanvasFit>();
                changed = true;
            }
            foreach (string name in names)
            {
                if (FindDeep(canvas, name) == null)
                    throw new InvalidOperationException("Missing mobile section " + name + " in " + path);
            }
            for (int i = 0; i < names.Length; i++)
            {
                Transform item = FindDeep(canvas, names[i]);
                MobileSafeAreaDock dock = item.GetComponent<MobileSafeAreaDock>();
                if (dock != null) continue; // Retain any Inspector changes.
                dock = item.gameObject.AddComponent<MobileSafeAreaDock>();
                SerializedObject serialized = new SerializedObject(dock);
                serialized.FindProperty("edge").enumValueIndex = (int)edges[i];
                serialized.FindProperty("maximumShift").floatValue = maxShifts[i];
                serialized.FindProperty("influence").floatValue = 1f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
            if (changed)
            {
                if (PrefabUtility.SaveAsPrefabAsset(editable, path) == null)
                    throw new InvalidOperationException("Unable to save prefab " + path);
                Debug.Log("[Mobile Safe Area] Applied editable docking components to " + path);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(editable);
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
