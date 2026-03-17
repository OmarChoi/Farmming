using UnityEngine;

public class HelperLevel
{
    private HelperDataSO _data;

    public int CurrentLevel { get; private set; }

    public HelperLevel(HelperDataSO data)
    {
        _data = data;
    }

    public void LevelUp()
    {
        if(CurrentLevel >= _data.MaxLevel)
        {
            return;
        }

        CurrentLevel++;
    }

    public float GetEnergyCost()
    {
        return Mathf.Max(1f, _data.BaseEnergyCost - (_data.EnergyReducePerLevel * (CurrentLevel-1)));
    }
}
