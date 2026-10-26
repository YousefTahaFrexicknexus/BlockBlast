using System;
using UnityEngine;
namespace MagicalWorlds.Journey
{
    [Serializable] public sealed class CosmeticItem
    {
        public string id, displayName;
        public Sprite preview, background;
        public int unlockLevel = 1;
    }
    public static class JourneyCosmetics
    {
        public const string BlocksKey = "MW.Cosmetics.Blocks.v1";
        public const string BackgroundKey = "MW.Cosmetics.Background.v1";
        public static CollectionsCatalog Catalog => Resources.Load<CollectionsCatalog>("MagicalWorlds/Cosmetics/CollectionsCatalog");
        public static bool Unlocked(CosmeticItem item) => item != null && JourneyProgress.HighestUnlocked >= item.unlockLevel;
        public static CosmeticItem Selected(bool blocks)
        {
            var catalog=Catalog;if(catalog==null)return null;
            var items=blocks?catalog.blocks:catalog.backgrounds;if(items==null||items.Length==0)return null;
            string id=PlayerPrefs.GetString(blocks?BlocksKey:BackgroundKey,items[0].id);
            foreach(var item in items)if(item!=null&&item.id==id&&Unlocked(item))return item;
            return items[0];
        }
        public static bool Equip(bool blocks,string id)
        {
            var catalog=Catalog;if(catalog==null)return false;var items=blocks?catalog.blocks:catalog.backgrounds;
            if(items==null)return false;
            foreach(var item in items)if(item!=null&&item.id==id&&Unlocked(item))
            {PlayerPrefs.SetString(blocks?BlocksKey:BackgroundKey,id);PlayerPrefs.Save();return true;}
            return false;
        }
    }
}
