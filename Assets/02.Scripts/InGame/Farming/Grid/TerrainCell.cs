using UnityEngine;

public class TerrainCell : MonoBehaviour
{
    [SerializeField] private GameObject _dirtBlock;
    [SerializeField] private GameObject _grassBlock;
    [SerializeField] private Transform _objectPoint;

    [Header("에디터 초기값 (씬 저장용)")]
    [SerializeField] private ECellType _initialCellType = ECellType.Dirt;
    [SerializeField] private ETileType _initialTileType = ETileType.VillageDirt;
    [SerializeField] private int _initialDirtLevel = 1;
    [SerializeField] private EGridObjectType _initialObjectType = EGridObjectType.None;
    [SerializeField] private int _initialObjectLevel = 0;

    private TerrainCellData _data;
    private FarmTile _farmTile;
    public GameObject CurrentObject { get; private set; }

    public Vector3Int GridPosition { get; private set; }
    public TerrainCellData Data => _data;
    public FarmTile FarmTile => _farmTile;

    public void Init(Vector3Int gridPos, TerrainCellData data)
    {
        GridPosition = gridPos;
        _data = data;
        _farmTile = GetComponentInChildren<FarmTile>(true);
        Refresh();
    }

    /// 에디터 직렬화 값으로 초기화 (CollectExistingCells용)
    public void InitFromSerializedData(Vector3Int gridPos)
    {
        Init(gridPos, new TerrainCellData(_initialCellType, _initialTileType, _initialDirtLevel, _initialObjectType, _initialObjectLevel));
    }

    /// 에디터에서 배치 시 직렬화 값도 갱신
    public void SetInitialData(ECellType cellType, ETileType tileType, int dirtLevel, EGridObjectType objectType, int objectLevel)
    {
        _initialCellType = cellType;
        _initialTileType = tileType;
        _initialDirtLevel = dirtLevel;
        _initialObjectType = objectType;
        _initialObjectLevel = objectLevel;
    }

    public void Refresh()
    {
        bool isFarmLand = _data.ObjectType == EGridObjectType.FarmLand;
        bool isDirt = _data.CellType == ECellType.Dirt && !isFarmLand;

        if (_dirtBlock != null)
            _dirtBlock.SetActive(isDirt && !_data.IsTop);

        if (_grassBlock != null)
            _grassBlock.SetActive(isDirt && _data.IsTop);

        if (_farmTile != null)
            _farmTile.gameObject.SetActive(isFarmLand);
    }

    public bool TryDig(int toolLevel)
    {
        // 위에 블록이 있으면 그 블록을 대신 파되, 그 위에도 블록이 있으면 아무것도 안 캠
        var abovePos = GridPosition + Vector3Int.up;
        var aboveCell = TerrainGridManager.Instance.GetCell(abovePos);
        if (aboveCell != null && aboveCell.Data.CellType == ECellType.Dirt)
        {
            var aboveAbovePos = abovePos + Vector3Int.up;
            var aboveAboveCell = TerrainGridManager.Instance.GetCell(aboveAbovePos);
            if (aboveAboveCell != null && aboveAboveCell.Data.CellType == ECellType.Dirt)
                return false;

            return aboveCell.DigSelf(toolLevel);
        }

        return DigSelf(toolLevel);
    }

    private bool DigSelf(int toolLevel)
    {
        if (!_data.CanDig(toolLevel)) return false;

        // 아래 셀을 풀블록으로 전환
        var belowPos = GridPosition + Vector3Int.down;
        var belowCell = TerrainGridManager.Instance.GetCell(belowPos);
        if (belowCell != null && belowCell.Data.CellType == ECellType.Dirt)
        {
            belowCell.Data.SetTop(true);
            belowCell.Refresh();
        }
        TerrainGridManager.Instance.RemoveCell(GridPosition);

        return true;
    }

    public bool TryPlaceBlock(int dirtLevel = 1)
    {
        if (_data.CellType != ECellType.Empty) return false;

        _data.PlaceBlock(dirtLevel);
        _data.SetTop(true);
        Refresh();

        var belowPos = GridPosition + Vector3Int.down;
        var belowCell = TerrainGridManager.Instance.GetCell(belowPos);
        if (belowCell != null && belowCell.Data.IsTop)
        {
            belowCell.Data.SetTop(false);
            belowCell.Refresh();
        }

        return true;
    }

    public bool TryConvertToFarm()
    {
        if (_farmTile == null) return false;
        if (_data.CellType != ECellType.Dirt) return false;
        if (_data.ObjectType != EGridObjectType.None) return false;

        _data.SetObject(EGridObjectType.FarmLand);
        Refresh();
        _farmTile.Init();

        return true;
    }

    public void SpawnObject(GameObject prefab, EGridObjectType type)
    {
        ClearCurrentObject();
        _data.SetObject(type);
        CurrentObject = Instantiate(prefab, _objectPoint.position, Quaternion.identity, _objectPoint);
    }

    public void DestroyObject()
    {
        ClearCurrentObject();
        _data.RemoveObject();
    }

    public void ExportTo(TerrainCellSaveData saveData)
    {
        if (_farmTile != null && _farmTile.gameObject.activeSelf)
            _farmTile.ExportTo(saveData);
    }

    public void ImportFarm(TerrainCellSaveData saveData, SeedDatabase seedDb)
    {
        if (_farmTile == null) return;
        if (_data.ObjectType != EGridObjectType.FarmLand) return;

        _farmTile.Init();
        _farmTile.ImportFrom(saveData, seedDb);
    }

    private void ClearCurrentObject()
    {
        if (CurrentObject != null)
            Destroy(CurrentObject);

        CurrentObject = null;
    }
}