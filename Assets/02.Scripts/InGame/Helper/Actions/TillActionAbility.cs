using UnityEngine;

// 개간 곡룡: Ground -> FarmDry
public class TillActionAbility : FarmBaseAbility
{
    public override void InteractPrimary(TerrainCell cell)
    {
        if(cell.FarmTile == null)
        {
            cell.TryConvertToFarm();
            return;
        }

        FarmTile farmTile = GetFarmTile(cell);
        if(farmTile == null)
        {
            return;
        }

        farmTile.Interact();
    }
    
    public override void InteractSecondary(TerrainCell cell)
    {
        // TODO: 우클릭 동작 구현
    }
}
