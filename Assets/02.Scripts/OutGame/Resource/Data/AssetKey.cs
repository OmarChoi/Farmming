using System;
using System.Collections.Generic;

public static class AssetKey
{
    public static class Prefab
    {
        public const string FarmTile = "FarmTile";
        public const string TerrainCell = "TerrainCell";
        public const string Player = "Player";
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

        private static readonly HashSet<string> _registered = new HashSet<string>
        {
            MarketStall,
            Smithy
        };

        public static string GetKey(string buildingId)
        {
            return _registered.Contains(buildingId) ? buildingId : null;
        }
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

        private static readonly Dictionary<Type, string> _registered = new Dictionary<Type, string>
        {
            {typeof(UI_BuildingList), BuildingList},
            {typeof(UI_BuildInfo), BuildInfo},
        };

        public static string GetKey<T>() where T : UIBase
        {
            return _registered.GetValueOrDefault(typeof(T));
        }
    }
}
