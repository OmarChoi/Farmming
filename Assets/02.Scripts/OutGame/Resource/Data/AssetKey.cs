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
        public const string MarketStall = "MarketStall";

        public static string GetKey(string buildingId) => buildingId switch
        {
            "MarketStall" => MarketStall,
            _ => null
        };
    }

    public static class UI
    {
        public const string Inventory = "UI_Inventory";
        public const string Slot = "UI_Slot";
        public const string HarvestNotification = "HarvestNotification";
    }
}
