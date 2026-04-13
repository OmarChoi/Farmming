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
    [SerializeField] private BuildingDatabase _buildingDatabase;

    public SeedDatabase SeedDatabase => _seedDatabase;
    public BuildingDatabase BuildingDatabase => _buildingDatabase;

    private TerrainGridData _gridData;
    private readonly Dictionary<Vector3Int, TerrainCell> _cells = new();
    private int _maxHeight = int.MaxValue;

    public float CellSize => _cellSize;
    public IReadOnlyDictionary<Vector3Int, TerrainCell> Cells => _cells;

    public int MaxHeight => _maxHeight;
    public void SetMaxHeight(int maxHeight) => _maxHeight = maxHeight;

    public void OverrideObjectPrefabs(GameObject tree, GameObject rock)
    {
        if (tree != null) _treePrefab = tree;
        if (rock != null) _rockPrefab = rock;
    }

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

    public bool IsCellAvailableForInteraction(TerrainCell cell)
    {
        if (cell == null || cell.Data == null)
            return false;

        if (!cell.Data.IsTop)
            return false;

        return GetTopY(cell.GridPosition.x, cell.GridPosition.z) == cell.GridPosition.y;
    }

    public TerrainCell GetInteractionCell(Vector3Int gridPos)
    {
        return GetInteractionCell(gridPos, true, out _);
    }

    public TerrainCell GetInteractionCell(Vector3Int gridPos, bool allowBelowFallback, out bool isBelowFallback)
    {
        isBelowFallback = false;

        TerrainCell cell = GetCell(gridPos);
        if (IsCellAvailableForInteraction(cell))
            return cell;

        if (!allowBelowFallback || cell != null)
            return null;

        TerrainCell belowCell = GetCell(gridPos + Vector3Int.down);
        if (!IsCellAvailableForInteraction(belowCell))
            return null;

        isBelowFallback = true;
        return belowCell;
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

    /// 블록 설치. 위에 쌓거나 빈 자리에 설치. 아래 셀의 IsTop도 갱신.
    public bool TryPlaceBlock(Vector3Int gridPos, ETileType tileType, int dirtLevel = 1)
    {
        if (gridPos.y >= _maxHeight) return false;

        var existing = GetCell(gridPos);
        if (existing != null && existing.Data.CellType != ECellType.Empty)
            return false;

        SetCell(gridPos, new TerrainCellData(ECellType.Dirt, tileType, dirtLevel, EGridObjectType.None, 0, false, isTop: true));

        var belowCell = GetCell(gridPos + Vector3Int.down);
        if (belowCell != null && belowCell.Data.IsTop)
        {
            belowCell.Data.SetTop(false);
            belowCell.Refresh();
        }

        return true;
    }

    /// 땅 파기. 위에 블록이 있으면 위 블록을 파고, 그 위에도 있으면 실패.
    public bool TryDig(Vector3Int gridPos, int toolLevel)
    {
        var cell = GetCell(gridPos);
        if (cell == null) return false;

        var abovePos = gridPos + Vector3Int.up;
        var aboveCell = GetCell(abovePos);
        if (aboveCell != null && aboveCell.Data.CellType == ECellType.Dirt)
        {
            var aboveAboveCell = GetCell(abovePos + Vector3Int.up);
            if (aboveAboveCell != null && aboveAboveCell.Data.CellType == ECellType.Dirt)
                return false;

            return DigCell(abovePos, toolLevel);
        }

        return DigCell(gridPos, toolLevel);
    }

    private bool DigCell(Vector3Int gridPos, int toolLevel)
    {
        var cell = GetCell(gridPos);
        if (cell == null) return false;
        if (cell.Data.ObjectType != EGridObjectType.None) return false;
        if (!cell.Data.CanDig(toolLevel)) return false;

        var belowCell = GetCell(gridPos + Vector3Int.down);
        if (belowCell != null && belowCell.Data.CellType == ECellType.Dirt)
        {
            belowCell.Data.SetTop(true);
            belowCell.Refresh();
        }

        RemoveCell(gridPos);
        return true;
    }

    public TerrainCell TryDetachForAnimation(Vector3Int gridPos, int toolLevel)
    {
        var cell = GetCell(gridPos);
        if (cell == null) return null;

        var abovePos = gridPos + Vector3Int.up;
        var aboveCell = GetCell(abovePos);
        if (aboveCell != null && aboveCell.Data.CellType == ECellType.Dirt)
        {
            var aboveAboveCell = GetCell(abovePos + Vector3Int.up);
            if (aboveAboveCell != null && aboveAboveCell.Data.CellType == ECellType.Dirt)
                return null;

            return DetachCell(abovePos, toolLevel);
        }

        return DetachCell(gridPos, toolLevel);
    }

    private TerrainCell DetachCell(Vector3Int gridPos, int toolLevel)
    {
        var cell = GetCell(gridPos);
        if (cell == null) return null;
        if (cell.Data.ObjectType != EGridObjectType.None) return null;
        if (!cell.Data.CanDig(toolLevel)) return null;

        var belowCell = GetCell(gridPos + Vector3Int.down);
        if (belowCell != null && belowCell.Data.CellType == ECellType.Dirt)
        {
            belowCell.Data.SetTop(true);
            belowCell.Refresh();
        }

        _cells.Remove(gridPos);
        _gridData?.RemoveCell(gridPos);

        return cell;
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

    public bool TryBuildCellDelta(Vector3Int gridPos, out TerrainCellDelta delta)
    {
        delta = new TerrainCellDelta
        {
            X = gridPos.x,
            Y = gridPos.y,
            Z = gridPos.z
        };

        TerrainCell cell = GetCell(gridPos);
        if (cell == null)
        {
            delta.RemoveCell = true;
            delta.Cell = null;
            return true;
        }

        delta.RemoveCell = false;
        delta.Cell = ExportCellSaveData(gridPos);
        return delta.Cell != null;
    }

    public TerrainCellSaveData ExportCellSaveData(Vector3Int gridPos)
    {
        TerrainCell cell = GetCell(gridPos);
        if (cell == null)
            return null;

        return CreateCellSaveData(gridPos, cell);
    }

    public void ApplyCellDelta(TerrainCellDelta delta)
    {
        if (delta == null) return;

        Vector3Int gridPos = new(delta.X, delta.Y, delta.Z);
        if (delta.RemoveCell)
        {
            RemoveCell(gridPos);
            return;
        }

        if (delta.Cell == null) return;

        var cellData = new TerrainCellData(
            delta.Cell.CellType,
            delta.Cell.TileType,
            delta.Cell.DirtLevel,
            delta.Cell.ObjectType,
            delta.Cell.ObjectLevel,
            delta.Cell.IsIndestructible,
            delta.Cell.IsTop);

        SetCell(gridPos, cellData);

        if (delta.Cell.Farm != null &&
            _cells.TryGetValue(gridPos, out TerrainCell cell) &&
            cell != null &&
            cell.Data.ObjectType == EGridObjectType.FarmLand)
        {
            cell.ImportFarm(delta.Cell, _seedDatabase);
        }
    }

    public void ApplyCellDeltas(IEnumerable<TerrainCellDelta> deltas)
    {
        if (deltas == null) return;

        foreach (TerrainCellDelta delta in deltas)
            ApplyCellDelta(delta);
    }

    public TerrainSaveData ExportSaveData()
    {
        var saveData = new TerrainSaveData();

        foreach (var kvp in _cells)
        {
            TerrainCellSaveData cellSave = CreateCellSaveData(kvp.Key, kvp.Value);
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

    private static TerrainCellSaveData CreateCellSaveData(Vector3Int gridPos, TerrainCell cell)
    {
        var saveData = new TerrainCellSaveData
        {
            X = gridPos.x,
            Y = gridPos.y,
            Z = gridPos.z,
            CellType = cell.Data.CellType,
            TileType = cell.Data.TileType,
            DirtLevel = cell.Data.DirtLevel,
            ObjectType = cell.Data.ObjectType == EGridObjectType.Building
                ? EGridObjectType.None : cell.Data.ObjectType,
            ObjectLevel = cell.Data.ObjectType == EGridObjectType.Building
                ? 0 : cell.Data.ObjectLevel,
            IsIndestructible = cell.Data.IsIndestructible,
            IsTop = cell.Data.IsTop
        };

        cell.ExportTo(saveData);
        return saveData;
    }
}
