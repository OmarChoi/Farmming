using UnityEngine;

// 관수 곡룡: FarmDry => FarmWet
public class WaterActionAbility : FarmBaseAbility
{
    public override void Interact(TerrainCell cell)
    {
        FarmTile farmTile = GetFarmTile(cell);
        if(farmTile == null)
        {
            return;
        }

        if(farmTile.StateMachine.CurrentStateType != EFarmTileStateType.FarmDry)
        {
            Debug.Log("물을 줄 수 없는 상태");
            return;
        }

        if(!farmTile.HasSeed)
        {
            Debug.Log("씨앗없음");
            return;
        }

        farmTile.Interact();
    }
}
