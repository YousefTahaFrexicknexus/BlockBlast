using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BlockBlastClone;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MagicalWorldsClassicPolishQA
{
    private const string ClassicPrefab = "Assets/MagicalWorlds/Prefabs/Classic/ClassicGame.prefab";
    private static readonly BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    static MagicalWorldsClassicPolishQA()
    {
        EditorApplication.delayCall += RunWhenReady;
    }

    private static void RunWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunWhenReady;
            return;
        }
        try
        {
            Run();
        }
        catch (Exception ex)
        {
            Debug.LogError("[Classic Polish QA] FAILED: " + ex);
        }
    }

    [MenuItem("Tools/Magical Worlds/Classic Gameplay/Validate Scoring Feedback")]
    public static void Run()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ClassicPrefab);
        Require(source != null, "Classic prefab not found");
        GameObject editing = PrefabUtility.LoadPrefabContents(ClassicPrefab);
        try
        {
            BlockBlastPrototype game = editing.GetComponent<BlockBlastPrototype>();
            BlockBlastUIReferences view = editing.GetComponentInChildren<BlockBlastUIReferences>(true);
            Require(game != null && view != null && view.IsComplete,
                "Missing Classic controller or serialized UI references");

            RectTransform canvas = view.GetComponent<RectTransform>();
            RectTransform board = view.Board;
            Set(game, "canvasRect", canvas);
            Set(game, "boardRect", board);
            Set(game, "feedbackText", view.Instruction);

            float lift = Get<float>(game, "floatingScoreLift");
            float rise = Get<float>(game, "floatingScoreRise");
            float duration = Get<float>(game, "floatingScoreDuration");
            Require(lift >= 0f && rise > 0f && duration > 0f,
                "Popup tuning values must be positive");

            List<Vector2Int> cornerA = new List<Vector2Int> { new Vector2Int(0, 0) };
            List<Vector2Int> cornerB = new List<Vector2Int> { new Vector2Int(7, 7) };
            Vector2 popupA = Anchor(game, cornerA);
            Vector2 popupB = Anchor(game, cornerB);
            Require(popupA.x < popupB.x - 300f && popupA.y > popupB.y + 300f,
                "Popup must move with placement location, not remain fixed on screen");

            Vector2 cell = (Vector2)typeof(BlockBlastPrototype)
                .GetMethod("CellPosition", PrivateInstance)
                .Invoke(game, new object[] { 0, 0 });
            Vector2 expected = (Vector2)canvas.InverseTransformPoint(board.TransformPoint(cell)) +
                Vector2.up * lift;
            expected.x = Mathf.Clamp(expected.x, -390f, 390f);
            expected.y = Mathf.Clamp(expected.y, -590f, 555f);
            Require((popupA - expected).sqrMagnitude < .01f,
                "Popup didn't match board-to-canvas coordinate transformation");

            Transform group = board.parent;
            Vector3 oldScale = group.localScale;
            Vector3 oldPosition = group.localPosition;
            try
            {
                group.localPosition = oldPosition + new Vector3(75f, 40f, 0f);
                group.localScale = Vector3.one * .85f;
                Vector2 adjusted = Anchor(game, cornerA);
                Require((popupA - adjusted).sqrMagnitude > 150f,
                    "Popup anchor ignored modified prefab transforms");
            }
            finally
            {
                group.localPosition = oldPosition;
                group.localScale = oldScale;
            }

            Set(game, "game", new BlockBlastModel(123));
            Set(game, "selectedSlot", 0);
            IEnumerator feedback = (IEnumerator)typeof(BlockBlastPrototype)
                .GetMethod("Feedback", PrivateInstance)
                .Invoke(game, new object[] { "TRY AGAIN" });
            Require(feedback.MoveNext(), "Feedback should display initial text");
            Require(feedback.MoveNext() == false, "Feedback coroutine should complete on its next iteration");
            Require(view.Instruction.text == "TAP THE GRID TO PLACE THE PIECE",
                "Selected piece instruction was lost after feedback delay");

            Set(game, "selectedSlot", -1);
            IEnumerator idle = (IEnumerator)typeof(BlockBlastPrototype)
                .GetMethod("Feedback", PrivateInstance)
                .Invoke(game, new object[] { "NICE PLACEMENT!" });
            idle.MoveNext();
            idle.MoveNext();
            Require(view.Instruction.text == "DRAG A PIECE TO PLACE IT",
                "Unselected piece instruction did not return to drag guidance");

            Debug.Log("[Classic Polish QA] PASSED: scoring popup follows board location and nested prefab changes; " +
                "feedback preserves tap-to-place instruction and restores idle hint; scoring settings serialized.");
        }
        finally
        {
            BlockBlastPrototype cleanup = editing.GetComponent<BlockBlastPrototype>();
            if (cleanup != null)
            {
                Set(cleanup, "game", null); // prevent OnDestroy from saving QA model over the player session
            }
            PrefabUtility.UnloadPrefabContents(editing);
        }
    }

    private static Vector2 Anchor(BlockBlastPrototype game, List<Vector2Int> placed)
    {
        return (Vector2)typeof(BlockBlastPrototype)
            .GetMethod("GetPlacementFeedbackAnchor", PrivateInstance)
            .Invoke(game, new object[] { placed });
    }

    private static T Get<T>(BlockBlastPrototype game, string field)
    {
        return (T)typeof(BlockBlastPrototype).GetField(field, PrivateInstance).GetValue(game);
    }

    private static void Set(BlockBlastPrototype game, string field, object value)
    {
        typeof(BlockBlastPrototype).GetField(field, PrivateInstance).SetValue(game, value);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
