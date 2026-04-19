using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HelperDataSO", menuName = "Scriptable Objects/HelperDataSO")]
public class HelperDataSO : ScriptableObject
{
    public string HelperId;
    public string HelperName;
    public EHelperType HelperType = EHelperType.Unknown;
    public Sprite HelperIcon;

    [Header("Helper Grade Icons")]
    [SerializeField] private Sprite _normalIcon;
    [SerializeField] private Sprite _epicIcon;
    [SerializeField] private Sprite _legendaryIcon;

    public HelperController Prefab;         
    public HelperController EpicPrefab;     
    public HelperController LegendaryPrefab;

    [Header("Evolution Preview Prefabs")]
    [SerializeField] private GameObject _normalEvolutionPreviewPrefab;
    [SerializeField] private GameObject _epicEvolutionPreviewPrefab;
    [SerializeField] private GameObject _legendaryEvolutionPreviewPrefab;

    public HelperEvolutionProfileSO EvolutionProfile;

    public HelperController GetPrefabForGrade(EHelperGrade grade)
    {
        return grade switch
        {
            EHelperGrade.Epic      => EpicPrefab      != null ? EpicPrefab      : Prefab,
            EHelperGrade.Legendary => LegendaryPrefab != null ? LegendaryPrefab : Prefab,
            _                      => Prefab
        };
    }

    public Sprite GetIconForGrade(EHelperGrade grade)
    {
        return grade switch
        {
            EHelperGrade.Legendary => _legendaryIcon != null ? _legendaryIcon : GetEpicIcon(),
            EHelperGrade.Epic => GetEpicIcon(),
            _ => GetNormalIcon()
        };
    }

    private Sprite GetNormalIcon()
    {
        return _normalIcon != null ? _normalIcon : HelperIcon;
    }

    private Sprite GetEpicIcon()
    {
        return _epicIcon != null ? _epicIcon : GetNormalIcon();
    }

    public GameObject GetEvolutionPreviewPrefab(EHelperGrade grade)
    {
        return grade switch
        {
            EHelperGrade.Epic => _epicEvolutionPreviewPrefab,
            EHelperGrade.Legendary => _legendaryEvolutionPreviewPrefab,
            _ => _normalEvolutionPreviewPrefab
        };
    }

    public float MaxEnergy = 100f;
    public float EnergyRecoveryPerSecond = 5f;

    public int GatherDamage = 10;
    public float StaminaCost = 5f;

    public int MaxLevel = 10;
    public float BaseEnergyCost = 10f;
    public float EnergyReducePerLevel = 1f;

    public int NormalRange = 1;
    public int EpicRange = 2;
    public int LegendaryRange = 3;

    public int NormalMaxExp = 500;
    public int EpicMaxExp = 1000;

    public EHelperGrade SecondaryUnlockGrade = EHelperGrade.Normal;
    public bool IsLightHelper = false;

    public bool UsesLegendaryMaxExpBar
        => HelperType == EHelperType.Water
        || HelperType == EHelperType.Harvest
        || HelperType == EHelperType.WoodCuttingMine
        || HelperType == EHelperType.Sow;

    public bool UsesGroundFixedExpBar => HelperType == EHelperType.Ground;

    [SerializeField] private Sprite _leftClickIcon;
    [SerializeField] private Sprite _rightClickIcon;
    [SerializeField] private string _leftClickExplanation;
    [SerializeField] private string _rightClickExplanation;

    public Sprite LeftClickIcon => _leftClickIcon;
    public Sprite RightClickIcon => _rightClickIcon;
    public string LeftClickExplanation => _leftClickExplanation;
    public string RightClickExplanation => _rightClickExplanation;

    [SerializeField] private HelperUpgradeCostGroup[] _upgradeCosts;

    public IReadOnlyList<HelperUpgradeCostGroup> UpgradeCosts => _upgradeCosts;

    // 업그레이드에 필요한 아이템 비용을 가져오는 메서드입니다.
    public IReadOnlyList<HelperUpgradeCostEntry> GetUpgradeCosts(EHelperGrade fromGrade)
    {
        if (_upgradeCosts == null) return Array.Empty<HelperUpgradeCostEntry>();

        for (int i = 0; i < _upgradeCosts.Length; i++)
        {
            if (_upgradeCosts[i].FromGrade == fromGrade) return _upgradeCosts[i].Costs;
        }

        return Array.Empty<HelperUpgradeCostEntry>();
    }

    // 업그레이드에 필요한 골드 비용을 가져오는 메서드입니다.
    public int GetUpgradeGoldCost(EHelperGrade fromGrade)
    {
        if (_upgradeCosts == null) return 0;

        for (int i = 0; i < _upgradeCosts.Length; i++)
        {
            if (_upgradeCosts[i].FromGrade == fromGrade) return _upgradeCosts[i].GoldCost;
        }

        return 0;
    }
}
