using UnityEngine;

public enum EHelperGrade
{
    Normal,
    Epic,
    Legendary
}
public class HelperGrade
{
    private readonly HelperDataSO _data;

    public EHelperGrade CurrentGrade { get; private set; }

    public HelperGrade(HelperDataSO data)
    {
        _data = data;
    }

    public int GetRange()
    {
        return CurrentGrade switch
        { 
          EHelperGrade.Normal=> _data.NormalRange,
          EHelperGrade.Epic => _data.EpicRange,
          EHelperGrade.Legendary => _data.LegendaryRange,
          _ => 1
        };
    }

    public void Upgrade()
    {
        if(CurrentGrade == EHelperGrade.Legendary)
        {
            return;
        }
        CurrentGrade = (EHelperGrade)((int)CurrentGrade + 1);
    }
}
