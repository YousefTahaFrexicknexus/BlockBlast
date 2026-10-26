using System;
using BlockBlastClone;
using UnityEngine;

namespace MagicalWorlds.Journey
{
    [Serializable]
    public sealed class JourneyAttemptSnapshot
    {
        public int version = 1;
        public int levelNumber;
        public int randomSeed;
        public int moveLimit;
        public JourneyGoalType goal;
        public int targetScore;
        public int targetLines;
        public int clearedLines;
        public int extraMovesGranted;
        public bool rewardedContinueUsed;
        public BlockBlastSnapshot board;
    }

    /// <summary>Independent per-level attempt storage. Never touches Classic's save key.</summary>
    public static class JourneyAttemptStore
    {
        private const string KeyPrefix = "MW.Journey.Attempt.v1.";

        private static string Key(int number)
        {
            return KeyPrefix + number;
        }

        public static bool HasAttempt(JourneyLevelDefinition level)
        {
            return TryLoad(level, out _);
        }

        public static bool TryLoad(JourneyLevelDefinition level, out JourneyAttemptSnapshot snapshot)
        {
            snapshot = null;
            if (level == null || !PlayerPrefs.HasKey(Key(level.levelNumber)))
            {
                return false;
            }

            try
            {
                JourneyAttemptSnapshot saved = JsonUtility.FromJson<JourneyAttemptSnapshot>(
                    PlayerPrefs.GetString(Key(level.levelNumber), ""));
                if (!Validate(saved, level))
                {
                    return false;
                }
                snapshot = saved;
                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("[Journey] Ignoring invalid level attempt: " + error.Message);
                return false;
            }
        }

        public static bool Save(JourneyLevelDefinition level, BlockBlastModel model,
            int clearedLines, int extraMoves, bool rewardUsed)
        {
            if (level == null || model == null || model.Moves == 0 || model.GameOver ||
                level.IsComplete(model.Score, clearedLines) ||
                level.IsFailed(model.Score, clearedLines, model.Moves, false, extraMoves))
            {
                return false;
            }

            JourneyAttemptSnapshot saved = new JourneyAttemptSnapshot
            {
                version = 1,
                levelNumber = level.levelNumber,
                randomSeed = level.randomSeed,
                moveLimit = level.moveLimit,
                goal = level.goal,
                targetScore = level.targetScore,
                targetLines = level.targetLines,
                clearedLines = clearedLines,
                extraMovesGranted = extraMoves,
                rewardedContinueUsed = rewardUsed,
                board = model.ExportSnapshot()
            };
            if (!Validate(saved, level))
            {
                return false;
            }

            try
            {
                PlayerPrefs.SetString(Key(level.levelNumber), JsonUtility.ToJson(saved));
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("[Journey] Could not save level attempt: " + error.Message);
                return false;
            }
        }

        public static void Clear(int levelNumber)
        {
            if (levelNumber < 1) return;
            PlayerPrefs.DeleteKey(Key(levelNumber));
            PlayerPrefs.Save();
        }

        private static bool Validate(JourneyAttemptSnapshot saved, JourneyLevelDefinition level)
        {
            if (saved == null || level == null || saved.version != 1 ||
                saved.levelNumber != level.levelNumber || saved.randomSeed != level.randomSeed ||
                saved.moveLimit != level.moveLimit || saved.goal != level.goal ||
                saved.targetScore != level.targetScore || saved.targetLines != level.targetLines ||
                saved.board == null || saved.board.gameOver || saved.board.moves < 1 ||
                saved.extraMovesGranted < 0 || saved.extraMovesGranted > 3 ||
                saved.rewardedContinueUsed != (saved.extraMovesGranted > 0) ||
                saved.clearedLines < 0 || saved.clearedLines > saved.board.moves * 16 ||
                saved.board.moves >= saved.moveLimit + saved.extraMovesGranted ||
                level.IsComplete(saved.board.score, saved.clearedLines))
            {
                return false;
            }
            return true;
        }
    }
}
