using UnityEngine;

[CreateAssetMenu(fileName = "HelperDataSO", menuName = "Scriptable Objects/HelperDataSO")]
public class HelperDataSO : ScriptableObject
{
    public string HelperId;
    public string HelperName;
    public EHelperType HelperType = EHelperType.Unknown;
    public Sprite HelperIcon;
    public HelperController Prefab;         
    public HelperController EpicPrefab;     
    public HelperController LegendaryPrefab;
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
}
