using UnityEngine;

[CreateAssetMenu(fileName = "HelperDataSO", menuName = "Scriptable Objects/HelperDataSO")]
public class HelperDataSO : ScriptableObject
{
    public string HelperId;
    public string HelperName;
    public Sprite HerlerIcon;

    public float MaxEnergy = 100f;
    public float EnergyRecoveryPerSecond = 5f;

    public int MaxLevel = 10;

    public int NormalRange = 1;
    public int EpicRange = 2;
    public int LegendaryRange = 3;
}
