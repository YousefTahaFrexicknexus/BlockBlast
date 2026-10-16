using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MagicalWorldsMainMenuBuilder
{
    private const string ArtFolder = "Assets/MagicalWorlds/Art/EnchantedGarden/Menu/";
    private const string PrefabFolder = "Assets/MagicalWorlds/Prefabs/MainMenu";
    private const string PrefabPath = PrefabFolder + "/MainMenu.prefab";
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string AttemptKey = "MagicalWorlds.MainMenu.Generated.v1";
    private static Font font;

    static MagicalWorldsMainMenuBuilder()
    {
        EditorApplication.delayCall += GenerateOnce;
    }

    private static void GenerateOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += GenerateOnce;
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }
        if (EditorPrefs.GetBool(AttemptKey, false))
        {
            return;
        }

        EditorPrefs.SetBool(AttemptKey, true);
        try
        {
            Generate();
        }
        catch (Exception ex)
        {
            Debug.LogError("[Magical Worlds Main Menu] Auto-generation failed: " + ex);
        }
    }

    [MenuItem("Tools/Magical Worlds/Main Menu/Generate Main Menu Prefabs and Scene")]
    public static void GenerateFromMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            EditorUtility.DisplayDialog("Main Menu Already Exists",
                "Your MainMenu prefab already exists. This tool will not overwrite your Inspector edits.", "OK");
            return;
        }
        Generate();
    }

    [MenuItem("Tools/Magical Worlds/Main Menu/Open Main Menu Scene")]
    public static void OpenMenuScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            EditorUtility.DisplayDialog("Main Menu Missing", "Generate the Main Menu first.", "OK");
            return;
        }
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(ScenePath);
        }
    }

    [MenuItem("Tools/Magical Worlds/Main Menu/Validate Main Menu")]
    public static void Validate()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException("MainMenu.prefab is missing.");
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            throw new InvalidOperationException("MainMenu.unity is missing.");
        }

        MagicalWorldsMenuController controller = prefab.GetComponent<MagicalWorldsMenuController>();
        Require(controller != null, "Menu controller missing");
        Canvas canvas = prefab.GetComponentInChildren<Canvas>(true);
        Require(canvas != null, "Menu Canvas missing");
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        Require(scaler != null && scaler.referenceResolution == new Vector2(1080, 1920), "Invalid CanvasScaler reference resolution");
        Require(canvas.transform.localScale == Vector3.one, "Canvas scale should be 1");
        Require(FindDeep(canvas.transform, "Button_Play") != null, "Play Button missing");
        Require(FindDeep(canvas.transform, "Button_Journey") != null, "Journey Button missing");
        Require(FindDeep(canvas.transform, "Navigation_Shop") != null, "Shop missing");
        Require(FindDeep(canvas.transform, "Navigation_Skins") != null, "Skins missing");
        Require(FindDeep(canvas.transform, "Navigation_Events") != null, "Events missing");
        Require(FindDeep(canvas.transform, "Navigation_Settings") != null, "Settings missing");
        Require(FindDeep(canvas.transform, "Popup_Frame") != null, "Shared popup missing");

        var serialized = new SerializedObject(controller);
        string[] fieldNames = {
            "playButton","journeyButton","shopButton","skinsButton","eventsButton",
            "settingsButton","headerSettingsButton","coinsAddButton","gemsAddButton","energyAddButton","coinsValue","gemsValue","energyValue",
            "popupRoot","popupTitle","popupDescription","popupCloseButton",
            "popupBackdropButton","popupSoundButton","popupHapticsButton",
            "popupSoundLabel","popupHapticsLabel"
        };
        foreach (string fieldName in fieldNames)
        {
            SerializedProperty property = serialized.FindProperty(fieldName);
            Require(property != null && property.objectReferenceValue != null, "Missing serialized menu field: " + fieldName);
        }
        Require(CountMissingScripts(prefab.transform) == 0, "One or more menu objects have missing scripts");
        Require(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/Button_Play.prefab") != null, "Reusable Play prefab missing");

        Debug.Log("[Magical Worlds Main Menu QA] Passed: scene, 1080x1920 Canvas, prefab controls, serialized navigation, popup, no missing scripts.");
    }

    private static void Generate()
    {
        EnsureFolder(PrefabFolder);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        SpriteAsset("Menu_Background.png");
        SpriteAsset("Logo_MagicalWorlds.png");
        SpriteAsset("Button_Play.png");
        SpriteAsset("Button_Journey.png");
        SpriteAsset("Header_Settings.png");
        SpriteAsset("Header_Coins_Blank.png");
        SpriteAsset("Header_Gems_Blank.png");
        SpriteAsset("Header_Energy_Blank.png");
        SpriteAsset("Navigation_Shop.png");
        SpriteAsset("Navigation_Skins.png");
        SpriteAsset("Navigation_Events.png");
        SpriteAsset("Navigation_Settings.png");

        Scene original = SceneManager.GetActiveScene();
        bool openAfter = original.isLoaded && !original.isDirty && !EditorApplication.isPlayingOrWillChangePlaymode;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);

        try
        {
            EditorSceneManager.SetActiveScene(scene);
            GameObject root = new GameObject("Magical Worlds - Main Menu", typeof(MagicalWorldsMenuController));
            MagicalWorldsMenuController menu = root.GetComponent<MagicalWorldsMenuController>();

            GameObject canvasGO = new GameObject("Canvas - Main Menu 1080x1920",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(root.transform, false);
            canvasGO.transform.localScale = Vector3.one;
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler canvasScaler = canvasGO.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            RectTransform backgrounds = Group("01 - Enchanted Garden Background", canvasRect);
            RectTransform top = Group("02 - Currency and Settings Bar", canvasRect);
            RectTransform logo = Group("03 - Magical Worlds Logo", canvasRect);
            RectTransform actions = Group("04 - Primary Navigation", canvasRect);
            RectTransform navigation = Group("05 - Bottom Navigation", canvasRect);
            RectTransform overlay = Group("06 - Popup Container", canvasRect);

            SpriteImage("Background - Enchanted Garden", backgrounds, "Menu_Background.png", 0, 0, 1080, 1920);
            SpriteImage("Logo_MagicalWorlds", logo, "Logo_MagicalWorlds.png", 177, 124, 708, 475);

            Button headerSettings = SpriteButton("Header_Settings", top, "Header_Settings.png", 13, 25, 123, 117);
            SpriteImage("Currency_Coins", top, "Header_Coins_Blank.png", 138, 21, 351, 120);
            SpriteImage("Currency_Gems", top, "Header_Gems_Blank.png", 492, 21, 284, 120);
            SpriteImage("Currency_Energy", top, "Header_Energy_Blank.png", 770, 21, 283, 120);
            Text coins = TextAt("Coins - Editable Value", top, 339, 81, 168, 72, "1,250", 37);
            Text gems = TextAt("Gems - Editable Value", top, 640, 81, 115, 72, "80", 37);
            Text energy = TextAt("Energy - Editable Value", top, 916, 81, 115, 72, "5/5", 36);

            Button play = SpriteButton("Button_Play", actions, "Button_Play.png", 173, 1164, 726, 291);
            Button journey = SpriteButton("Button_Journey", actions, "Button_Journey.png", 197, 1430, 684, 189);

            Button shop = SpriteButton("Navigation_Shop", navigation, "Navigation_Shop.png", 82, 1640, 213, 211);
            Button skins = SpriteButton("Navigation_Skins", navigation, "Navigation_Skins.png", 323, 1640, 214, 211);
            Button events = SpriteButton("Navigation_Events", navigation, "Navigation_Events.png", 539, 1631, 226, 221);
            Button settings = SpriteButton("Navigation_Settings", navigation, "Navigation_Settings.png", 774, 1640, 228, 211);

            // Separate hit targets: these are editable, real uGUI Buttons.
            Button coinPlus = InvisibleButton("Plus - Coins", top, 444, 82, 92, 93);
            Button gemPlus = InvisibleButton("Plus - Gems", top, 725, 82, 92, 93);
            Button energyPlus = InvisibleButton("Plus - Energy", top, 1016, 82, 92, 93);

            RectTransform popupRect = Panel("Popup Overlay", overlay, 0, 0, 1080, 1920, new Color(0.02f,0.025f,0.12f,0.84f));
            Button backdropButton = popupRect.gameObject.AddComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            RectTransform card = Panel("Popup_Frame", popupRect, 90, 545, 900, 805, Hex("#243365"));
            Image bg = card.GetComponent<Image>();
            bg.raycastTarget = true;
            Outline outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = Hex("#E8C3FF");
            outline.effectDistance = new Vector2(5,-5);
            Text title = TextAt("Popup_Title", card, 540, 645, 720, 105, "COMING SOON", 66, Hex("#FFF1B6"));
            Text desc = TextAt("Popup_Description", card, 540, 875, 710, 235, "More adventures are on the way.", 37, Color.white);
            Button soundButton = SolidButton("Popup_SoundToggle", card, 285, 1082, 380, 105, "SOUND  ON", Hex("#304990"), 33);
            Button hapticsButton = SolidButton("Popup_HapticsToggle", card, 754, 1082, 380, 105, "VIBRATION  OFF", Hex("#304990"), 32);
            Button closeButton = SolidButton("Popup_Close", card, 540, 1210, 420, 104, "BACK", Hex("#5C4BC6"), 40);
            Text soundText = soundButton.GetComponentInChildren<Text>();
            Text hapticText = hapticsButton.GetComponentInChildren<Text>();
            popupRect.gameObject.SetActive(false);

            Assign(menu, "playButton", play);
            Assign(menu, "journeyButton", journey);
            Assign(menu, "shopButton", shop);
            Assign(menu, "skinsButton", skins);
            Assign(menu, "eventsButton", events);
            Assign(menu, "settingsButton", settings);
            Assign(menu, "headerSettingsButton", headerSettings);
            Assign(menu, "coinsAddButton", coinPlus);
            Assign(menu, "gemsAddButton", gemPlus);
            Assign(menu, "energyAddButton", energyPlus);
            Assign(menu, "coinsValue", coins);
            Assign(menu, "gemsValue", gems);
            Assign(menu, "energyValue", energy);
            Assign(menu, "popupRoot", popupRect.gameObject);
            Assign(menu, "popupTitle", title);
            Assign(menu, "popupDescription", desc);
            Assign(menu, "popupCloseButton", closeButton);
            Assign(menu, "popupBackdropButton", backdropButton);
            Assign(menu, "popupSoundButton", soundButton);
            Assign(menu, "popupHapticsButton", hapticsButton);
            Assign(menu, "popupSoundLabel", soundText);
            Assign(menu, "popupHapticsLabel", hapticText);

            // Economy buttons lead to the Shop placeholder until implemented.
            coinPlus.onClick.AddListener(menu.ShowShop);
            gemPlus.onClick.AddListener(menu.ShowShop);
            energyPlus.onClick.AddListener(menu.ShowShop);

            SaveNested(canvasRect, "Button_Play", "Button_Play");
            SaveNested(canvasRect, "Button_Journey", "Button_Journey");
            SaveNested(canvasRect, "Navigation_Shop", "Navigation_Shop");
            SaveNested(canvasRect, "Navigation_Skins", "Navigation_Skins");
            SaveNested(canvasRect, "Navigation_Events", "Navigation_Events");
            SaveNested(canvasRect, "Navigation_Settings", "Navigation_Settings");
            SaveNested(canvasRect, "Currency_Coins", "Currency_Coins");
            SaveNested(canvasRect, "Currency_Gems", "Currency_Gems");
            SaveNested(canvasRect, "Currency_Energy", "Currency_Energy");
            SaveNested(canvasRect, "Popup Overlay", "Popup_Shared");
            SaveNested(canvasRect, "03 - Magical Worlds Logo", "Logo_Section");
            SaveNested(canvasRect, "02 - Currency and Settings Bar", "Header_Section");
            SaveNested(canvasRect, "04 - Primary Navigation", "PrimaryButtons_Section");
            SaveNested(canvasRect, "05 - Bottom Navigation", "BottomNavigation_Section");

            GameObject created = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (created == null)
            {
                throw new InvalidOperationException("Failed to save MainMenu.prefab.");
            }

            UnityEngine.Object.DestroyImmediate(root);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(created, scene);
            instance.name = "Magical Worlds - Main Menu";

            GameObject inputGO = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            inputGO.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save MainMenu.unity.");
            }
            SetBuildSettings();
            AssetDatabase.SaveAssets();

            EditorSceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(scene, true);
            Validate();

            Debug.Log("[Magical Worlds Main Menu] MainMenu scene and reusable editable prefabs created successfully.");

            if (openAfter)
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

    private static Sprite SpriteAsset(string name)
    {
        string path = ArtFolder + name;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("Menu art missing: " + path);
        }
        bool dirty = importer.textureType != TextureImporterType.Sprite ||
            importer.mipmapEnabled || importer.maxTextureSize < 2048;
        if (dirty)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            throw new InvalidOperationException("Could not import menu sprite: " + path);
        }
        return sprite;
    }

    private static RectTransform Group(string name, Transform parent)
    {
        RectTransform rect = Make(name, parent, 0, 0, 1080, 1920);
        return rect;
    }

    private static RectTransform Make(string name, Transform parent, float left, float top, float w, float h)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(left + w * .5f - 540f, 960f - (top + h * .5f));
        rect.sizeDelta = new Vector2(w, h);
        return rect;
    }

    private static RectTransform SpriteImage(string name, Transform parent, string sprite, float x, float y, float w, float h)
    {
        RectTransform rect = Make(name, parent, x, y, w, h);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = SpriteAsset(sprite);
        image.color = Color.white;
        image.raycastTarget = false;
        return rect;
    }

    private static Button SpriteButton(string name, Transform parent, string sprite, float x, float y, float w, float h)
    {
        RectTransform rect = SpriteImage(name, parent, sprite, x, y, w, h);
        Image image = rect.GetComponent<Image>();
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.93f, 0.93f, 1f, 1);
        colors.pressedColor = new Color(0.74f, 0.75f, 0.94f, 1);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
    }

    private static Text TextAt(string name, Transform parent, float centerX, float centerY, float width, float height,
        string text, int size, Color? color = null)
    {
        RectTransform rect = Make(name, parent, centerX - width * 0.5f, centerY - height * 0.5f, width, height);
        Text label = rect.gameObject.AddComponent<Text>();
        label.text = text;
        label.font = font;
        label.fontSize = size;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color ?? Color.white;
        label.raycastTarget = false;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 21;
        label.resizeTextMaxSize = size;
        Shadow shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(.03f, .05f, .2f, .85f);
        shadow.effectDistance = new Vector2(2, -3);
        return label;
    }

    private static Button SolidButton(string name, Transform parent, float centerX, float centerY,
        float width, float height, string label, Color background, int size)
    {
        RectTransform rect = Panel(name, parent, centerX - width * 0.5f, centerY - height * 0.5f, width, height, background);
        Button button = rect.gameObject.AddComponent<Button>();
        button.colors = ColorBlock.defaultColorBlock;
        TextAt("Label - " + label, rect, 540, 960, width - 20, height - 10, label, size);
        return button;
    }

    private static RectTransform Panel(string name, Transform parent, float x, float y, float w, float h, Color background)
    {
        RectTransform rect = Make(name, parent, x, y, w, h);
        Image img = rect.gameObject.AddComponent<Image>();
        img.color = background;
        img.raycastTarget = true;
        return rect;
    }

    private static Button InvisibleButton(string name, Transform parent, float centerX, float centerY, float w, float h)
    {
        RectTransform rect = Make(name, parent, centerX - w * .5f, centerY - h * .5f, w, h);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(1,1,1,0.001f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        return button;
    }

    private static void Assign(MagicalWorldsMenuController controller, string name, UnityEngine.Object target)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null)
        {
            throw new InvalidOperationException("Missing menu controller field: " + name);
        }
        property.objectReferenceValue = target;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SaveNested(RectTransform canvas, string name, string pathName)
    {
        Transform child = FindDeep(canvas, name);
        if (child == null)
        {
            throw new InvalidOperationException("Nested prefab target not found: " + name);
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(child.gameObject,
            PrefabFolder + "/" + pathName + ".prefab", InteractionMode.AutomatedAction);
    }

    private static Transform FindDeep(Transform transform, string name)
    {
        foreach (Transform child in transform)
        {
            if (child.name == name)
            {
                return child;
            }
            Transform result = FindDeep(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    private static int CountMissingScripts(Transform root)
    {
        int result = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root.gameObject);
        foreach (Transform child in root)
        {
            result += CountMissingScripts(child);
        }
        return result;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    private static void SetBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        const string classic = "Assets/Scenes/ClassicGameplay.unity";
        scenes.Add(new EditorBuildSettingsScene(classic, true));
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.path != ScenePath && scene.path != classic)
            {
                scenes.Add(scene);
            }
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolder(string path)
    {
        string[] folders = path.Split('/');
        string parent = folders[0];
        for (int i = 1; i < folders.Length; i++)
        {
            string next = parent + "/" + folders[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(parent, folders[i]);
            }
            parent = next;
        }
    }
}
