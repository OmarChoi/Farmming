using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TerrainGridData
{
    private readonly Dictionary<Vector3Int, TerrainCellData> _cells = new();

    public IReadOnlyDictionary<Vector3Int, TerrainCellData> Cells => _cells;

    public TerrainCellData GetCell(Vector3Int pos)
    {
        return _cells.TryGetValue(pos, out var cell) ? cell : null;
    }

    public void SetCell(Vector3Int pos, TerrainCellData data)
    {
        _cells[pos] = data;
    }

    public void RemoveCell(Vector3Int pos)
    {
        _cells.Remove(pos);
    }

    public bool HasCell(Vector3Int pos)
    {
        return _cells.ContainsKey(pos);
    }
}