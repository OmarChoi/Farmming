using UnityEngine;

// 수확 공룡: IsHarvestable -> 수확
public class HarvestActionAbility : FarmBaseAbility
{
    public override void Interact(TerrainCell cell)
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
}
