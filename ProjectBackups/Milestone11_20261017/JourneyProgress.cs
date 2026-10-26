using UnityEngine;

namespace MagicalWorlds.Journey
{
    public static class JourneyProgress
    {
        private const string UnlockKey = "MW.Journey.Unlocked.v1";
        private const string SelectedKey = "MW.Journey.Selected.v1";
        private const string StarsPrefix = "MW.Journey.Stars.v1.";

        public static int HighestUnlocked
        {
            get
            {
                int highest = Mathf.Max(1, PlayerPrefs.GetInt(UnlockKey, 1));
                JourneyLevelCatalog catalog = JourneyLevelCatalog.Load();
                int total = catalog != null ? catalog.Count : highest;
                // Migration from the original six-level catalog: a completed
                // former final level must unlock the following authored level.
                while (highest < total && Stars(highest) > 0)
                {
                    highest++;
                }
                return highest;
            }
        }
        public static int SelectedLevel => Mathf.Max(1, PlayerPrefs.GetInt(SelectedKey, 1));

        public static bool Select(int level, int levelCount)
        {
            if (level < 1 || level > levelCount || level > HighestUnlocked) return false;
            PlayerPrefs.SetInt(SelectedKey, level);
            PlayerPrefs.Save();
            return true;
        }

        public static int Stars(int level)
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(StarsPrefix + level, 0), 0, 3);
        }

        public static void Complete(int level, int awardedStars, int levelCount)
        {
            if (level < 1 || level > levelCount) return;
            int stars = Mathf.Clamp(awardedStars, 1, 3);
            PlayerPrefs.SetInt(StarsPrefix + level, Mathf.Max(stars, Stars(level)));
            PlayerPrefs.SetInt(UnlockKey, Mathf.Max(HighestUnlocked, Mathf.Min(level + 1, levelCount)));
            PlayerPrefs.Save();
        }
    }
}
