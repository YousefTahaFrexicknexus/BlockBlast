using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class JourneyPresentationUpgrade
{
    private const string PrefabPath = "Assets/MagicalWorlds/Prefabs/Journey/JourneyGameplayRoot.prefab";

    static JourneyPresentationUpgrade()
    {
        EditorApplication.delayCall += Apply;
    }

    [MenuItem("Tools/Magical Worlds/Journey/Repair Journey HUD Layers")]
    public static void Apply()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += Apply;
            return;
        }

        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (asset == null) return;

        GameObject editing = PrefabUtility.LoadPrefabContents(PrefabPath);
        bool changed = false;
        try
        {
            JourneySession journey = editing.GetComponent<JourneySession>();
            BlockBlastPrototype gameplay = editing.GetComponentInChildren<BlockBlastPrototype>(true);
            if (journey == null || gameplay == null)
                throw new InvalidOperationException("Journey gameplay prefab is missing its controllers.");

            Transform hud = editing.transform.Find("Journey HUD Canvas");
            Transform header = hud != null ? hud.Find("01 - Level Objective and Moves") : null;
            if (header == null) throw new InvalidOperationException("Journey objective header missing.");
            SerializedObject serialized = new SerializedObject(journey);
            SerializedProperty hudRoot = serialized.FindProperty("hudRoot");
            if (hudRoot == null) throw new InvalidOperationException("Journey hudRoot serialized field missing.");
            if (hudRoot.objectReferenceValue != header.gameObject)
            {
                hudRoot.objectReferenceValue = header.gameObject;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
            Transform progress = header.Find("Goal Progress");
            if (progress != null)
            {
                RectTransform rect = progress.GetComponent<RectTransform>();
                Vector2 desiredPosition = new Vector2(-175f, 750f);
                Vector2 desiredSize = new Vector2(360f, 65f);
                if (rect.anchoredPosition != desiredPosition || rect.sizeDelta != desiredSize)
                {
                    rect.anchoredPosition = desiredPosition;
                    rect.sizeDelta = desiredSize;
                    changed = true;
                }
            }
            Canvas canvas = hud != null ? hud.GetComponent<Canvas>() : null;
            if (canvas == null) throw new InvalidOperationException("Journey HUD Canvas missing.");
            if (canvas.sortingOrder <= 50)
            {
                canvas.sortingOrder = 80;
                EditorUtility.SetDirty(canvas);
                changed = true;
            }

            Transform classic = gameplay.transform.Find("Block Blast Canvas");
            if (classic == null) throw new InvalidOperationException("Shared Classic Canvas missing.");

            Transform oldHeader = classic.Find("02 - Header and Score");
            if (oldHeader == null) throw new InvalidOperationException("Classic header group is missing.");
            if (oldHeader.gameObject.activeSelf)
            {
                oldHeader.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(oldHeader.gameObject);
                changed = true;
            }
            if (changed)
            {
                if (PrefabUtility.SaveAsPrefabAsset(editing, PrefabPath) == null)
                    throw new InvalidOperationException("Journey UI prefab could not be saved.");
                Debug.Log("[Journey Presentation] Authored prefab now hides the Classic-only header and layers Journey HUD above the gameplay board.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[Journey Presentation] FAILED: " + ex);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(editing);
        }
    }
}
