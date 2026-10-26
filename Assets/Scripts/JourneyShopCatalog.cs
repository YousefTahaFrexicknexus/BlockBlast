using System;
using UnityEngine;

namespace MagicalWorlds.Journey
{
    [Serializable]
    public sealed class BoardPaletteItem
    {
        public string id;
        public string displayName;
        [Min(0)] public int coinPrice;
        public Color emptyCellColor;
        public Color trayColor;
        public Color selectedTrayColor;
    }

    [CreateAssetMenu(fileName = "JourneyShopCatalog", menuName = "Magical Worlds/Board Palette Shop")]
    public sealed class JourneyShopCatalog : ScriptableObject
    {
        public BoardPaletteItem[] palettes;
        public int Count => palettes != null ? palettes.Length : 0;
        public BoardPaletteItem Get(int index) { return index >= 0 && index < Count ? palettes[index] : null; }
        public static JourneyShopCatalog Load() { return Resources.Load<JourneyShopCatalog>("MagicalWorlds/Economy/JourneyShopCatalog"); }
    }
}