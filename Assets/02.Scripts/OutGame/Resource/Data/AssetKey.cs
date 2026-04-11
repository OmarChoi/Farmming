using System;
using System.Collections.Generic;

public static class AssetKey
{
    public static class Prefab
    {
        public const string FarmTile = "FarmTile";
        public const string TerrainCell = "TerrainCell";
        public const string Player = "Player";
        public const string UnlockedItem = "UnlockedItem";
    }

    public static class Gathering
    {
        public const string NormalRock = "NormalRock";
        public const string NormalTree = "NormalTree";
    }

    public static class Building
    {
        private const string MarketStall = "MarketStall";
        private const string Smithy = "Smithy";
        private const string HairShop = "HairShop";

        private static readonly HashSet<string> _registered = new HashSet<string>
        {
            MarketStall,
            Smithy,
            HairShop
        };

        public static string GetKey(string buildingId)
        {
            return _registered.Contains(buildingId) ? buildingId : null;
        }
    }

    public static class BGM
    {
        public const string StartScene = "SFXstartscenebgm";
        public const string Village = "SFXvillagebgm";
        public const string Dungeon2 = "SFXdungeon2bgm";
    }

    public static class UI
    {
        public const string Inventory = "UI_Inventory";
        public const string Slot = "UI_Slot";
        public const string HarvestNotification = "HarvestNotification";
        public const string Shop = "UI_Shop";
        public const string NpcDialogue = "UI_NpcDialogue";
        public const string QuestBoard = "UI_QuestBoard";
        public const string QuestJournal = "UI_QuestJournal";
        public const string QuestCompletePopup = "UI_QuestCompletePopup";
        public const string Stamina = "UI_Stamina";
        public const string BuildingList = "UI_BuildingList";
        public const string BuildInfo = "UI_BuildInfo";
        public const string VillageState = "UI_VillageState";
        public const string VillageLevelUp = "UI_VillageLevelUp";

        private static readonly Dictionary<Type, string> _registered = new Dictionary<Type, string>
        {
            {typeof(UI_BuildingList), BuildingList},
            {typeof(UI_BuildInfo), BuildInfo},
            {typeof(UI_VillageState), VillageState},
            {typeof(UI_VillageLevelUp), VillageLevelUp},
        };

        public static string GetKey<T>() where T : UIBase
        {
            return _registered.GetValueOrDefault(typeof(T));
        }
    }
    
    public static class SFX
    {
        #region Player
        public const string Move = "SFX_Move";
        
        #endregion

        #region Helper
        public const string HarvestNormal = "SFXharvestnormal";
        public const string HarvestEpic = "SFXharvestepic";
        public const string HarvestLegendary = "SFXharvestlegendary";
        public const string SowNormal = "SFXsownormal";
        public const string SowLegendary = "SFXsowlegendary";
        public const string WaterNormalSplash = "SFXwaternormal";
        public const string WaterEpicSplash = "SFXwaterepic";
        public const string WaterLegendarySplash = "SFXwaterlegendary";
        public const string WoodLeaf = "SFXwoodleaf";

        #endregion
    }
}
