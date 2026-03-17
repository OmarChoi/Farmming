using UnityEngine;

public class TerrainCell : MonoBehaviour
{
    [SerializeField] private GameObject _dirtBlock;
    [SerializeField] private Transform _objectPoint;

    [Header("에디터 초기값 (씬 저장용)")]
    [SerializeField] private ECellType _initialCellType = ECellType.Dirt;
    [SerializeField] private int _initialDirtLevel = 1;
    [SerializeField] private EGridObjectType _initialObjectType = EGridObjectType.None;
    [SerializeField] private int _initialObjectLevel = 0;

    private TerrainCellData _data;
    private FarmTile _farmTile;
    private GameObject _currentObject;

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
        Init(gridPos, new TerrainCellData(_initialCellType, _initialDirtLevel, _initialObjectType, _initialObjectLevel));
    }

    /// 에디터에서 배치 시 직렬화 값도 갱신
    public void SetInitialData(ECellType cellType, int dirtLevel, EGridObjectType objectType, int objectLevel)
    {
        _initialCellType = cellType;
        _initialDirtLevel = dirtLevel;
        _initialObjectType = objectType;
        _initialObjectLevel = objectLevel;
    }

    public void Refresh()
    {
        bool isFarmLand = _data.ObjectType == EGridObjectType.FarmLand;

        _dirtBlock.SetActive(_data.CellType == ECellType.Dirt && !isFarmLand);

        if (_farmTile != null)
            _farmTile.gameObject.SetActive(isFarmLand);
    }

    public bool TryDig(int toolLevel)
    {
        if (!_data.CanDig(toolLevel)) return false;

        _data.Dig();
        Refresh();
        return true;
    }

    public bool TryPlaceBlock(int dirtLevel = 1)
    {
        if (_data.CellType != ECellType.Empty) return false;

        _data.PlaceBlock(dirtLevel);
        Refresh();
        return true;
    }

    public bool TryConvertToFarm()
    {
        if (_data.CellType != ECellType.Dirt) return false;
        if (_data.ObjectType != EGridObjectType.None) return false;

        _data.SetObject(EGridObjectType.FarmLand);
        Refresh();

        if (_farmTile != null)
            _farmTile.Init();

        return true;
    }

    public void SpawnObject(GameObject prefab, EGridObjectType type)
    {
        ClearCurrentObject();
        _data.SetObject(type);
        _currentObject = Instantiate(prefab, _objectPoint.position, Quaternion.identity, _objectPoint);
    }

    public void DestroyObject()
    {
        ClearCurrentObject();
        _data.RemoveObject();
    }

    public void ExportTo(TerrainCellSaveData saveData)
    {
        if (_farmTile != null && _farmTile.gameObject.activeSelf)
        {
            _farmTile.ExportTo(saveData);

            var cropGrowth = _farmTile.GetComponent<CropGrowth>();
            if (cropGrowth != null)
                cropGrowth.ExportTo(saveData);
        }
    }

    public void ImportFarm(TerrainCellSaveData saveData, SeedDatabase seedDb)
    {
        if (_farmTile == null) return;
        if (_data.ObjectType != EGridObjectType.FarmLand) return;

        _farmTile.Init();
        _farmTile.ImportFrom(saveData, seedDb);

        var cropGrowth = _farmTile.GetComponent<CropGrowth>();
        if (cropGrowth != null && !string.IsNullOrEmpty(saveData.SeedId))
        {
            SeedConfig seed = seedDb.GetById(saveData.SeedId);
            if (seed != null)
                cropGrowth.ImportFrom(saveData, seed);
        }
    }

    private void ClearCurrentObject()
    {
        if (_currentObject != null)
            Destroy(_currentObject);

        _currentObject = null;
    }
}