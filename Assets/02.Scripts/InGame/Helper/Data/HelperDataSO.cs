using UnityEngine;

[CreateAssetMenu(fileName = "HelperDataSO", menuName = "Scriptable Objects/HelperDataSO")]
public class HelperDataSO : ScriptableObject
{
    public string HelperId;
    public string HelperName;
    public Sprite HelperIcon;
    public HelperController Prefab;

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

    [SerializeField] private Sprite _leftClickIcon;
    [SerializeField] private Sprite _rightClickIcon;
    [SerializeField] private string _leftClickExplanation;
    [SerializeField] private string _rightClickExplanation;

    public Sprite LeftClickIcon => _leftClickIcon;
    public Sprite RightClickIcon => _rightClickIcon;
    public string LeftClickExplanation => _leftClickExplanation;
    public string RightClickExplanation => _rightClickExplanation;
}