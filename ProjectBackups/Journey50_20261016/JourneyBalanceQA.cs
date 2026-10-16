using System;
using BlockBlastClone;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class JourneyBalanceQA
{
    [MenuItem("Tools/Magical Worlds/Journey/Simulate Level Difficulty")]
    public static void Simulate()
    {
        JourneyLevelCatalog catalog = JourneyLevelCatalog.Load();
        if (catalog == null) throw new InvalidOperationException("Missing level catalog.");
        const int runs = 18;
        for (int index = 1; index <= catalog.Count; index++)
        {
            JourneyLevelDefinition level = catalog.Get(index);
            int randomWins = 0;
            int greedyWins = 0;
            int randomMoves = 0;
            int greedyMoves = 0;
            for (int trial = 0; trial < runs; trial++)
            {
                int used;
                if (TryFinish(level, index * 10000 + trial, false, out used))
                {
                    randomWins++;
                    randomMoves += used;
                }
                if (TryFinish(level, index * 10000 + trial, true, out used))
                {
                    greedyWins++;
                    greedyMoves += used;
                }
            }
            string message = "[Journey Balance QA] Level " + index + " " + level.levelName +
                " moves=" + level.moveLimit + " " + level.ObjectiveText +
                " | random=" + randomWins + "/" + runs +
                " | line-seeking=" + greedyWins + "/" + runs +
                " | mean winning moves=" + (greedyWins > 0 ?
                    ((float)greedyMoves / greedyWins).ToString("F1") : "n/a");
            if (greedyWins == 0) Debug.LogWarning(message + " — check reachability.");
            else Debug.Log(message);
            if (index == 4 || index == 6)
            {
                JourneyLevelDefinition candidate = UnityEngine.Object.Instantiate(level);
                try
                {
                    if (index == 4) candidate.targetScore = 125;
                    else
                    {
                        candidate.targetScore = 225;
                        candidate.targetLines = 3;
                    }

                    int candidateRandom = 0;
                    int candidateStrategic = 0;
                    int winningMoves = 0;
                    for (int trial = 0; trial < runs; trial++)
                    {
                        if (TryFinish(candidate, index * 10000 + trial, false, out int randomUsed))
                            candidateRandom++;
                        if (TryFinish(candidate, index * 10000 + trial, true, out int smartUsed))
                        {
                            candidateStrategic++;
                            winningMoves += smartUsed;
                        }
                    }
                    Debug.Log("[Journey Balance QA] CANDIDATE Level " + index +
                        " " + candidate.ObjectiveText +
                        " | random=" + candidateRandom + "/" + runs +
                        " | line-seeking=" + candidateStrategic + "/" + runs +
                        " | mean strategic moves=" +
                        (candidateStrategic > 0 ?
                            ((float)winningMoves / candidateStrategic).ToString("F1") : "n/a"));
                }
                finally { UnityEngine.Object.DestroyImmediate(candidate); }
            }
        }
        Debug.Log("[Journey Balance QA] Completed deterministic heuristic simulations. These are not player success statistics.");
    }

    private static bool TryFinish(JourneyLevelDefinition level, int seed, bool strategic, out int usedMoves)
    {
        BlockBlastModel model = new BlockBlastModel(level.randomSeed);
        System.Random decisions = new System.Random(seed);
        int clearedLines = 0;
        while (model.Moves < level.moveLimit && !model.GameOver)
        {
            if (level.IsComplete(model.Score, clearedLines)) break;
            int bestSlot = -1, bestRow = -1, bestCol = -1;
            double best = double.NegativeInfinity;
            for (int slot = 0; slot < model.Hand.Length; slot++)
            {
                BlockPiece piece = model.Hand[slot];
                if (piece == null) continue;
                for (int row = 0; row <= 8 - piece.Height; row++)
                for (int col = 0; col <= 8 - piece.Width; col++)
                {
                    if (!model.CanPlace(piece, row, col)) continue;
                    double rating = decisions.NextDouble();
                    if (strategic)
                    {
                        int[] rows = new int[8];
                        int[] cols = new int[8];
                        for (int i = 0; i < 8; i++)
                        for (int j = 0; j < 8; j++)
                        {
                            if (model.Board[i, j] == 0) continue;
                            rows[i]++;
                            cols[j]++;
                        }
                        int completed = 0;
                        double density = 0;
                        foreach (Vector2Int square in piece.Cells)
                        {
                            int y = row + square.y;
                            int x = col + square.x;
                            rows[y]++;
                            cols[x]++;
                            density += (rows[y] * rows[y] + cols[x] * cols[x]) * .02;
                        }
                        for (int k = 0; k < 8; k++)
                        {
                            if (rows[k] == 8) completed++;
                            if (cols[k] == 8) completed++;
                        }
                        rating += completed * 1000 + density + piece.Cells.Length * .04;
                    }
                    if (rating <= best) continue;
                    best = rating;
                    bestSlot = slot;
                    bestRow = row;
                    bestCol = col;
                }
            }
            if (bestSlot < 0) break;
            if (!model.TryPlace(bestSlot, bestRow, bestCol, out PlacementResult result)) break;
            clearedLines += result.LinesCleared;
        }
        usedMoves = model.Moves;
        return level.IsComplete(model.Score, clearedLines);
    }
}
