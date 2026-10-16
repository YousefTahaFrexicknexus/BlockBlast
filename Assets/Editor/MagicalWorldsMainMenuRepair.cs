using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MagicalWorldsMainMenuRepair
{
    private const string MenuPrefab = "Assets/MagicalWorlds/Prefabs/MainMenu/MainMenu.prefab";
    private const string MenuScene = "Assets/Scenes/MainMenu.unity";
    private const string OpenOnceKey = "MagicalWorlds.MainMenu.AutoOpened.v1";

    static MagicalWorldsMainMenuRepair()
    {
        EditorApplication.delayCall += RepairAndValidate;
    }

    private static void EnsureCurrencyBindings()
    {
        const string path = "Assets/MagicalWorlds/Prefabs/MainMenu/MainMenu.prefab";
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null)
        {
            return;
        }

        MagicalWorldsMenuController controller = root.GetComponent<MagicalWorldsMenuController>();
        SerializedObject view = new SerializedObject(controller);
        if (view.FindProperty("coinsAddButton").objectReferenceValue != null &&
            view.FindProperty("gemsAddButton").objectReferenceValue != null &&
            view.FindProperty("energyAddButton").objectReferenceValue != null)
        {
            return;
        }

        GameObject editable = PrefabUtility.LoadPrefabContents(path);
        try
        {
            SerializedObject serialized = new SerializedObject(editable.GetComponent<MagicalWorldsMenuController>());
            SetButton(serialized, "coinsAddButton", editable.transform, "Plus - Coins");
            SetButton(serialized, "gemsAddButton", editable.transform, "Plus - Gems");
            SetButton(serialized, "energyAddButton", editable.transform, "Plus - Energy");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(editable, path);
            Debug.Log("[Magical Worlds Main Menu] Serialized Shop links for three currency + buttons.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(editable);
        }
    }

    private static void SetButton(SerializedObject serialized, string propertyName, Transform parent, string objectName)
    {
        Transform child = FindDeep(parent, objectName);
        if (child == null)
        {
            throw new InvalidOperationException("Missing currency + button: " + objectName);
        }
        SerializedProperty field = serialized.FindProperty(propertyName);
        field.objectReferenceValue = child.GetComponent<Button>();
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name)
            {
                return child;
            }
            Transform found = FindDeep(child, name);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
    private static void RepairAndValidate()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += RepairAndValidate;
            return;
        }
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefab);
        if (prefab == null)
        {
            return;
        }

        try
        {
            Canvas canvas = prefab.GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                throw new InvalidOperationException("Main Menu Canvas missing from prefab.");
            }
            if (canvas.transform.localScale != Vector3.one)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(MenuPrefab);
                try
                {
                    Canvas authored = contents.GetComponentInChildren<Canvas>(true);
                    authored.transform.localScale = Vector3.one;
                    PrefabUtility.SaveAsPrefabAsset(contents, MenuPrefab);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
                Debug.Log("[Magical Worlds Main Menu] Authored Canvas scale repaired.");
            }

            EnsureCurrencyBindings();
            MagicalWorldsMainMenuBuilder.Validate();

            if (!EditorPrefs.GetBool(OpenOnceKey, false) &&
                SceneManager.GetActiveScene().path == "Assets/Scenes/ClassicGameplay.unity" &&
                !SceneManager.GetActiveScene().isDirty &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScene) != null)
            {
                EditorPrefs.SetBool(OpenOnceKey, true);
                EditorSceneManager.OpenScene(MenuScene);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[Magical Worlds Main Menu] Repair or QA failed: " + ex);
        }
    }
}
