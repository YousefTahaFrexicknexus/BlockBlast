using System;
using BlockBlastClone;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class JourneyAttemptQA
{
    private const string TestKey = "MW.Journey.Attempt.v1.4";

    [MenuItem("Tools/Magical Worlds/Journey/Validate Attempt Save and Resume")]
    public static void Validate()
    {
        JourneyLevelCatalog catalog = JourneyLevelCatalog.Load();
        Require(catalog != null, "Missing Journey catalog");
        JourneyLevelDefinition level = catalog.Get(4);
        Require(level != null, "Level 4 missing");

        bool previouslySaved = PlayerPrefs.HasKey(TestKey);
        string previous = PlayerPrefs.GetString(TestKey, "");
        try
        {
            JourneyAttemptStore.Clear(level.levelNumber);
            Require(!JourneyAttemptStore.HasAttempt(level), "New level must not report CONTINUE");
            BlockBlastModel model = new BlockBlastModel(level.randomSeed);
            bool placed = false;
            for (int slot = 0; slot < model.Hand.Length && !placed; slot++)
            {
                BlockPiece piece = model.Hand[slot];
                if (piece == null) continue;
                for (int row = 0; row < 8 && !placed; row++)
                for (int col = 0; col < 8 && !placed; col++)
                {
                    if (!model.CanPlace(piece, row, col)) continue;
                    Require(model.TryPlace(slot, row, col, out PlacementResult result), "Legal placement failed");
                    placed = true;
                }
            }
            Require(placed && model.Moves == 1, "Could not prepare one-move attempt");
            Require(JourneyAttemptStore.Save(level, model, 0, 0, false),
                "Valid unfinished attempt was not saved");
            Require(JourneyAttemptStore.HasAttempt(level),
                "CONTINUE state should be available after saving");
            Require(JourneyAttemptStore.TryLoad(level, out JourneyAttemptSnapshot saved),
                "Saved attempt could not be loaded");
            Require(saved.board.moves == 1 && saved.board.score == model.Score &&
                saved.levelNumber == level.levelNumber && saved.clearedLines == 0,
                "Saved level or move/score values changed");

            BlockBlastModel restored = new BlockBlastModel(level.randomSeed);
            Require(restored.TryRestore(saved.board), "Board snapshot failed roundtrip validation");
            Require(restored.Score == model.Score && restored.Moves == model.Moves,
                "Restored score or moves differ from saved");
            for (int row = 0; row < 8; row++)
            for (int col = 0; col < 8; col++)
                Require(restored.Board[row, col] == model.Board[row, col],
                    "Restored board cells differ from original");

            JourneyLevelDefinition changed = UnityEngine.Object.Instantiate(level);
            try
            {
                changed.targetScore += 1;
                Require(!JourneyAttemptStore.HasAttempt(changed),
                    "Changing the level definition must invalidate an outdated attempt");
            }
            finally { UnityEngine.Object.DestroyImmediate(changed); }

            PlayerPrefs.SetString(TestKey, "{broken");
            Require(!JourneyAttemptStore.HasAttempt(level),
                "Corrupt JSON must never be offered as a resumable attempt");

            JourneyAttemptStore.Clear(level.levelNumber);
            Require(!JourneyAttemptStore.HasAttempt(level),
                "Retry/finish must erase the saved attempt");

            Debug.Log("[Journey Attempt QA] PASSED: unfinished save, CONTINUE state, " +
                "board/hand snapshot restore, edit invalidation, corrupt-data rejection and clear.");
        }
        finally
        {
            if (previouslySaved) PlayerPrefs.SetString(TestKey, previous);
            else PlayerPrefs.DeleteKey(TestKey);
            PlayerPrefs.Save();
        }
    }

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }
}
