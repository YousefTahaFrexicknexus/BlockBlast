using UnityEngine;

namespace MagicalWorlds.Journey
{
    /// <summary>Read-only cosmetic board palette. Original colors match approved art.</summary>
    public static class JourneyBoardPalette
    {
        private static readonly Color OriginalEmpty = Parse("#2C3E78");
        private static readonly Color OriginalTray = Parse("#22336D");
        private static readonly Color OriginalSelected = Parse("#34519A");

        private static Color Parse(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        private static BoardPaletteItem Selected
        {
            get
            {
                JourneyShopCatalog catalog = JourneyShopCatalog.Load();
                if (catalog == null) return null;
                string id = JourneyWallet.EquippedPaletteId;
                for (int i = 0; i < catalog.Count; i++)
                {
                    BoardPaletteItem item = catalog.Get(i);
                    if (item != null && item.id == id) return item;
                }
                return null;
            }
        }

        public static Color EmptyCell
        {
            get { BoardPaletteItem item = Selected; return item != null ? item.emptyCellColor : OriginalEmpty; }
        }
        public static Color Tray
        {
            get { BoardPaletteItem item = Selected; return item != null ? item.trayColor : OriginalTray; }
        }
        public static Color SelectedTray
        {
            get { BoardPaletteItem item = Selected; return item != null ? item.selectedTrayColor : OriginalSelected; }
        }
    }
}