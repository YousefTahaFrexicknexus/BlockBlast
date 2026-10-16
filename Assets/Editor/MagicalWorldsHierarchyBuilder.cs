using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MagicalWorldsHierarchyBuilder
{
    private const string PrefabFolder = "Assets/MagicalWorlds/Prefabs/Classic";
    private const string ScenePath = "Assets/Scenes/ClassicGameplay.unity";
    private const string RootPrefabPath = PrefabFolder + "/ClassicGame.prefab";
    private const string RoundedSpritePath = "Assets/Resources/MagicalWorlds/UI/RoundedPanel.png";
    private const string AutoAttemptKey = "MagicalWorlds.Hierarchy.FirstGenerateAttempt.v1";

    static MagicalWorldsHierarchyBuilder()
    {
        EditorApplication.delayCall += TryInitialGeneration;
    }

    private static void TryInitialGeneration()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryInitialGeneration;
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath) != null)
        {
            return;
        }
        if (EditorPrefs.GetBool(AutoAttemptKey, false))
        {
            return;
        }
        EditorPrefs.SetBool(AutoAttemptKey, true);
        try
        {
            Generate();
        }
        catch (Exception ex)
        {
            Debug.LogError("[Magical Worlds Hierarchy] Automatic prefab generation failed. Use Tools > Magical Worlds > Hierarchy > Generate Classic Prefabs. " + ex);
        }
    }

    [MenuItem("Tools/Magical Worlds/Hierarchy/Generate Classic Prefabs and Scene")]
    private static void GenerateFromMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath) != null)
        {
            EditorUtility.DisplayDialog("Classic UI Already Created",
                "The Classic Game prefab already exists. To protect your authored edits, this tool does not overwrite existing prefabs. Use the existing ClassicGameplay scene.", "OK");
            return;
        }
        Generate();
    }

    [MenuItem("Tools/Magical Worlds/Hierarchy/Open Classic Gameplay Scene")]
    private static void OpenScene()
    {
        if (!File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Classic Scene Missing", "Run Generate Classic Prefabs and Scene first.", "OK");
            return;
        }
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(ScenePath);
        }
    }

    private static void Generate()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder("Assets/Resources/MagicalWorlds/UI");
        Sprite rounded = CreateRoundedSpriteAsset();

        Scene original = SceneManager.GetActiveScene();
        bool canOpen = original.path == "Assets/Scenes/SampleScene.unity" && !original.isDirty;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);

        try
        {
            EditorSceneManager.SetActiveScene(scene);
            GameObject input = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            input.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            GameObject root = new GameObject("Classic Game", typeof(BlockBlastPrototype));
            BlockBlastPrototype prototype = root.GetComponent<BlockBlastPrototype>();
            prototype.BuildEditorVisualLayout(rounded);
            RectTransform canvas = root.transform.Find("Block Blast Canvas").GetComponent<RectTransform>();
            OrganizeCanvas(canvas);

            // Save source components first, then named layout groups as nested
            // prefab instances, and finally the game root as a reusable prefab.
            SaveNested(canvas, "8 x 8 Grid", "GameBoard_8x8");
            SaveNested(canvas, "Piece Tray 1", "PieceTray_01");
            SaveNested(canvas, "Piece Tray 2", "PieceTray_02");
            SaveNested(canvas, "Piece Tray 3", "PieceTray_03");
            SaveNested(canvas, "Sound Toggle", "Button_Sound");
            SaveNested(canvas, "Haptics Toggle", "Button_Haptics");
            SaveNested(canvas, "Pause", "Button_Pause");
            SaveNested(canvas, "Restart", "Button_Restart");
            SaveNested(canvas, "Game Over Overlay", "Popup_GameOver");
            SaveNested(canvas, "Dialog Overlay", "Popup_Dialog");

            string[] groups = { "01 - Background", "02 - Header and Score", "03 - Board", "04 - Controls and Trays", "05 - Dialogs" };
            foreach (string group in groups)
            {
                SaveNested(canvas, group, "Section_" + group.Substring(0, 2));
            }

            BlockBlastUIReferences references = canvas.gameObject.AddComponent<BlockBlastUIReferences>();
            references.RebuildReferences();
            if (!references.IsComplete)
            {
                throw new InvalidOperationException("Generated Classic UI Inspector references are incomplete.");
            }
            GameObject gamePrefab = PrefabUtility.SaveAsPrefabAsset(root, RootPrefabPath);
            if (gamePrefab == null)
            {
                throw new InvalidOperationException("Unity failed to save the Classic Game prefab.");
            }
            UnityEngine.Object.DestroyImmediate(root);
            GameObject sceneInstance = (GameObject)PrefabUtility.InstantiatePrefab(gamePrefab, scene);
            sceneInstance.name = "Classic Game";

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Unable to save ClassicGameplay.unity.");
            }
            EnableSceneInBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[Magical Worlds Hierarchy] Authored ClassicGameplay scene and nested prefabs created. Scene uses a real editable Canvas at 1080x1920.");

            EditorSceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(scene, true);

            if (canOpen && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
        }
        catch
        {
            if (scene.isLoaded)
            {
                EditorSceneManager.SetActiveScene(original);
                EditorSceneManager.CloseScene(scene, true);
            }
            throw;
        }
    }

    private static void OrganizeCanvas(RectTransform canvas)
    {
        Transform background = CreateGroup(canvas, "01 - Background");
        Transform hud = CreateGroup(canvas, "02 - Header and Score");
        Transform board = CreateGroup(canvas, "03 - Board");
        Transform controls = CreateGroup(canvas, "04 - Controls and Trays");
        Transform dialogs = CreateGroup(canvas, "05 - Dialogs");

        Move(canvas, background, "Background", "Top Glow");
        Move(canvas, hud, "Title", "Subtitle", "Sound Toggle", "Haptics Toggle",
            "Score Caption", "Score", "Best Caption", "Best");
        Move(canvas, board, "Board Shadow", "8 x 8 Grid");
        Move(canvas, controls, "Combo", "Pause", "Piece Tray 1", "Piece Tray 2", "Piece Tray 3", "Instruction");
        Move(canvas, dialogs, "Game Over Overlay", "Dialog Overlay");
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1080, 1920);
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    private static void Move(Transform parent, Transform group, params string[] names)
    {
        foreach (string name in names)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                throw new InvalidOperationException("Cannot organize missing UI GameObject: " + name);
            }
            child.SetParent(group, false);
        }
    }

    private static void SaveNested(Transform canvas, string name, string filename)
    {
        Transform child = FindDeep(canvas, name);
        if (child == null)
        {
            throw new InvalidOperationException("Cannot create prefab for missing GameObject: " + name);
        }
        string path = PrefabFolder + "/" + filename + ".prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(child.gameObject, path, InteractionMode.AutomatedAction);
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

    private static void EnableSceneInBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        foreach (EditorBuildSettingsScene old in EditorBuildSettings.scenes)
        {
            if (old.path != ScenePath)
            {
                scenes.Add(old);
            }
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static Sprite CreateRoundedSpriteAsset()
    {
        if (!File.Exists(RoundedSpritePath))
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - 31.5f) - 18f, 0);
                    float dy = Mathf.Max(Mathf.Abs(y - 31.5f) - 18f, 0);
                    float coverage = Mathf.Clamp01(14f - Mathf.Sqrt(dx * dx + dy * dy) + 0.6f);
                    pixels[y * size + x] = new Color(1, 1, 1, coverage);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(RoundedSpritePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(RoundedSpritePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(RoundedSpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(14, 14, 14, 14);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        if (sprite == null)
        {
            throw new InvalidOperationException("Failed to create editable rounded UI sprite asset.");
        }
        return sprite;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}