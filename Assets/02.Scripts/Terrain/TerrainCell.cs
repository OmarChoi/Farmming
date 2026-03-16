using UnityEngine;

public class TerrainCell : MonoBehaviour
{
    [SerializeField] private GameObject _dirtBlock;
    [SerializeField] private Transform _objectPoint;

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