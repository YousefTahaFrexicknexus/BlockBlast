using System;
using BlockBlastClone;
using UnityEditor;
using UnityEngine;

public static class BlockBlastSmokeTests
{
    [InitializeOnLoadMethod]
    private static void VerifyAtEditorLoad()
    {
        try
        {
            Run();
            Debug.Log("[Block Puzzle Milestone 3] Rules smoke tests passed.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[Block Puzzle Milestone 3] Rules smoke tests FAILED: " + ex);
        }
    }

    [MenuItem("Tools/Block Puzzle/Run Rules Smoke Tests")]
    public static void Run()
    {
        BlockBlastModel game = new BlockBlastModel(42);
        Check(game.Hand.Length == 3 && game.Hand[0] != null && game.Hand[1] != null && game.Hand[2] != null, "Initial hand");
        Check(game.HasAnyMove(), "Opening hand must fit");
        Check(!game.CanPlace(game.Hand[0], -1, 0), "Negative board coordinates");
        Check(!game.CanPlace(game.Hand[0], 8, 0), "Board outer boundary");
        Check(!game.TryPlace(0, -1, 0, out PlacementResult ignored) && game.Score == 0, "Invalid move must not change score");

        game.Reset();
        for (int c = 1; c < 8; c++)
        {
            game.Board[0, c] = 1;
        }
        for (int r = 1; r < 8; r++)
        {
            game.Board[r, 0] = 1;
        }
        game.Hand[0] = One();
        Check(game.TryPlace(0, 0, 0, out PlacementResult cross), "Cross completion placement");
        Check(cross.LinesCleared == 2, "Simultaneous row and column clearing");
        Check(cross.ClearedCells.Length == 15, "Cross intersection counted once");
        Check(cross.ScoreAdded > 15, "Clear awards more than placement-only points");
        for (int i = 0; i < 8; i++)
        {
            Check(game.Board[0, i] == 0 && game.Board[i, 0] == 0, "Cleared cells emptied");
        }

        // Another clear immediately after the first maintains a combo.
        for (int c = 1; c < 8; c++)
        {
            game.Board[1, c] = 1;
        }
        game.Hand[1] = One();
        Check(game.TryPlace(1, 1, 0, out PlacementResult combo), "Second line placement");
        Check(combo.Combo == 2 && combo.LinesCleared == 1, "Consecutive clear combo");

        game.Reset();
        game.Hand[0] = One();
        game.Hand[1] = One();
        game.Hand[2] = One();
        Check(game.TryPlace(0, 7, 0, out PlacementResult first), "First placement");
        Check(!first.NewHand && game.Hand[0] == null && game.Hand[1] != null, "First placement cannot refill");
        Check(game.TryPlace(1, 7, 1, out PlacementResult second), "Second placement");
        Check(!second.NewHand && game.Hand[2] != null, "Second placement cannot refill");
        Check(game.TryPlace(2, 7, 2, out PlacementResult third), "Third placement");
        Check(third.NewHand && game.HandNumber == 2, "Hand refills after all three moves");
        Check(game.Hand[0] != null && game.Hand[1] != null && game.Hand[2] != null, "Refilled hand");

        // A remaining piece that cannot fit anywhere triggers game over.
        game.Reset();
        game.Hand[0] = One();
        game.Hand[1] = Huge();
        game.Hand[2] = Huge();
        Check(game.TryPlace(0, 0, 0, out PlacementResult blocked), "Placement before game over");
        Check(blocked.GameOver && game.GameOver && !game.HasAnyMove(), "No remaining legal placements");
        Check(!game.TryPlace(1, 0, 0, out PlacementResult rejected), "Cannot place after game over");

        game.Reset();
        Check(!game.GameOver && game.Score == 0 && game.Moves == 0 && game.HandNumber == 1, "Restart resets current game");

        // Save/load must preserve the exact board and remaining hand.
        game.Reset();
        Check(game.TryPlace(0, 0, 0, out PlacementResult savedMove), "Saved run initial move");
        BlockBlastSnapshot snapshot = game.ExportSnapshot();
        BlockBlastSnapshot decoded = JsonUtility.FromJson<BlockBlastSnapshot>(JsonUtility.ToJson(snapshot));
        BlockBlastModel reloaded = new BlockBlastModel(123);
        Check(reloaded.TryRestore(decoded), "Saved run JSON round-trip");
        Check(reloaded.Score == game.Score && reloaded.Moves == game.Moves &&
            reloaded.HandNumber == game.HandNumber && reloaded.Combo == game.Combo &&
            reloaded.PlacementsWithoutClear == game.PlacementsWithoutClear,
            "Saved run counters preserved");
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                Check(reloaded.Board[r, c] == game.Board[r, c], "Saved board cell mismatch");
            }
        }
        for (int i = 0; i < 3; i++)
        {
            BlockPiece a = game.Hand[i];
            BlockPiece b = reloaded.Hand[i];
            Check((a == null) == (b == null), "Remaining piece null status");
            if (a == null) continue;
            Check(a.Color == b.Color && a.Cells.Length == b.Cells.Length, "Remaining piece color and shape");
            for (int j = 0; j < a.Cells.Length; j++)
            {
                Check(a.Cells[j].x == b.Cells[j].x && a.Cells[j].y == b.Cells[j].y, "Remaining piece cell");
            }
        }

        BlockBlastSnapshot badVersion = game.ExportSnapshot();
        badVersion.version = 1000;
        Check(!reloaded.TryRestore(badVersion) && reloaded.Score == game.Score, "Future save rejected without mutation");
        BlockBlastSnapshot badCell = game.ExportSnapshot();
        badCell.board[0] = 200;
        Check(!reloaded.TryRestore(badCell) && reloaded.Score == game.Score, "Invalid color rejected without mutation");
        BlockBlastSnapshot badShape = game.ExportSnapshot();
        badShape.hand[1].coordinates[0] = -1;
        Check(!reloaded.TryRestore(badShape) && reloaded.Score == game.Score, "Broken piece rejected without mutation");
    }

    private static BlockPiece One()
    {
        return new BlockPiece(new[] { Vector2Int.zero }, 1);
    }

    private static BlockPiece Huge()
    {
        Vector2Int[] cells = new Vector2Int[64];
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                cells[r * 8 + c] = new Vector2Int(c, r);
            }
        }
        return new BlockPiece(cells, 2);
    }

    private static void Check(bool condition, string label)
    {
        if (!condition)
        {
            throw new InvalidOperationException(label);
        }
    }
}
