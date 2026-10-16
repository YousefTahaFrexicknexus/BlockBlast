using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class JourneyResultPrefabUpgrade
{
    static JourneyResultPrefabUpgrade() { EditorApplication.delayCall += UpgradeWhenReady; }

    private static void UpgradeWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += UpgradeWhenReady;
            return;
        }

        string path = MagicalWorldsJourneyBuilder.PlayPrefab;
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (source == null) return;
        JourneySession check = source.GetComponent<JourneySession>();
        SerializedObject serialized = new SerializedObject(check);
        SerializedProperty existing = serialized.FindProperty("rewardedContinueButton");
        if (existing == null || existing.objectReferenceValue != null) return;

        GameObject editing = PrefabUtility.LoadPrefabContents(path);
        try
        {
            JourneyEditorUI.Setup();
            Transform card = FindDeep(editing.transform, "Result Card");
            if (card == null) throw new Exception("Journey Result Card missing");
            Button rewarded = JourneyEditorUI.Button("Rewarded Continue", card,
                "WATCH AD +3 MOVES", new Vector2(0,-195), new Vector2(660,102),
                JourneyEditorUI.Hex("#8A65C4"), 34);
            rewarded.interactable = false;
            JourneySession session = editing.GetComponent<JourneySession>();
            JourneyEditorUI.Assign(session, "rewardedContinueButton", rewarded);
            JourneyEditorUI.Assign(session, "rewardedContinueLabel",
                rewarded.GetComponentInChildren<Text>());
            if (PrefabUtility.SaveAsPrefabAsset(editing, path) == null)
                throw new Exception("Could not save Journey result prefab upgrade");
            Debug.Log("[Journey] Added visible but disabled +3-move rewarded continue placeholder.");
        }
        catch (Exception ex) { Debug.LogError("[Journey] Rewarded UI upgrade failed: " + ex); }
        finally { PrefabUtility.UnloadPrefabContents(editing); }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
