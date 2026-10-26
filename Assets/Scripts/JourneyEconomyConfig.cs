using UnityEngine;

namespace MagicalWorlds.Journey
{
    [CreateAssetMenu(fileName = "JourneyEconomyConfig", menuName = "Magical Worlds/Journey Economy")]
    public sealed class JourneyEconomyConfig : ScriptableObject
    {
        [Header("Starting wallet - first install only")]
        [Min(0)] public int startingCoins = 1250;
        [Min(0)] public int startingGems = 80;
        [Min(1)] public int maximumEnergy = 5;

        [Header("Journey - coins awarded for new progress only")]
        [Min(0)] public int firstClearCoins = 40;
        [Min(0)] public int coinsPerLevel = 4;
        [Min(0)] public int coinsPerNewStar = 20;
        [Min(0)] public int checkpointBonusCoins = 100;
        [Min(1)] public int checkpointEveryLevels = 10;

        public int CalculateNewCoins(int level, int previousBestStars, int newStars)
        {
            if (level < 1 || level > 50) return 0;
            int oldStars = Mathf.Clamp(previousBestStars, 0, 3);
            int currentStars = Mathf.Clamp(newStars, 0, 3);
            if (currentStars <= oldStars) return 0;
            long amount = (long)(oldStars == 0 ? firstClearCoins : 0) +
                (oldStars == 0 ? (long)coinsPerLevel * level : 0) +
                (long)(currentStars - oldStars) * coinsPerNewStar +
                (oldStars == 0 && checkpointEveryLevels > 0 && level % checkpointEveryLevels == 0
                    ? checkpointBonusCoins : 0);
            return amount > int.MaxValue ? int.MaxValue : (int)amount;
        }
    }
}
