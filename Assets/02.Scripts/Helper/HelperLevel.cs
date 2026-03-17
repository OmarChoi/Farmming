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
}
