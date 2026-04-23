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
        public const string Villager1 = "Villager1";
        public const string Villager2 = "Villager2";
        public const string Villager3 = "Villager3";
        public const string Villager4 = "Villager4";
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
        public const string QuestBoard = "QuestBoard";
        public const string Portal = "Portal";
        public const string GamblingBuilding = "GamblingBuilding";
        public const string Shrine = "Shrine";
        public const string Hospital = "Hospital";
        public const string House1 = "House1";
        public const string House2 = "House2";
        public const string House3 = "House3";
        public const string House4 = "House4";

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

    public static class BGM
    {
        public const string StartScene = "BGMStartScene";
        public const string Village = "BGMVillage";
        public const string Dungeon1 = "Dungeon1Bgm";
        public const string HelperEvolution = "HelperEvolutionBGM";
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
        public const string VillageLevelUp = "UI_VillageLevelUp";
        public const string Pause = "UI_Pause";
        public const string VillageInfoPopup = "UI_VillageInfoPopup";
        public const string Setting = "UI_Setting";
        public const string TradeAmountPopup = "UI_TradeAmountPopup";
        public const string DinoRace = "UI_DinoRace";

        private static readonly Dictionary<Type, string> _registered = new Dictionary<Type, string>
        {
            {typeof(UI_BuildingList), BuildingList},
            {typeof(UI_BuildInfo), BuildInfo},
            {typeof(UI_Shrine), Shrine},
            {typeof(UI_VillageLevelUp), VillageLevelUp},
            {typeof(UI_QuestBoard), QuestBoard},
            {typeof(UI_Pause), Pause},
            {typeof(UI_VillageInfoPopup), VillageInfoPopup},
            {typeof(UI_Setting), Setting},
            {typeof(UI_NpcDialogue), NpcDialogue},
            {typeof(UI_DinoRace), DinoRace},
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
        public const string SowNormalCultivate = "SFXsownormalcultivate";
        public const string SowEpicCultivate = "SFXsowepiccultivate";
        public const string SowLegendaryCultivate = "SFXsowlegendarycultivate";
        public const string WaterNormalSplash = "SFXwaternormal";
        public const string WaterEpicSplash = "SFXwaterepic";
        public const string WaterLegendarySplash = "SFXwaterlegendary";
        public const string WoodLeaf = "SFXwoodleaf";
        public const string WoodNormalHelper = "NormalWoodHelper";
        public const string WoodEpicHelper = "EpicWoodHelper";
        public const string WoodLegendaryHelper = "LegendaryWoodHelper";
        public const string WoodRangeNormalHelper = "RI-NormalWoodHelper";
        public const string WoodRangeEpicHelper = "RI-EpicWoodHelper";
        public const string WoodRangeLegendaryHelper = "RI-LegendaryWoodHelper";
        public const string DirtSizzleLava = "DirtSizzle-Lava";
        public const string HelperSummon = "SFXhelpersummon";
        public const string HelperEquip = "SFXhelperequip";
        public const string HelperUnequip = "SFXhelperunequip";
        public const string HelperFollow = "SFXhelperfollow";
        public const string HelperEvolution = "HelperEvolution";

        #endregion

        #region Dungeon
        public const string ChestOpen = "SFXchestopen";
        public const string MonkeyDetect = "SFXmonkeydetect";
        public const string MonkeyTrouble = "SFXmonkeytrouble";
        public const string MushroomDetect = "SFXmushroomdetect";
        public const string MushroomTrouble = "SFXmushroomtrouble";

        #endregion
    }
}
