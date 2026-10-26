using UnityEngine;
namespace MagicalWorlds.Journey
{
    [CreateAssetMenu(menuName="Magical Worlds/Collections Catalog")]
    public sealed class CollectionsCatalog:ScriptableObject
    {
        public CosmeticItem[] blocks;
        public CosmeticItem[] backgrounds;
    }
}
