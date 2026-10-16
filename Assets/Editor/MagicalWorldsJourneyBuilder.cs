using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MagicalWorlds.Journey;

[InitializeOnLoad]
public static class MagicalWorldsJourneyBuilder
{
    public const string Prefabs = "Assets/MagicalWorlds/Prefabs/Journey";
    public const string ResourcesFolder = "Assets/Resources/MagicalWorlds/Journey";
    public const string CatalogPath = ResourcesFolder + "/LevelCatalog.asset";
    public const string MapScene = "Assets/Scenes/JourneyMap.unity";
    public const string PlayScene = "Assets/Scenes/JourneyGameplay.unity";
    public const string MapPrefab = Prefabs + "/JourneyMapRoot.prefab";
    public const string PlayPrefab = Prefabs + "/JourneyGameplayRoot.prefab";
    public const string NodePrefab = Prefabs + "/JourneyLevelNode.prefab";
    public const string ClassicPrefab = "Assets/MagicalWorlds/Prefabs/Classic/ClassicGame.prefab";
    public const string Backdrop = "Assets/MagicalWorlds/Art/EnchantedGarden/Menu/Menu_Background.png";

    static MagicalWorldsJourneyBuilder() { EditorApplication.delayCall += AutoGenerate; }

    private static void AutoGenerate()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += AutoGenerate;
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(MapPrefab) != null) return;
        if (EditorPrefs.GetBool("MW.Journey.GenerationAttempt.v1", false)) return;
        EditorPrefs.SetBool("MW.Journey.GenerationAttempt.v1", true);
        try { Generate(); }
        catch (Exception ex) { Debug.LogError("[Journey Builder] Generation failed: " + ex); }
    }

    [MenuItem("Tools/Magical Worlds/Journey/Generate Scenes and Prefabs")]
    public static void GenerateFromMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(MapPrefab) != null)
        {
            EditorUtility.DisplayDialog("Journey Exists", "Existing Journey prefabs are protected; they won't be overwritten.", "OK");
            return;
        }
        Generate();
    }

    [MenuItem("Tools/Magical Worlds/Journey/Open Journey Map")]
    public static void OpenMap()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(MapScene);
    }

    [MenuItem("Tools/Magical Worlds/Journey/Validate Journey")]
    public static void Validate()
    {
        JourneyLevelCatalog catalog = AssetDatabase.LoadAssetAtPath<JourneyLevelCatalog>(CatalogPath);
        Check(catalog != null && catalog.Count == 50, "Fifty Journey level assets expected.");
        for (int i = 1; i <= catalog.Count; i++)
            Check(catalog.Get(i) != null && catalog.Get(i).levelNumber == i, "Missing level " + i);
        GameObject map = AssetDatabase.LoadAssetAtPath<GameObject>(MapPrefab);
        GameObject play = AssetDatabase.LoadAssetAtPath<GameObject>(PlayPrefab);
        Check(map != null && play != null, "Journey root prefabs missing");
        Check(AssetDatabase.LoadAssetAtPath<SceneAsset>(MapScene) != null &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(PlayScene) != null, "Journey scenes missing");
        Check(map.GetComponent<JourneyMapController>() != null, "Map controller missing");
        Check(play.GetComponent<JourneySession>() != null &&
            play.GetComponentInChildren<BlockBlastPrototype>(true) != null, "Gameplay components missing");
        Check(map.GetComponentsInChildren<JourneyLevelNode>(true).Length == catalog.Count, "Journey node count mismatch");
        Debug.Log("[Journey QA] PASSED: 50 levels, paged map and gameplay scenes, prefab nodes, shared Classic board.");
    }

    public static void Generate()
    {
        Folder(Prefabs);
        Folder(ResourcesFolder);
        JourneyEditorUI.Setup();
        JourneyCatalogCreator.Ensure(); JourneyFiftyLevelAuthoring.EnsureCatalog(); JourneyLevelCatalog catalog = AssetDatabase.LoadAssetAtPath<JourneyLevelCatalog>(CatalogPath);
        Sprite backdrop = AssetDatabase.LoadAssetAtPath<Sprite>(Backdrop);
        Check(backdrop != null, "Approved background sprite missing");
        Check(AssetDatabase.LoadAssetAtPath<GameObject>(ClassicPrefab) != null, "Classic prefab missing");
        JourneyMapEditor.Build(catalog, backdrop);
        JourneyGameplayEditor.Build();
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/ClassicGameplay.unity", true),
            new EditorBuildSettingsScene(MapScene, true),
            new EditorBuildSettingsScene(PlayScene, true),
            new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true)
        };
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("[Journey Builder] Journey scenes and prefabs generated.");
    }

    private static void Folder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    public static void SaveScene(Scene scene, GameObject root, string prefabPath, string scenePath)
    {
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Check(saved != null, "Could not save Journey prefab");
        UnityEngine.Object.DestroyImmediate(root);
        PrefabUtility.InstantiatePrefab(saved, scene);
        EditorSceneManager.MarkSceneDirty(scene);
        Check(EditorSceneManager.SaveScene(scene, scenePath), "Could not save Journey scene");
    }

    public static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
