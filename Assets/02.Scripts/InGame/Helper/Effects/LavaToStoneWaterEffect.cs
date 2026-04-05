using UnityEngine;

public class LavaToStoneWaterEffect : IWaterEffect
{
    public bool CanHandle(TerrainCell cell)
    {
        return cell.Data.TileType == ETileType.Dungeon3Lava;
    }

    public void Apply(TerrainCell cell)
    {
        LavaTileTransition transition = cell?.GetComponent<LavaTileTransition>();

        if (transition == null) return;
        transition.StartTransition(cell);
    }
}
