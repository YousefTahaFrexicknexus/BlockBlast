using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MagicalWorldsClassicFeelQA
{
    static MagicalWorldsClassicFeelQA()
    {
        EditorApplication.delayCall += RunOnceAfterCompile;
    }

    private static void RunOnceAfterCompile()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunOnceAfterCompile;
            return;
        }

        try
        {
            VerifyDragGeometry();
            VerifyPreviewCleanup();
        }
        catch (Exception ex)
        {
            Debug.LogError("[Classic Feel QA] FAILED: " + ex);
        }
    }
    [MenuItem("Tools/Magical Worlds/Classic Gameplay/Run Drag Geometry Checks")]
    public static void VerifyDragGeometry()
    {
        const float boardSize = 916f;
        const float padding = 12f;
        const float gap = 6f;
        const float cellSize = (boardSize - 2f * padding - 7f * gap) / 8f;
        float step = cellSize + gap;
        Vector2 first = new Vector2(-boardSize * .5f + padding + cellSize * .5f,
            boardSize * .5f - padding - cellSize * .5f);

        Check(BlockBlastDragMath.PieceOrigin(first, first, 1, 1, step) == Vector2Int.zero,
            "Single-cell origin mapping");
        Check(GridPosition(first, 3, 2, 5, 4, step) == new Vector2Int(5, 4),
            "Multi-cell piece origin mapping");
        Check(GridPosition(first, 2, 2, 0, 0, step) == Vector2Int.zero,
            "Upper-left 2x2 mapping");
        Check(GridPosition(first, 2, 3, 6, 5, step) == new Vector2Int(6, 5),
            "Lower-right 2x3 mapping");

        Check(BlockBlastDragMath.NearBoard(Vector2.zero, boardSize, step), "Board center proximity");
        Check(!BlockBlastDragMath.NearBoard(new Vector2(2000, 0), boardSize, step),
            "Distant drop must be rejected");

        GameObject canvas = new GameObject("QA Canvas", typeof(RectTransform));
        GameObject group = new GameObject("QA Board Group", typeof(RectTransform));
        GameObject board = new GameObject("QA Board", typeof(RectTransform));
        try
        {
            group.transform.SetParent(canvas.transform, false);
            board.transform.SetParent(group.transform, false);
            canvas.transform.localScale = Vector3.one;
            group.transform.localPosition = new Vector3(-72f, 48f, 0f);
            group.transform.localScale = Vector3.one * .83f;
            board.transform.localPosition = new Vector3(30f, -45f, 0f);

            Vector2 boardCenter = first + new Vector2((3 + (2 - 1) * .5f) * step,
                -(4 + (3 - 1) * .5f) * step);
            Vector2 canvasLocal = canvas.transform.InverseTransformPoint(board.transform.TransformPoint(boardCenter));
            Vector2 recovered = board.transform.InverseTransformPoint(canvas.transform.TransformPoint(canvasLocal));
            Check(BlockBlastDragMath.PieceOrigin(recovered, first, 2, 3, step) == new Vector2Int(3, 4),
                "Nested, moved, scaled Board prefab snapping");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(board);
            UnityEngine.Object.DestroyImmediate(group);
            UnityEngine.Object.DestroyImmediate(canvas);
        }

        const string classicPath = "Assets/MagicalWorlds/Prefabs/Classic/ClassicGame.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(classicPath);
        Check(prefab != null && prefab.GetComponent<BlockBlastPrototype>() != null,
            "Editable Classic Game prefab exists");

        SerializedObject serialized = new SerializedObject(prefab.GetComponent<BlockBlastPrototype>());
        string values = "";
        foreach (string name in new[] { "dragLift", "dragFollowSpeed", "dragStartScale",
            "dragValidScale", "dragInvalidScale", "previewPulseSpeed", "invalidNudgeScale",
            "invalidNudgeDuration" })
        {
            SerializedProperty property = serialized.FindProperty(name);
            Check(property != null, "Missing Inspector tuning field: " + name);
            values += " " + name + "=" + property.floatValue.ToString("F2");
        }
        Debug.Log("[Classic Feel QA] PASSED: multi-cell grid origin, boundary checks, " +
            "nested board transforms and serialized feel tuning fields. Values:" + values);
    }

    [MenuItem("Tools/Magical Worlds/Classic Gameplay/Validate Preview Cleanup")]
    public static void VerifyPreviewCleanup()
    {
        const string path = "Assets/MagicalWorlds/Prefabs/Classic/ClassicGame.prefab";
        GameObject editable = PrefabUtility.LoadPrefabContents(path);
        BlockBlastPrototype controller = null;
        try
        {
            controller = editable.GetComponent<BlockBlastPrototype>();
            BlockBlastUIReferences view = editable.GetComponentInChildren<BlockBlastUIReferences>(true);
            Check(controller != null && view != null && view.IsComplete,
                "Classic prefab must contain its gameplay controller and 64 preview images.");

            var flags = System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic;
            Type type = typeof(BlockBlastPrototype);
            UnityEngine.UI.Image[,] grid = new UnityEngine.UI.Image[8, 8];
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    grid[row, col] = view.Previews[row * 8 + col];
                }
            }
            type.GetField("preview", flags).SetValue(controller, grid);
            var tracked = (System.Collections.IList)type.GetField("visibleDragPreviews", flags).GetValue(controller);
            var clear = type.GetMethod("ClearDragPreviews", flags);
            var lineHint = type.GetMethod("ShowLineHint", flags);
            var footprint = type.GetMethod("ShowPlacementPreview", flags);
            Check(clear != null && lineHint != null && footprint != null, "Preview lifecycle methods must exist.");

            lineHint.Invoke(controller, new object[] { 0, 3 });
            Check(tracked.Count == 1 && grid[0, 3].gameObject.activeSelf,
                "Full-line hint must be tracked when activated.");
            clear.Invoke(controller, new object[] { false });
            Check(tracked.Count == 0 && ActiveCount(view) == 0,
                "Full-line hint remained active after standard preview cleanup.");

            BlockBlastClone.BlockBlastModel model = new BlockBlastClone.BlockBlastModel(123);
            type.GetField("game", flags).SetValue(controller, model);
            type.GetField("hoverNear", flags).SetValue(controller, true);
            type.GetField("hoverValid", flags).SetValue(controller, true);
            type.GetField("hoverRow", flags).SetValue(controller, 0);
            type.GetField("hoverCol", flags).SetValue(controller, 0);
            footprint.Invoke(controller, new object[] { model.Hand[0] });
            Check(ActiveCount(view) > 0 && tracked.Count > 0,
                "The actual dragged piece preview wasn't registered.");
            clear.Invoke(controller, new object[] { false });
            Check(ActiveCount(view) == 0 && tracked.Count == 0,
                "Moving away from a placement left a visual footprint.");

            // Reproduce the older untracked activation path and test the
            // release/cancellation safety sweep.
            grid[2, 4].gameObject.SetActive(true);
            clear.Invoke(controller, new object[] { true });
            Check(ActiveCount(view) == 0,
                "The safety sweep failed to remove an orphan highlight.");

            Debug.Log("[Classic Preview Cleanup QA] PASSED: tracked piece footprint, full-line hint, " +
                "off-board clear and orphaned highlight recovery.");
        }
        finally
        {
            if (controller != null)
            {
                var gameField = typeof(BlockBlastPrototype).GetField(
                    "game", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                gameField.SetValue(controller, null);
            }
            PrefabUtility.UnloadPrefabContents(editable);
        }
    }

    private static int ActiveCount(BlockBlastUIReferences view)
    {
        int count = 0;
        foreach (UnityEngine.UI.Image image in view.Previews)
        {
            if (image != null && image.gameObject.activeSelf)
            {
                count++;
            }
        }
        return count;
    }

    private static Vector2Int GridPosition(Vector2 first, int width, int height, int col, int row, float step)
    {
        Vector2 center = new Vector2(first.x + (col + (width - 1) * .5f) * step,
            first.y - (row + (height - 1) * .5f) * step);
        return BlockBlastDragMath.PieceOrigin(center, first, width, height, step);
    }

    private static void Check(bool success, string message)
    {
        if (!success)
        {
            throw new InvalidOperationException(message);
        }
    }
}
