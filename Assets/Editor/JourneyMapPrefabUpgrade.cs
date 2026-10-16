using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class JourneyMapPrefabUpgrade
{
    static JourneyMapPrefabUpgrade()
    {
        EditorApplication.delayCall += UpdateOnce;
    }

    private static void UpdateOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += UpdateOnce;
            return;
        }
        string path = MagicalWorldsJourneyBuilder.MapPrefab;
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (source == null) return;
        JourneyLevelNode[] nodes = source.GetComponentsInChildren<JourneyLevelNode>(true);
        if (nodes.Length != 6) return;
        bool needsUpgrade = false;
        for (int i = 0; i < nodes.Length; i++)
            if (nodes[i].LevelNumber != i + 1) needsUpgrade = true;
        if (!needsUpgrade) return;

        GameObject editing = PrefabUtility.LoadPrefabContents(path);
        try
        {
            JourneyLevelNode[] editableNodes = editing.GetComponentsInChildren<JourneyLevelNode>(true);
            for (int i = 0; i < editableNodes.Length; i++)
            {
                JourneyLevelNode node = editableNodes[i];
                node.ConfigureInEditor(i + 1);
                PrefabUtility.RecordPrefabInstancePropertyModifications(node);
                foreach (Text label in node.GetComponentsInChildren<Text>(true))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(label);
            }
            if (PrefabUtility.SaveAsPrefabAsset(editing, path) == null)
                throw new InvalidOperationException("Could not save level-node labels.");
            Debug.Log("[Journey] Fixed prefab level numbers and lock labels for Edit Mode.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[Journey] Node prefab upgrade failed: " + ex);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(editing);
        }
    }
}
