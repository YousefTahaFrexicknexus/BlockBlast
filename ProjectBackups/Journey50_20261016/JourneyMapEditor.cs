using MagicalWorlds.Journey;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class JourneyMapEditor
{
    public static void Build(JourneyLevelCatalog catalog, Sprite background)
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
        try
        {
            EditorSceneManager.SetActiveScene(scene);
            GameObject root = new GameObject("Journey - Level Map", typeof(JourneyMapController));
            JourneyMapController controller = root.GetComponent<JourneyMapController>();
            RectTransform canvas = JourneyEditorUI.Canvas(root.transform, "Journey Map Canvas");
            RectTransform backdrop = JourneyEditorUI.Group("01 - Enchanted Garden Background", canvas);
            RectTransform header = JourneyEditorUI.Group("02 - Map Header", canvas);
            RectTransform path = JourneyEditorUI.Group("03 - Level Path", canvas);
            RectTransform footer = JourneyEditorUI.Group("04 - Navigation", canvas);
            JourneyEditorUI.Art("Approved Fantasy Garden", backdrop, background);
            JourneyEditorUI.Panel("Readable Twilight Tint", backdrop, Vector2.zero,
                new Vector2(1080,1920), new Color(.02f,.03f,.11f,.42f));

            JourneyEditorUI.Text("Journey Heading", header, "JOURNEY", new Vector2(0,800),
                new Vector2(920,125), 86, JourneyEditorUI.Gold);
            Text progress = JourneyEditorUI.Text("Collection Progress", header,
                "0 / 6 LEVELS COMPLETE", new Vector2(0,693), new Vector2(820,68),
                34, Color.white);

            JourneyLevelNode[] nodes = new JourneyLevelNode[catalog.Count];
            GameObject nodePrefab = null;
            for (int i = 0; i < catalog.Count; i++)
            {
                int column = i % 2;
                int row = i / 2;
                Vector2 position = new Vector2(column == 0 ? -225 : 225, 458 - row * 400);
                JourneyLevelNode node;
                if (i == 0)
                {
                    node = MakeNode(path, position);
                    PrefabUtility.SaveAsPrefabAssetAndConnect(node.gameObject,
                        MagicalWorldsJourneyBuilder.NodePrefab, InteractionMode.AutomatedAction);
                    nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MagicalWorldsJourneyBuilder.NodePrefab);
                }
                else
                {
                    GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(nodePrefab, scene);
                    item.transform.SetParent(path, false);
                    item.GetComponent<RectTransform>().anchoredPosition = position;
                    node = item.GetComponent<JourneyLevelNode>();
                }
                nodes[i] = node;
            }

            Button back = JourneyEditorUI.Button("Back to Main Menu", footer, "MAIN MENU",
                new Vector2(0,-797), new Vector2(530,116), JourneyEditorUI.Hex("#4A53AC"),42);
            JourneyEditorUI.Assign(controller, "backButton", back);
            JourneyEditorUI.Assign(controller, "progressLabel", progress);
            JourneyEditorUI.AssignArray(controller, "levelNodes", nodes);
            JourneyEditorUI.EventSystem();
            MagicalWorldsJourneyBuilder.SaveScene(scene, root,
                MagicalWorldsJourneyBuilder.MapPrefab, MagicalWorldsJourneyBuilder.MapScene);
        }
        finally
        {
            EditorSceneManager.SetActiveScene(previous);
            if (scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static JourneyLevelNode MakeNode(Transform parent, Vector2 position)
    {
        RectTransform rect = JourneyEditorUI.Panel("Journey Level Node", parent, position,
            new Vector2(333,302), JourneyEditorUI.Hex("#3A428D"),true);
        Outline border = rect.gameObject.AddComponent<Outline>();
        border.effectColor = JourneyEditorUI.Gold;
        border.effectDistance = new Vector2(3,-3);

        Button select = rect.gameObject.AddComponent<Button>();
        Text number = JourneyEditorUI.Text("Level Number", rect, "1",
            new Vector2(0,66), new Vector2(305,110),78,JourneyEditorUI.Gold);
        Text stars = JourneyEditorUI.Text("Stars", rect, "",
            new Vector2(0,-25), new Vector2(310,70),48,JourneyEditorUI.Gold);
        Text state = JourneyEditorUI.Text("Level Status", rect, "PLAY",
            new Vector2(0,-109), new Vector2(300,55),31,Color.white);
        JourneyLevelNode node = rect.gameObject.AddComponent<JourneyLevelNode>();
        JourneyEditorUI.Assign(node, "selectButton", select);
        JourneyEditorUI.Assign(node, "numberText", number);
        JourneyEditorUI.Assign(node, "starsText", stars);
        JourneyEditorUI.Assign(node, "stateText", state);
        return node;
    }
}
