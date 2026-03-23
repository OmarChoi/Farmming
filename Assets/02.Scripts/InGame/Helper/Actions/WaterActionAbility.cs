using UnityEngine;

// 관수 곡룡: FarmDry => FarmWet
public class WaterActionAbility : FarmBaseAbility
{
    public override void InteractPrimary(TerrainCell cell)
    {
        FarmTile farmTile = GetFarmTile(cell);
        if(farmTile == null) return;
        if(farmTile.StateMachine.CurrentStateType != EFarmTileStateType.FarmDry) return;
        if(!farmTile.HasSeed) return;

        _owner.BeginAction();
        farmTile.Interact();
        _owner.EndAction();
    }

    public override void InteractSecondary(TerrainCell cell)
    {
        // TODO: 우클릭 동작 구현
    }
}
