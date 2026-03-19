using UnityEngine;

// 수확 공룡: IsHarvestable -> 수확
public class HarvestActionAbility : FarmBaseAbility
{
    public override void InteractPrimary(TerrainCell cell)
    {
        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null)
        {
            return;
        }

        if(!farmTile.HasSeed)
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
