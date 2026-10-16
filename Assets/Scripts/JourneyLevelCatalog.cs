using UnityEngine;

namespace MagicalWorlds.Journey
{
    [CreateAssetMenu(menuName = "Magical Worlds/Journey Level Catalog", fileName = "JourneyLevelCatalog")]
    public sealed class JourneyLevelCatalog : ScriptableObject
    {
        public JourneyLevelDefinition[] levels;

        public int Count => levels != null ? levels.Length : 0;

        public JourneyLevelDefinition Get(int level)
        {
            if (levels == null || level < 1 || level > levels.Length) return null;
            return levels[level - 1];
        }

        public static JourneyLevelCatalog Load()
        {
            return Resources.Load<JourneyLevelCatalog>("MagicalWorlds/Journey/LevelCatalog");
        }
    }

}
