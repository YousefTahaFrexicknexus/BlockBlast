using System;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class JourneyFiftyMapUpgrade
{
    const string Map = "Assets/MagicalWorlds/Prefabs/Journey/JourneyMapRoot.prefab";
    const string Node = "Assets/MagicalWorlds/Prefabs/Journey/JourneyLevelNode.prefab";

    static JourneyFiftyMapUpgrade() { EditorApplication.delayCall += Auto; }

    static void Auto()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += Auto;
            return;
        }
        try { JourneyFiftyLevelAuthoring.EnsureCatalog(); Upgrade(); }
        catch (Exception e) { Debug.LogError("[Journey 50 Map] FAILED: " + e); }
    }

    [MenuItem("Tools/Magical Worlds/Journey/Upgrade Paged 50-Level Map")]
    public static void Upgrade()
    {
        JourneyLevelCatalog catalog = AssetDatabase.LoadAssetAtPath<JourneyLevelCatalog>(
            MagicalWorldsJourneyBuilder.CatalogPath);
        if (catalog == null || catalog.Count != 50) return;
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(Map);
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(Node);
        if (asset == null || template == null) throw new Exception("Missing Journey prefab.");
        if (asset.GetComponentsInChildren<JourneyLevelNode>(true).Length == 50 &&
            HasPageControls(asset.GetComponent<JourneyMapController>())) return;

        GameObject root = PrefabUtility.LoadPrefabContents(Map);
        try
        {
            JourneyMapController controller = root.GetComponent<JourneyMapController>();
            Transform canvas = Find(root.transform, "Journey Map Canvas");
            Transform path = Find(canvas, "03 - Level Path");
            if (controller == null || path == null) throw new Exception("Map hierarchy missing.");
            JourneyLevelNode[] nodes = new JourneyLevelNode[50];
            foreach (JourneyLevelNode item in root.GetComponentsInChildren<JourneyLevelNode>(true))
            {
                int number = item.LevelNumber;
                if (number < 1 || number > 50 || nodes[number - 1] != null)
                    throw new Exception("Duplicated map level " + number);
                nodes[number - 1] = item;
            }

            for (int number = 7; number <= 50; number++)
            {
                if (nodes[number - 1] != null) continue;
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template, path);
                if (instance == null) throw new Exception("Level prefab instantiation failed");
                instance.name = "Level " + number.ToString("00");
                int cell = (number - 1) % 6;
                RectTransform rect = instance.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(cell % 2 == 0 ? -225 : 225,
                    458 - (cell / 2) * 400);
                JourneyLevelNode node = instance.GetComponent<JourneyLevelNode>();
                node.ConfigureInEditor(number);
                PrefabUtility.RecordPrefabInstancePropertyModifications(node);
                foreach (Text label in node.GetComponentsInChildren<Text>(true))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(label);
                instance.SetActive(false);
                nodes[number - 1] = node;
            }
// pagination continuation
            JourneyEditorUI.Setup();
            Transform nav = Find(canvas, "05 - Page Navigation");
            if (nav == null) nav = JourneyEditorUI.Group("05 - Page Navigation", canvas);
            Button previous = Find(nav, "Previous Levels")?.GetComponent<Button>();
            if (previous == null)
                previous = JourneyEditorUI.Button("Previous Levels", nav, "PREV",
                    new Vector2(-350, -628), new Vector2(210,104),
                    JourneyEditorUI.Hex("#4B57A0"), 37);
            Button next = Find(nav, "Next Levels")?.GetComponent<Button>();
            if (next == null)
                next = JourneyEditorUI.Button("Next Levels", nav, "NEXT",
                    new Vector2(350, -628), new Vector2(210,104),
                    JourneyEditorUI.Hex("#4B57A0"), 37);
            Text counter = Find(nav, "Page Count")?.GetComponent<Text>();
            if (counter == null)
                counter = JourneyEditorUI.Text("Page Count", nav,
                    "LEVELS 1 - 6 (1/9)", new Vector2(0,-628),
                    new Vector2(440,95),31,JourneyEditorUI.Gold);
            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty array = serialized.FindProperty("levelNodes");
            array.arraySize = 50;
            for (int index = 0; index < 50; index++)
            {
                if (nodes[index] == null) throw new Exception("Missing node " + (index + 1));
                array.GetArrayElementAtIndex(index).objectReferenceValue = nodes[index];
            }
            serialized.FindProperty("previousPageButton").objectReferenceValue = previous;
            serialized.FindProperty("nextPageButton").objectReferenceValue = next;
            serialized.FindProperty("pageLabel").objectReferenceValue = counter;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.SaveAsPrefabAsset(root, Map) == null)
                throw new Exception("Saving paged map failed.");
            Debug.Log("[Journey 50 Map] Saved 50 authored nodes with nine-page controls.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static bool HasPageControls(JourneyMapController map)
    {
        if (map == null) return false;
        SerializedObject s = new SerializedObject(map);
        return s.FindProperty("previousPageButton")?.objectReferenceValue != null &&
            s.FindProperty("nextPageButton")?.objectReferenceValue != null &&
            s.FindProperty("pageLabel")?.objectReferenceValue != null;
    }

    static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform found = Find(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
