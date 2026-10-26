using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicalWorlds.Journey
{
    /// <summary>Temporary Editor-only level preview. No unlock, stars, selected-level
    /// PlayerPrefs or unfinished attempt data are changed by playtest mode.</summary>
    public static class JourneyPlaytestContext
    {
#if UNITY_EDITOR
        private const string Key = "MW.Journey.Playtest.Level.v1";
        public static bool IsPreview => Application.isPlaying && PreviewLevel > 0;
        public static int PreviewLevel => SessionState.GetInt(Key, 0);

        public static void SetPreview(int level)
        {
            SessionState.SetInt(Key, Mathf.Max(0, level));
        }

        public static void Clear()
        {
            SessionState.EraseInt(Key);
        }
#else
        public static bool IsPreview => false;
        public static int PreviewLevel => 0;
#endif
    }
}
