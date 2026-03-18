using UnityEngine;

// 파종 곡룡: FarmDry에서 씨앗 주기
public class SowActionAbility : FarmBaseAbility
{
    [SerializeField] private SeedConfig _currentSeed;

    public void SetSeed(SeedConfig seed)
    {
        _currentSeed = seed;
    }

    public override void Interact(TerrainCell cell)
    {
        FarmTile farmTile = GetFarmTile(cell);
        if(farmTile == null)
        {
            Debug.Log("농지가 없음");
            return;
        }

        if(farmTile.StateMachine.CurrentStateType != EFarmTileStateType.FarmDry)
        {
            return;
        }

        if(farmTile.HasSeed)
        {
            return;
        }

        if(_currentSeed == null)
        {
            Debug.Log("씨앗이 선택되지 않음");
            return;
        }

        farmTile.Interact(_currentSeed);
    }
}
