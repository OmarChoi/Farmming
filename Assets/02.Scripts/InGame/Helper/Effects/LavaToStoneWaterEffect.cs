using UnityEngine;

public class LavaToStoneWaterEffect : IWaterEffect
{
    public bool CanHandle(TerrainCell cell)
    {
        Debug.Log($"TileType: {cell.Data.TileType}");
        return cell.Data.TileType == ETileType.Dungeon2Lava;
    }

    public void Apply(TerrainCell cell)
    {
        Debug.Log("Apply 호출됨");
        LavaTileTransition transition = cell?.GetComponent<LavaTileTransition>();
        Debug.Log($"Transition 찾음: {transition != null}");

        if (transition == null) return;
        transition.StartTransition(cell);
    }
}
