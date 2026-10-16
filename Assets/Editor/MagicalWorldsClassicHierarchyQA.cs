using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MagicalWorldsClassicHierarchyQA
{
    private const string Root = "Assets/MagicalWorlds/Prefabs/Classic/ClassicGame.prefab";
    private const string Scene = "Assets/Scenes/ClassicGameplay.unity";

    static MagicalWorldsClassicHierarchyQA()
    {
        EditorApplication.delayCall += RunAfterReload;
    }

    private static void RunAfterReload()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunAfterReload;
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Root) == null)
        {
            return;
        }
        try
        {
            MagicalWorldsClassicGameOverUpgrade.EnsureGameOverMenuButton();
            MagicalWorldsClassicPauseMenuUpgrade.EnsurePauseMenuButton();
            UpgradeSerializedUIReferences();
            FixCanvasScaleIfNeeded();
            Validate();
        }
        catch (Exception ex)
        {
            Debug.LogError("[Magical Worlds Hierarchy QA] " + ex);
        }
    }

    [MenuItem("Tools/Magical Worlds/Hierarchy/Validate Classic Prefabs")]
    public static void Validate()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root);
        Require(prefab != null, "ClassicGame prefab missing");
        Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(Scene) != null, "ClassicGameplay scene missing");
        Require(prefab.GetComponent<BlockBlastPrototype>() != null, "Prototype component missing");

        RectTransform canvas = FindDeep(prefab.transform, "Block Blast Canvas") as RectTransform;
        Require(canvas != null, "Canvas missing");
        Require(canvas.GetComponent<Canvas>() != null, "Canvas component missing");
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        Require(scaler != null && scaler.referenceResolution == new Vector2(1080, 1920), "CanvasScaler is not 1080x1920");
        Require(canvas.localScale == Vector3.one, "Canvas scale is not (1, 1, 1)");
        BlockBlastUIReferences view = canvas.GetComponent<BlockBlastUIReferences>();
        Require(view != null && view.IsComplete, "Inspector UI references are missing or incomplete");

        string[] required =
        {
            "01 - Background", "02 - Header and Score", "03 - Board",
            "04 - Controls and Trays", "05 - Dialogs", "8 x 8 Grid",
            "Score", "Best", "Combo", "Piece Tray 1", "Piece Tray 2",
            "Piece Tray 3", "Game Over Overlay", "Dialog Overlay",
            "Dialog Primary", "Dialog Secondary", "Dialog Tertiary"
        };
        foreach (string element in required)
        {
            Require(FindDeep(canvas, element) != null, "Missing authored element " + element);
        }

        for (int row = 0; row < 8; row++)
        {
            for (int column = 0; column < 8; column++)
            {
                Require(FindDeep(canvas, "Cell " + row + ", " + column) != null, "Missing grid cell");
                Require(FindDeep(canvas, "Preview " + row + ", " + column) != null, "Missing placement preview");
            }
        }

        int missingScripts = CountMissingScripts(prefab.transform);
        Require(missingScripts == 0, "Missing MonoBehaviours: " + missingScripts);
        Require(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MagicalWorlds/Prefabs/Classic/GameBoard_8x8.prefab") != null, "Board prefab missing");
        Require(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MagicalWorlds/Prefabs/Classic/PieceTray_01.prefab") != null, "Tray prefab missing");

        Debug.Log("[Magical Worlds Hierarchy QA] Passed: authored Canvas, five sections, 64 cells and previews, UI references, no missing scripts, reusable prefab assets.");
    }

    private static void UpgradeSerializedUIReferences()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(Root);
        if (existing == null)
        {
            return;
        }
        Transform rootCanvas = FindDeep(existing.transform, "Block Blast Canvas");
        BlockBlastUIReferences current = rootCanvas != null ? rootCanvas.GetComponent<BlockBlastUIReferences>() : null;
        if (current != null && current.IsComplete)
        {
            return;
        }

        GameObject editing = PrefabUtility.LoadPrefabContents(Root);
        try
        {
            Transform canvas = FindDeep(editing.transform, "Block Blast Canvas");
            if (canvas == null)
            {
                throw new InvalidOperationException("Authored Canvas not found during reference upgrade.");
            }
            BlockBlastUIReferences references = canvas.GetComponent<BlockBlastUIReferences>();
            if (references == null)
            {
                references = canvas.gameObject.AddComponent<BlockBlastUIReferences>();
            }
            references.RebuildReferences();
            if (!references.IsComplete)
            {
                throw new InvalidOperationException("Cannot populate all UI references. Check the scene hierarchy.");
            }
            PrefabUtility.SaveAsPrefabAsset(editing, Root);
            Debug.Log("[Magical Worlds Hierarchy QA] Inspector UI bindings added to Classic Game prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(editing);
        }
    }
    [MenuItem("Tools/Magical Worlds/Hierarchy/Repair Canvas Scale")]
    public static void FixCanvasScaleIfNeeded()
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(Root);
        if (root == null)
        {
            return;
        }
        RectTransform canvas = FindDeep(root.transform, "Block Blast Canvas") as RectTransform;
        if (canvas == null || canvas.localScale == Vector3.one)
        {
            return;
        }
        GameObject editing = PrefabUtility.LoadPrefabContents(Root);
        try
        {
            RectTransform rect = FindDeep(editing.transform, "Block Blast Canvas") as RectTransform;
            if (rect == null)
            {
                throw new InvalidOperationException("Canvas not found inside prefab contents.");
            }
            rect.localScale = Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(editing, Root);
            Debug.Log("[Magical Worlds Hierarchy QA] Corrected authored Canvas scale to (1, 1, 1).");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(editing);
        }
    }

    private static int CountMissingScripts(Transform root)
    {
        int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root.gameObject);
        foreach (Transform child in root)
        {
            count += CountMissingScripts(child);
        }
        return count;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name)
            {
                return child;
            }
            Transform nested = FindDeep(child, name);
            if (nested != null)
            {
                return nested;
            }
        }
        return null;
    }

    private static void Require(bool success, string message)
    {
        if (!success)
        {
            throw new InvalidOperationException(message);
        }
    }
}