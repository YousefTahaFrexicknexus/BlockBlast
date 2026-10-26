using UnityEngine;

namespace MagicalWorlds.Journey
{
    public enum JourneyGoalType
    {
        ReachScore,
        ClearLines,
        ScoreAndLines
    }

    [CreateAssetMenu(menuName = "Magical Worlds/Journey Level", fileName = "JourneyLevel")]
    public sealed class JourneyLevelDefinition : ScriptableObject
    {
        [Min(1)] public int levelNumber = 1;
        public string levelName = "Enchanted Garden";
        [Min(1)] public int moveLimit = 10;
        public JourneyGoalType goal = JourneyGoalType.ReachScore;
        [Min(1)] public int targetScore = 30;
        [Min(0)] public int targetLines = 0;
        [Min(1)] public int randomSeed = 101;

        [Header("Star Awards - Percentage of Move Budget Remaining")]
        [Range(0f, 1f)] public float threeStarRemainingRatio = .5f;
        [Range(0f, 1f)] public float twoStarRemainingRatio = .2f;

        private void OnValidate()
        {
            threeStarRemainingRatio = Mathf.Clamp01(threeStarRemainingRatio);
            twoStarRemainingRatio = Mathf.Clamp(twoStarRemainingRatio, 0f, threeStarRemainingRatio);
        }
        public int CalculateStars(int usedMoves)
        {
            int remaining = Mathf.Max(0, moveLimit - Mathf.Max(0, usedMoves));
            float top = Mathf.Clamp01(threeStarRemainingRatio);
            float middle = Mathf.Min(top, Mathf.Clamp01(twoStarRemainingRatio));
            return remaining >= Mathf.CeilToInt(moveLimit * top) ? 3 :
                remaining >= Mathf.CeilToInt(moveLimit * middle) ? 2 : 1;
        }
        public bool IsComplete(int score, int clearedLines)
        {
            switch (goal)
            {
                case JourneyGoalType.ReachScore: return score >= targetScore;
                case JourneyGoalType.ClearLines: return clearedLines >= targetLines;
                case JourneyGoalType.ScoreAndLines: return score >= targetScore && clearedLines >= targetLines;
                default: return false;
            }
        }

        public bool IsFailed(int score, int clearedLines, int usedMoves, bool noLegalMoves, int bonusMoves = 0)
        {
            return !IsComplete(score, clearedLines) &&
                (usedMoves >= moveLimit + Mathf.Max(0, bonusMoves) || noLegalMoves);
        }
        public string ObjectiveText
        {
            get
            {
                switch (goal)
                {
                    case JourneyGoalType.ReachScore: return "SCORE " + targetScore.ToString("N0") + " POINTS";
                    case JourneyGoalType.ClearLines: return "CLEAR " + targetLines + " LINES";
                    default: return "SCORE " + targetScore.ToString("N0") + " + CLEAR " + targetLines + " LINES";
                }
            }
        }

        public string ProgressText(int score, int clearedLines)
        {
            switch (goal)
            {
                case JourneyGoalType.ReachScore: return Mathf.Min(score, targetScore) + " / " + targetScore;
                case JourneyGoalType.ClearLines: return Mathf.Min(clearedLines, targetLines) + " / " + targetLines;
                default: return Mathf.Min(score, targetScore) + "/" + targetScore + " PTS  •  " +
                    Mathf.Min(clearedLines, targetLines) + "/" + targetLines + " LINES";
            }
        }

        public float Progress(int score, int clearedLines)
        {
            float points = targetScore > 0 ? Mathf.Clamp01((float)score / targetScore) : 1f;
            float lines = targetLines > 0 ? Mathf.Clamp01((float)clearedLines / targetLines) : 1f;
            switch (goal)
            {
                case JourneyGoalType.ReachScore: return points;
                case JourneyGoalType.ClearLines: return lines;
                default: return (points + lines) * .5f;
            }
        }
    }

}
