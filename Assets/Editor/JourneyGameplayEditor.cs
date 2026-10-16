using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class JourneyGameplayEditor
{
    public static void Build()
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
        try
        {
            EditorSceneManager.SetActiveScene(scene);
            GameObject root = new GameObject("Journey - Gameplay", typeof(JourneySession));
            JourneySession session = root.GetComponent<JourneySession>();
            GameObject classic = AssetDatabase.LoadAssetAtPath<GameObject>(MagicalWorldsJourneyBuilder.ClassicPrefab);
            GameObject gameplay = (GameObject)PrefabUtility.InstantiatePrefab(classic, scene);
            gameplay.name = "Classic Board - Shared Rules";
            gameplay.transform.SetParent(root.transform, false);
            gameplay.transform.localScale = Vector3.one;

            RectTransform canvas = JourneyEditorUI.Canvas(root.transform, "Journey HUD Canvas",80);
            RectTransform header = JourneyEditorUI.Group("01 - Level Objective and Moves", canvas);
            RectTransform popupParent = JourneyEditorUI.Group("02 - Result Overlay", canvas);

            JourneyEditorUI.Panel("Objective Background", header,
                new Vector2(0,799), new Vector2(1080,298), JourneyEditorUI.Hex("#233468"));
            Text levelTitle = JourneyEditorUI.Text("Level Title", header, "LEVEL 1",
                new Vector2(0,898), new Vector2(910,65),42,JourneyEditorUI.Gold);
            Text objective = JourneyEditorUI.Text("Level Objective", header, "SCORE 12 POINTS",
                new Vector2(0,837), new Vector2(845,62),35,Color.white);
            Text progress = JourneyEditorUI.Text("Goal Progress", header, "0 / 12",
                new Vector2(-175,750), new Vector2(360,65),35,JourneyEditorUI.Hex("#C6FFE7"));
            Text moves = JourneyEditorUI.Text("Moves Remaining", header, "MOVES 8",
                new Vector2(290,750), new Vector2(340,68),43,JourneyEditorUI.Gold);
            JourneyEditorUI.Panel("Progress Track", header, new Vector2(0,681),
                new Vector2(828,23), JourneyEditorUI.Hex("#101D4A"));
            RectTransform bar = JourneyEditorUI.Panel("Progress Fill", header, new Vector2(0,681),
                new Vector2(828,23), JourneyEditorUI.Hex("#66D5C2"));
            Image fill = bar.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            Button back = JourneyEditorUI.Button("Level Map Button", header, "MAP",
                new Vector2(-455,748), new Vector2(150,95), JourneyEditorUI.Hex("#5554AC"),30);

            RectTransform result = JourneyEditorUI.Panel("Journey Result Overlay", popupParent,
                Vector2.zero, new Vector2(1080,1920), new Color(.02f,.025f,.1f,.92f),true);
            RectTransform card = JourneyEditorUI.Panel("Result Card", result,
                Vector2.zero, new Vector2(920,890), JourneyEditorUI.Hex("#293678"));
            JourneyEditorUI.Text("Ornament", card, "MAGICAL WORLDS", new Vector2(0,320),
                new Vector2(840,62),40,JourneyEditorUI.Gold);
            Text title = JourneyEditorUI.Text("Result Title", card, "LEVEL COMPLETE!",
                new Vector2(0,207), new Vector2(830,98),62,JourneyEditorUI.Gold);
            Text description = JourneyEditorUI.Text("Result Description", card, "",
                new Vector2(0,70), new Vector2(824,172),38,Color.white);
            Text stars = JourneyEditorUI.Text("Earned Stars", card, "",
                new Vector2(0,-82), new Vector2(760,92),69,JourneyEditorUI.Gold);
            Button next = JourneyEditorUI.Button("Next Level Button", card, "NEXT LEVEL",
                new Vector2(0,-195), new Vector2(660,102),JourneyEditorUI.Hex("#8056C2"),42);
            Text nextLabel = next.GetComponentInChildren<Text>();
            Button rewarded = JourneyEditorUI.Button("Rewarded Continue", card,
                "WATCH AD +3 MOVES", new Vector2(0,-195), new Vector2(660,102),
                JourneyEditorUI.Hex("#8A65C4"), 34);
            rewarded.interactable = false; // Enable only after verified rewarded-ad integration.
            Button retry = JourneyEditorUI.Button("Retry Level Button", card, "RETRY",
                new Vector2(-205,-326), new Vector2(370,95),JourneyEditorUI.Hex("#347DC3"),36);
            Button returnButton = JourneyEditorUI.Button("Level Map Button", card, "LEVEL MAP",
                new Vector2(205,-326), new Vector2(370,95),JourneyEditorUI.Hex("#5554AC"),36);

            JourneyEditorUI.Assign(session, "hudRoot", header.gameObject);
            JourneyEditorUI.Assign(session, "levelTitle", levelTitle);
            JourneyEditorUI.Assign(session, "objectiveText", objective);
            JourneyEditorUI.Assign(session, "progressText", progress);
            JourneyEditorUI.Assign(session, "movesText", moves);
            JourneyEditorUI.Assign(session, "progressFill", fill);
            JourneyEditorUI.Assign(session, "mapButton", back);
            JourneyEditorUI.Assign(session, "resultPanel", result.gameObject);
            JourneyEditorUI.Assign(session, "resultTitle", title);
            JourneyEditorUI.Assign(session, "resultDescription", description);
            JourneyEditorUI.Assign(session, "starsText", stars);
            JourneyEditorUI.Assign(session, "retryButton", retry);
            JourneyEditorUI.Assign(session, "nextButton", next);
            JourneyEditorUI.Assign(session, "returnButton", returnButton);
            JourneyEditorUI.Assign(session, "nextButtonLabel", nextLabel);
            JourneyEditorUI.Assign(session, "rewardedContinueButton", rewarded);
            JourneyEditorUI.Assign(session, "rewardedContinueLabel", rewarded.GetComponentInChildren<Text>());

            result.gameObject.SetActive(false);
            JourneyEditorUI.EventSystem();
            MagicalWorldsJourneyBuilder.SaveScene(scene, root,
                MagicalWorldsJourneyBuilder.PlayPrefab, MagicalWorldsJourneyBuilder.PlayScene);
        }
        finally
        {
            EditorSceneManager.SetActiveScene(previous);
            if (scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
