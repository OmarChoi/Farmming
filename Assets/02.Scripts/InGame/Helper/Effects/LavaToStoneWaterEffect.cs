using UnityEngine;

public class LavaToStoneWaterEffect : IWaterEffect
{
    public bool CanHandle(TerrainCell cell)
    {
        return cell.Data.TileType == ETileType.Dungeon2Lava;
    }

    public void Apply(TerrainCell cell)
    {
        Vector3Int pos = cell.GridPosition;

        TerrainCellData newData = new TerrainCellData(
            cell.Data.CellType,
            ETileType.Dungeon2Stone,
            cell.Data.DirtLevel,
            cell.Data.ObjectType,
            cell.Data.ObjectLevel,
            cell.Data.IsIndestructible,
            cell.Data.IsTop
        );

        TerrainGridManager.Instance.SetCell(pos, newData);
    }
}
