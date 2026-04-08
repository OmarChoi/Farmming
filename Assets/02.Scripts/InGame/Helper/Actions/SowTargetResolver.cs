using System.Collections.Generic;
using UnityEngine;

public sealed class SowTargetResolver
{
    private readonly HelperController _owner;

    public SowTargetResolver(HelperController owner)
    {
        _owner = owner;
    }

    public bool CanCultivate(TerrainCell cell)
    {
        if (cell == null) return false;
        if (cell.Data.CellType != ECellType.Dirt) return false;
        if (cell.Data.ObjectType != EGridObjectType.None && cell.Data.ObjectType != EGridObjectType.FarmLand)
            return false;
        return true;
    }

    public bool CanSow(TerrainCell cell)
    {
        FarmTile farmTile = GetFarmTile(cell);
        return farmTile != null && farmTile.IsReadyToSow;
    }

    public FarmTile GetFarmTile(TerrainCell cell)
    {
        if (cell == null) return null;
        if (cell.FarmTile != null && cell.FarmTile.gameObject.activeSelf)
        {
            return cell.FarmTile;
        }
        return null;
    }

    public bool NeedsFarmConversion(TerrainCell cell)
    {
        return cell.FarmTile == null || !cell.FarmTile.gameObject.activeSelf;
    }

    public List<TerrainCell> GetLateralCells(TerrainCell centerCell)
    {
        var cells = new List<TerrainCell>();
        int extension = _owner.Grade.GetRange() - 1;
        if (extension <= 0)
        {
            return cells;
        }

        Vector3Int rightOffset = GetGridRightOffset();
        for (int i = 1; i <= extension; i++)
        {
            var rightCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition + rightOffset * i);
            var leftCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition - rightOffset * i);
            if (rightCell != null)
            {
                cells.Add(rightCell);
            }
            if (leftCell != null)
            {
                cells.Add(leftCell);
            }
        }
        return cells;
    }

    public List<TerrainCell> GetTargetCells(TerrainCell centerCell)
    {
        var cells = new List<TerrainCell> { centerCell };
        cells.AddRange(GetLateralCells(centerCell));
        return cells;
    }

    public List<TerrainCell> GetOrderedTargetCells(TerrainCell centerCell)
    {
        Vector3Int rightOffset = GetGridRightOffset();
        int extension = _owner.Grade.GetRange() - 1;
        var cells = new List<TerrainCell>();
        for (int i = -extension; i <= extension; i++)
        {
            var cell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition + rightOffset * i);
            if (cell != null) cells.Add(cell);
        }
        return cells;
    }

    public List<FarmTile> GetSowableFarmTiles(TerrainCell centerCell)
    {
        var tiles = new List<FarmTile>();
        foreach (TerrainCell cell in GetTargetCells(centerCell))
        {
            FarmTile tile = GetFarmTile(cell);
            if (tile != null && tile.IsReadyToSow)
            {
                tiles.Add(tile);
            }
        }

        return tiles;
    }

    public TerrainCell GetLateralCell(TerrainCell centerCell, int directionSign)
    {
        Vector3Int rightOffset = GetGridRightOffset();
        return TerrainGridManager.Instance?.GetCell(centerCell.GridPosition + rightOffset * directionSign);
    }

    public void TryConvertLateralCell(TerrainCell centerCell, int directionSign)
    {
        Vector3Int rightOffset = GetGridRightOffset();
        TerrainCell lateralCell = TerrainGridManager.Instance?.GetCell(
            centerCell.GridPosition + rightOffset * directionSign);

        if (lateralCell != null && NeedsFarmConversion(lateralCell))
        {
            lateralCell.TryConvertToFarm();
        }
    }

    public Vector3Int GetGridRightOffset()
    {
        if (_owner.PlayerOwner == null)
        {
            return Vector3Int.right;
        }

        Vector3 right = _owner.PlayerOwner.transform.right;
        return new Vector3Int(Mathf.RoundToInt(right.x), 0, Mathf.RoundToInt(right.z));
    }
}
