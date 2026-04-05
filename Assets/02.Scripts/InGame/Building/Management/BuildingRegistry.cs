using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배치된 건물의 런타임 상태(세이브 데이터, 점유 셀 매핑, 스폰 인스턴스)를 소유한다.
/// 순수 Domain — Unity 라이프사이클/네트워크/그리드에 대한 지식을 갖지 않는다.
/// </summary>
public class BuildingRegistry
{
    // 앵커 좌표 -> 세이브 데이터. 철거/영속화/복원에 사용.
    private readonly Dictionary<Vector3Int, BuildingSaveData> _buildings = new Dictionary<Vector3Int, BuildingSaveData>();
    // 점유 셀 -> 앵커 좌표. 푸트프린트 내 임의 셀에서 건물 전체를 역참조하기 위한 매핑.
    private readonly Dictionary<Vector3Int, Vector3Int> _occupiedCells = new Dictionary<Vector3Int, Vector3Int>();
    // 앵커 좌표 -> 런타임 건물 인스턴스.
    private readonly Dictionary<Vector3Int, BaseBuilding> _instances = new Dictionary<Vector3Int, BaseBuilding>();

    public IEnumerable<KeyValuePair<Vector3Int, BuildingSaveData>> AllBuildings => _buildings;
    public int Count => _buildings.Count;

    #region Query
    public bool IsOccupied(Vector3Int cell) => _occupiedCells.ContainsKey(cell);

    public bool TryGetAnchor(Vector3Int anyCell, out Vector3Int anchor)
        => _occupiedCells.TryGetValue(anyCell, out anchor);

    public bool TryGetSaveData(Vector3Int anchor, out BuildingSaveData saveData)
        => _buildings.TryGetValue(anchor, out saveData);

    public bool TryGetInstance(Vector3Int anchor, out BaseBuilding instance)
        => _instances.TryGetValue(anchor, out instance);

    public bool IsConstructionComplete(Vector3Int anyCell)
    {
        if (!_occupiedCells.TryGetValue(anyCell, out Vector3Int anchor)) return false;
        if (!_buildings.TryGetValue(anchor, out BuildingSaveData saveData)) return false;
        return saveData.RemainingDays <= 0;
    }
    #endregion

    #region Mutation
    public void RegisterBuilding(Vector3Int anchor, BuildingSaveData saveData)
    {
        _buildings[anchor] = saveData;
    }

    public void RegisterInstance(Vector3Int anchor, BaseBuilding instance)
    {
        _instances[anchor] = instance;
    }

    public void MarkCell(Vector3Int cell, Vector3Int anchor)
    {
        _occupiedCells[cell] = anchor;
    }

    public bool RemoveBuilding(Vector3Int anchor) => _buildings.Remove(anchor);

    public bool RemoveInstance(Vector3Int anchor, out BaseBuilding instance)
        => _instances.Remove(anchor, out instance);

    public void UnmarkCell(Vector3Int cell)
    {
        _occupiedCells.Remove(cell);
    }
    #endregion

    #region Export
    public List<BuildingSaveData> ExportAll() => new List<BuildingSaveData>(_buildings.Values);
    #endregion
}
