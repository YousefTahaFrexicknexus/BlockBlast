using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class JourneyPresentationQA
{
    static JourneyPresentationQA()
    {
        EditorApplication.delayCall += ValidateAfterImport;
    }

    private static void ValidateAfterImport()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += ValidateAfterImport;
            return;
        }
        try { Validate(); }
        catch (Exception exception) { Debug.LogError("[Journey Presentation QA] FAILED: " + exception); }
    }

    [MenuItem("Tools/Magical Worlds/Journey/Validate HUD and Level Nodes")]
    public static void Validate()
    {
        const string path = "Assets/MagicalWorlds/Prefabs/Journey/JourneyGameplayRoot.prefab";
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Require(root != null, "JourneyGameplayRoot prefab missing");

        Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
        Canvas journeyCanvas = null;
        Canvas classicCanvas = null;
        foreach (Canvas canvas in canvases)
        {
            if (canvas.name == "Journey HUD Canvas") journeyCanvas = canvas;
            if (canvas.name == "Block Blast Canvas") classicCanvas = canvas;
        }
        Require(journeyCanvas != null && classicCanvas != null, "Both independent canvases required");
        Require(journeyCanvas.sortingOrder > classicCanvas.sortingOrder, "Journey HUD must draw above Classic board");

        JourneySession session = root.GetComponent<JourneySession>();
        Require(session != null, "JourneySession component missing");
        SerializedObject values = new SerializedObject(session);
        SerializedProperty header = values.FindProperty("hudRoot");
        Require(header != null && header.objectReferenceValue != null,
            "Journey objective header visibility reference missing");

        Transform classicHeader = classicCanvas.transform.Find("02 - Header and Score");
        Require(classicHeader != null && !classicHeader.gameObject.activeSelf,
            "Classic-only header must be hidden on Journey prefab");
        BlockBlastUIReferences refs = classicCanvas.GetComponent<BlockBlastUIReferences>();
        Require(refs != null && refs.IsComplete, "Shared Classic gameplay UI references changed");

        GameObject map = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/MagicalWorlds/Prefabs/Journey/JourneyMapRoot.prefab");
        Require(map != null, "JourneyMapRoot prefab missing");
        int checkedNodes = 0;
        foreach (JourneyLevelNode node in map.GetComponentsInChildren<JourneyLevelNode>(true))
        {
            if (node.LevelNumber < 1) throw new Exception("Journey map node is unnumbered");
            checkedNodes++;
        }
        Require(checkedNodes == 50, "Journey map requires fifty editable level-node components");

        Debug.Log("[Journey Presentation QA] PASSED: Journey HUD above Classic board, " +
            "Classic-only header hidden, Pause-header serialization, all shared UI references " +
            "and 50 numbered map nodes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
