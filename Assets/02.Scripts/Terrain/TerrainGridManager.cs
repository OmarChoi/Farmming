using System.Collections.Generic;
using UnityEngine;

public class TerrainGridManager : MonoBehaviour
{
    [Header("그리드 설정")]
    [SerializeField] private float _cellSize = 1f;

    [Header("프리팹")]
    [SerializeField] private TerrainCell _cellPrefab;

    [Header("오브젝트 프리팹 (레벨별)")]
    [SerializeField] private GameObject _treePrefab;
    [SerializeField] private GameObject _rockPrefab;

    private TerrainGridData _gridData;
    private readonly Dictionary<Vector3Int, TerrainCell> _cells = new();

    public float CellSize => _cellSize;

    private void Awake()
    {
        CollectExistingCells();
    }

    private void CollectExistingCells()
    {
        _gridData = new TerrainGridData();

        foreach (var cell in GetComponentsInChildren<TerrainCell>())
        {
            _cells[cell.GridPosition] = cell;
            _gridData.SetCell(cell.GridPosition, cell.Data);
        }
    }

    private void SpawnCell(Vector3Int gridPos, TerrainCellData data)
    {
        Vector3 worldPos = GridToWorld(gridPos);
        var cell = Instantiate(_cellPrefab, worldPos, Quaternion.identity, transform);
        cell.name = $"Cell({gridPos.x},{gridPos.y},{gridPos.z})";
        cell.Init(gridPos, data);
        _cells[gridPos] = cell;
    }

    public TerrainCell GetCell(Vector3Int gridPos)
    {
        return _cells.TryGetValue(gridPos, out var cell) ? cell : null;
    }

    public Vector3Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / _cellSize);
        int y = Mathf.RoundToInt(worldPos.y / _cellSize);
        int z = Mathf.RoundToInt(worldPos.z / _cellSize);
        return new Vector3Int(x, y, z);
    }

    public Vector3 GridToWorld(Vector3Int gridPos)
    {
        return new Vector3(gridPos.x * _cellSize, gridPos.y * _cellSize, gridPos.z * _cellSize);
    }

    /// <summary>
    /// 셀 추가/수정. 에디터와 런타임 모두 사용.
    /// </summary>
    public void SetCell(Vector3Int gridPos, TerrainCellData data)
    {
        if (_gridData == null)
            _gridData = new TerrainGridData();

        RemoveCell(gridPos);

        _gridData.SetCell(gridPos, data);
        SpawnCell(gridPos, data);
    }

    /// <summary>
    /// 셀 삭제. 에디터와 런타임 모두 사용.
    /// </summary>
    public void RemoveCell(Vector3Int gridPos)
    {
        if (_cells.TryGetValue(gridPos, out var existing))
        {
            if (existing != null)
            {
                if (Application.isPlaying)
                    Destroy(existing.gameObject);
                else
                    DestroyImmediate(existing.gameObject);
            }

            _cells.Remove(gridPos);
        }

        _gridData?.RemoveCell(gridPos);
    }

    /// <summary>
    /// 전체 셀 삭제.
    /// </summary>
    public void ClearAll()
    {
        foreach (var cell in _cells.Values)
        {
            if (cell == null) continue;

            if (Application.isPlaying)
                Destroy(cell.gameObject);
            else
                DestroyImmediate(cell.gameObject);
        }

        _cells.Clear();
        _gridData = new TerrainGridData();
    }

    /// <summary>
    /// 특정 xz 위치에서 가장 높은 셀의 y값 반환. 없으면 -1.
    /// </summary>
    public int GetTopY(int x, int z)
    {
        int topY = -1;
        foreach (var pos in _cells.Keys)
        {
            if (pos.x == x && pos.z == z && pos.y > topY)
                topY = pos.y;
        }
        return topY;
    }

    public TerrainGridData GetGridData() => _gridData;

    public void LoadFromData(TerrainGridData data)
    {
        foreach (var cell in _cells.Values)
        {
            if (cell != null)
                Destroy(cell.gameObject);
        }
        _cells.Clear();

        _gridData = data;

        foreach (var kvp in _gridData.Cells)
            SpawnCell(kvp.Key, kvp.Value);
    }
}