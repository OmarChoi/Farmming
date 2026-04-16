using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class AssetKey
{
    public static class Prefab
    {
        public const string FarmTile = "FarmTile";
        public const string TerrainCell = "TerrainCell";
        public const string Player = "Player";
        public const string UnlockedItem = "UnlockedItem";
    }

    /// <summary>
    /// PhotonNetwork.Instantiate와 Addressables preload가 공유하는 네트워크 프리팹 키를 제공한다.
    /// </summary>
    public static class NetworkPrefab
    {
        public const string Player = "Player";
        public const string Blacksmith = "Blacksmith";
        public const string Hairdresser = "Hairdresser";
        public const string Merchant = "Merchant";
        public const string Gambler = "Gambler";
        public const string Doctor = "Doctor";
        public const string GroundHelper = "GroundHelper";
        public const string LightHelper = "LightHelper";
        public const string HarvestHelper = "HarvestHelper";
        public const string HarvestEpicHelper = "HarvestEpicHelper";
        public const string HarvestLegendaryHelper = "HarvestLegendaryHelper";
        public const string SowHelper = "SowHelper";
        public const string SowEpicHelper = "SowEpicHelper";
        public const string SowLegendaryHelper = "SowLegendaryHelper";
        public const string WaterHelper = "WaterHelper";
        public const string WaterEpicHelper = "WaterEpicHelper";
        public const string WaterLegendaryHelper = "WaterLegendaryHelper";
        public const string WoodCuttingHelper = "WoodCuttingHelper";
        public const string WoodCuttingEpicHelper = "WoodCuttingEpicHelper";
        public const string WoodCuttingLegendaryHelper = "WoodCuttingLegendaryHelper";
        public const string JungleTroublemaker = "JungleTroublemaker";
        public const string CaveTroublemaker1 = "CaveTroublemaker1";
        public const string MarketStall = "MarketStall";
        public const string Smithy = "Smithy";
        public const string HairShop = "HairShop";
        public const string Storage = "Storage";
        public const string GamblingBuilding = "GamblingBuilding";
        public const string Shrine = "Shrine";

        // 상수를 추가할 때 수동 누락이 없도록 public const string 필드를 리플렉션으로 수집한다.
        private static readonly string[] _all = typeof(NetworkPrefab)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue())
            .ToArray();

        private static readonly HashSet<string> _registered = new HashSet<string>(_all);

        /// <summary>
        /// preload/Addressables 검증에 쓰이는 전체 네트워크 프리팹 키 목록.
        /// </summary>
        public static IReadOnlyList<string> All => _all;

        /// <summary>
        /// 주어진 키가 등록된 네트워크 프리팹인지 확인한다.
        /// </summary>
        public static bool Contains(string key)
        {
            return !string.IsNullOrEmpty(key) && _registered.Contains(key);
        }

        /// <summary>
        /// 프리팹 GameObject의 이름을 네트워크 프리팹 키로 검증해 반환한다.
        /// </summary>
        public static string GetKey(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("[AssetKey.NetworkPrefab] Prefab is null.");
                return null;
            }

            return GetKey(prefab.name);
        }

        /// <summary>
        /// 컴포넌트에서 소속 GameObject의 이름을 네트워크 프리팹 키로 검증해 반환한다.
        /// </summary>
        public static string GetKey(Component prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("[AssetKey.NetworkPrefab] Prefab component is null.");
                return null;
            }

            return GetKey(prefab.gameObject);
        }

        /// <summary>
        /// 문자열 이름이 등록된 네트워크 프리팹 키와 일치하는지 검증해 반환한다.
        /// </summary>
        public static string GetKey(string prefabName)
        {
            if (Contains(prefabName)) return prefabName;

            Debug.LogError($"[AssetKey.NetworkPrefab] Unregistered network prefab key: {prefabName}");
            return null;
        }
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
        private const string Shrine = "Shrine";
        private const string Storage = "Storage";
        private const string GamblingBuilding = "GamblingBuilding";

        private static readonly HashSet<string> _registered = new HashSet<string>
        {
            MarketStall,
            Smithy,
            HairShop,
            Shrine,
            Storage,
            GamblingBuilding
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
        public const string Shrine = "UI_Shrine";
        public const string VillageState = "UI_VillageState";
        public const string VillageLevelUp = "UI_VillageLevelUp";

        private static readonly Dictionary<Type, string> _registered = new Dictionary<Type, string>
        {
            {typeof(UI_BuildingList), BuildingList},
            {typeof(UI_BuildInfo), BuildInfo},
            {typeof(UI_Shrine), Shrine},
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
