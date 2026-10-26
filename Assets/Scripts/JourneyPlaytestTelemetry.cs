using System;
using System.IO;
using UnityEngine;

namespace MagicalWorlds.Journey
{
    [Serializable]
    public sealed class JourneyPlaytestEvent
    {
        public string utc;
        public string sessionId;
        public string eventType;
        public int level;
        public int score;
        public int clearedLines;
        public int movesUsed;
        public int moveLimit;
        public int stars;
        public bool preview;
        public bool automated;
    }

    /// <summary>Editor-only, local JSONL telemetry for balancing. Production
    /// builds never log player events or modify progression through this tool.</summary>
    public static class JourneyPlaytestTelemetry
    {
        public const string RelativeReport = "ProjectReports/JourneyPlaytestEvents.jsonl";
#if UNITY_EDITOR
        private const string AutomatedKey = "MW.Journey.Playtest.AutomatedQA.v1";
        public static bool AutomatedTest
        {
            get => UnityEditor.SessionState.GetBool(AutomatedKey, false);
            set => UnityEditor.SessionState.SetBool(AutomatedKey, value);
        }
#endif

        public static void Record(string sessionId, string type, JourneyLevelDefinition level,
            int score, int clearedLines, int movesUsed, int stars)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying || level == null) return;
            var record = new JourneyPlaytestEvent
            {
                utc = DateTime.UtcNow.ToString("o"),
                sessionId = sessionId,
                eventType = type,
                level = level.levelNumber,
                score = score,
                clearedLines = clearedLines,
                movesUsed = movesUsed,
                moveLimit = level.moveLimit,
                stars = stars,
                preview = JourneyPlaytestContext.IsPreview,
                automated = AutomatedTest
            };
            try
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string file = Path.Combine(root, RelativeReport);
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.AppendAllText(file, JsonUtility.ToJson(record) + Environment.NewLine);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Journey Playtest] Unable to write telemetry: " + e.Message);
            }
#endif
        }
    }
}
