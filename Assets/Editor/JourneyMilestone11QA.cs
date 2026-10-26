using System;
using BlockBlastClone;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class JourneyMilestone11QA
{
    static JourneyMilestone11QA()
    {
        EditorApplication.delayCall += ValidateOnce;
    }

    private static void ValidateOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += ValidateOnce;
            return;
        }
        try { Validate(); }
        catch (Exception error) { Debug.LogError("[Journey Milestone 11 QA] FAILED: " + error); }
    }

    [MenuItem("Tools/Magical Worlds/Journey/Validate Milestone 11")]
    public static void Validate()
    {
        JourneyLevelCatalog catalog = JourneyLevelCatalog.Load();
        Check(catalog != null && catalog.Count == 50, "Expected 50 Journey levels");

        for (int levelNumber = 1; levelNumber <= catalog.Count; levelNumber++)
        {
            JourneyLevelDefinition level = catalog.Get(levelNumber);
            Check(level != null, "Missing level " + levelNumber);
            Check(Mathf.Approximately(level.threeStarRemainingRatio, .5f) &&
                Mathf.Approximately(level.twoStarRemainingRatio, .2f),
                "Legacy star thresholds must remain unchanged for level " + levelNumber);
            for (int used = 0; used <= level.moveLimit; used++)
            {
                int movesLeft = Mathf.Max(0, level.moveLimit - used);
                int expected = movesLeft >= Mathf.CeilToInt(level.moveLimit * .5f) ? 3 :
                    movesLeft >= Mathf.CeilToInt(level.moveLimit * .2f) ? 2 : 1;
                Check(level.CalculateStars(used) == expected,
                    "Star regression on Level " + levelNumber + ", moves " + used);
            }
        }

        const int seed = 177;
        BlockBlastModel model = new BlockBlastModel(seed);
        Check(PlaceFirstLegal(model, out _), "Cannot place first piece");
        BlockBlastSnapshot snapshot = model.ExportSnapshot();
        Check(snapshot.randomDrawCount >= 6, "RNG draw count missing from snapshot");

        BlockBlastModel restored = new BlockBlastModel(seed);
        Check(restored.TryRestore(snapshot), "Could not restore seeded snapshot");
        Check(AreEquivalent(model.ExportSnapshot(), restored.ExportSnapshot()),
            "Restored snapshot differs from original");
        for (int i = 0; i < 7; i++)
        {
            Check(PlaceFirstLegal(model, out PlacementResult a), "Original model has no move");
            Check(PlaceFirstLegal(restored, out PlacementResult b), "Restored model has no move");
            Check(AreEquivalent(model.ExportSnapshot(), restored.ExportSnapshot()),
                "RNG or future piece hand diverged on move " + i);
            if (model.GameOver) break;
        }

        Debug.Log("[Journey Milestone 11 QA] PASSED: 50 star-threshold regressions, " +
            "seeded snapshot and future-hand determinism.");
    }

    private static bool PlaceFirstLegal(BlockBlastModel model, out PlacementResult result)
    {
        result = default;
        for (int slot = 0; slot < model.Hand.Length; slot++)
        {
            BlockPiece piece = model.Hand[slot];
            if (piece == null) continue;
            for (int row = 0; row <= 8 - piece.Height; row++)
            for (int col = 0; col <= 8 - piece.Width; col++)
            {
                if (model.TryPlace(slot, row, col, out result)) return true;
            }
        }
        return false;
    }

    private static bool AreEquivalent(BlockBlastSnapshot a, BlockBlastSnapshot b)
    {
        if (a.score != b.score || a.moves != b.moves || a.combo != b.combo ||
            a.randomDrawCount != b.randomDrawCount || a.handNumber != b.handNumber ||
            a.gameOver != b.gameOver) return false;
        for (int index = 0; index < a.board.Length; index++)
            if (a.board[index] != b.board[index]) return false;
        for (int i = 0; i < a.hand.Length; i++)
        {
            BlockPieceSnapshot x = a.hand[i], y = b.hand[i];
            if (x.active != y.active || x.color != y.color) return false;
            if (!x.active) continue;
            if (x.coordinates.Length != y.coordinates.Length) return false;
            for (int j = 0; j < x.coordinates.Length; j++)
                if (x.coordinates[j] != y.coordinates[j]) return false;
        }
        return true;
    }

    private static void Check(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }
}
