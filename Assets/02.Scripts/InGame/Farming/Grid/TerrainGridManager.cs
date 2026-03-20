using System.Collections.Generic;
using UnityEngine;

public class TerrainGridManager : MonoBehaviour
{
    public static TerrainGridManager Instance { get; private set; }

    [Header("그리드 설정")]
    [SerializeField] private float _cellSize = 2f;

    [Header("타일 프리팹")]
    [SerializeField] private TilePrefabDatabase _tileDatabase;
    [SerializeField] private TerrainCell _defaultCellPrefab;

    [Header("오브젝트 프리팹 (레벨별)")]
    [SerializeField] private GameObject _treePrefab;
    [SerializeField] private GameObject _rockPrefab;

    [Header("데이터베이스")]
    [SerializeField] private SeedDatabase _seedDatabase;

    private TerrainGridData _gridData;
    private readonly Dictionary<Vector3Int, TerrainCell> _cells = new();

    public float CellSize => _cellSize;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CollectExistingCells();
    }

    public void CollectExistingCells()
    {
        _cells.Clear();
        _gridData = new TerrainGridData();

        foreach (var cell in GetComponentsInChildren<TerrainCell>())
        {
            Vector3Int gridPos = WorldToGrid(cell.transform.position);

            if (Application.isPlaying)
            {
                cell.InitFromSerializedData(gridPos);

                GameObject objPrefab = GetObjectPrefab(cell.Data.ObjectType);
                if (objPrefab != null)
                    cell.SpawnObject(objPrefab, cell.Data.ObjectType);
            }

            _cells[gridPos] = cell;
            if (cell.Data != null)
                _gridData.SetCell(gridPos, cell.Data);
        }
    }

    private void SpawnCell(Vector3Int gridPos, TerrainCellData data)
    {
        GameObject prefab = _tileDatabase != null
            ? _tileDatabase.GetPrefab(data.TileType)
            : null;
        if (prefab == null && _defaultCellPrefab != null)
            prefab = _defaultCellPrefab.gameObject;

        Vector3 worldPos = GridToWorld(gridPos);
        if (prefab == null)
        {
            Debug.LogError($"Prefab for tile type {data.TileType} not found and no default prefab is set. Skipping cell at {gridPos}.");
            return;
        }
        var cellObj = Instantiate(prefab, worldPos, Quaternion.identity, transform);
        var cell = cellObj.GetComponent<TerrainCell>();
        cell.name = $"Cell({gridPos.x},{gridPos.y},{gridPos.z})";
        cell.Init(gridPos, data);
        cell.SetInitialData(data.CellType, data.TileType, data.DirtLevel, data.ObjectType, data.ObjectLevel);
        _cells[gridPos] = cell;

        GameObject objPrefab = GetObjectPrefab(data.ObjectType);
        if (objPrefab != null)
            cell.SpawnObject(objPrefab, data.ObjectType);
    }

    public GameObject GetObjectPrefab(EGridObjectType type)
    {
        return type switch
        {
            EGridObjectType.Tree => _treePrefab,
            EGridObjectType.Rock => _rockPrefab,
            _ => null
        };
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

    /// 셀 추가/수정. 에디터와 런타임 모두 사용.
    public void SetCell(Vector3Int gridPos, TerrainCellData data)
    {
        if (_gridData == null)
            _gridData = new TerrainGridData();

        RemoveCell(gridPos);

        _gridData.SetCell(gridPos, data);
        SpawnCell(gridPos, data);
    }

    /// 셀 삭제. 에디터와 런타임 모두 사용.
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

    /// 전체 셀 삭제.
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

    /// 특정 xz 위치에서 가장 높은 셀의 y값 반환. 없으면 -1.
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

    public TerrainSaveData ExportSaveData()
    {
        var saveData = new TerrainSaveData();

        foreach (var kvp in _cells)
        {
            var cellSave = new TerrainCellSaveData
            {
                X = kvp.Key.x,
                Y = kvp.Key.y,
                Z = kvp.Key.z,
                CellType = kvp.Value.Data.CellType,
                TileType = kvp.Value.Data.TileType,
                DirtLevel = kvp.Value.Data.DirtLevel,
                ObjectType = kvp.Value.Data.ObjectType,
                ObjectLevel = kvp.Value.Data.ObjectLevel,
                IsIndestructible = kvp.Value.Data.IsIndestructible,
                IsTop = kvp.Value.Data.IsTop
            };

            kvp.Value.ExportTo(cellSave);
            saveData.Cells.Add(cellSave);
        }

        return saveData;
    }

    public void ImportSaveData(TerrainSaveData saveData)
    {
        ClearAll();
        if (saveData == null) return;

        foreach (var cellData in saveData.Cells)
        {
            var gridPos = new Vector3Int(cellData.X, cellData.Y, cellData.Z);
            var data = new TerrainCellData(cellData.CellType, cellData.TileType, cellData.DirtLevel, cellData.ObjectType, cellData.ObjectLevel, cellData.IsIndestructible, cellData.IsTop);

            _gridData.SetCell(gridPos, data);
            SpawnCell(gridPos, data);

            if (data.ObjectType == EGridObjectType.FarmLand)
                _cells[gridPos].ImportFarm(cellData, _seedDatabase);
        }
    }

    public void LoadFromData(TerrainGridData data)
    {
        ClearAll();
        if (data == null) return;

        _gridData = data;

        foreach (var kvp in _gridData.Cells)
            SpawnCell(kvp.Key, kvp.Value);
    }
}