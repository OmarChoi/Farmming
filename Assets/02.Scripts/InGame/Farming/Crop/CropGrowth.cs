using UnityEngine;
using UnityEngine.Tilemaps;

public class CropGrowth : MonoBehaviour
{
    private SeedItemDataSO _seedConfig;
    private int _currentStageIndex = 0;
    private int _elapsedDays = 0;
    private bool _isGrowing = false;
    private bool _hasStarted = false;
    private GameObject _currentCropObject;

    private FarmTile _tile;

    private PlayerInventoryAbility _inventoryAbility;

    public bool HasStarted => _hasStarted;
    public bool IsHarvestable => !_isGrowing && _hasStarted;

    private void Awake()
    {
        _tile = GetComponent<FarmTile>();
    }

    private void Start()
    {
        PlayerInventoryAbility.OnLocalPlayerReady += OnPlayerReady;
    }

    private void OnDestroy()
    {
        PlayerInventoryAbility.OnLocalPlayerReady -= OnPlayerReady;
    }

    private void OnPlayerReady(PlayerInventoryAbility inventoryAbility)
    {
        _inventoryAbility = inventoryAbility;
        Debug.Log("수확물 인벤토리 연동");
    }

    public void ShowFirstStage(SeedItemDataSO seedConfig)
    {
        _seedConfig = seedConfig;
        _currentStageIndex = 0;
        _elapsedDays = 0;
        _isGrowing = false;
        _hasStarted = false; // 아직 성장 시작 아님

        ApplyStagePrefab(); // 첫 단계 프리팹만 표시
    }

    // 물을 줬을 때 외부에서 호출
    public void StartGrowth(SeedItemDataSO seedConfig)
    {
        _seedConfig = seedConfig;
        _isGrowing = true;
        _hasStarted = true;
        _currentStageIndex = 0;
        _elapsedDays = 0;
    }

    public void CheckMorningGrowth()
    {
        if (!_isGrowing)
        {
            return;
        }

        SeedGrowthStageData currentStage = _seedConfig.SeedGrowthStage[_currentStageIndex];

        if (currentStage.GrowthTiming == EGrowthTiming.Morning || currentStage.GrowthTiming == EGrowthTiming.Both)
        {
            TryGrow();
        }
    }

    public void CheckNightGrowth()
    {
        if (!_isGrowing)
        {
            return;
        }

        SeedGrowthStageData currentStage = _seedConfig.SeedGrowthStage[_currentStageIndex];

        if (currentStage.GrowthTiming == EGrowthTiming.Night || currentStage.GrowthTiming == EGrowthTiming.Both)
        {
            TryGrow();
        }
    }

    private void TryGrow()
    {
        _elapsedDays++;

        SeedGrowthStageData currentStage = _seedConfig.SeedGrowthStage[_currentStageIndex];

        if(_elapsedDays >= currentStage.RequireDays)
        {
            _elapsedDays = 0;

            if(_currentStageIndex + 1 >= _seedConfig.SeedGrowthStage.Count)
            {
                _isGrowing = false;
                Debug.Log("수확가능");
                return;
            }

            _currentStageIndex++;
            ApplyStagePrefab();
        }
    }

    public void Harvest()
    {
        if (_isGrowing)
        {
            return;
        }

        if(!_hasStarted)
        {
            Debug.Log("아직 성장 시작 안함");
            return;
        }

        if (_currentCropObject != null)
        {
            Destroy(_currentCropObject);
            _currentCropObject = null;
        }

        _hasStarted = false;
        _currentStageIndex = 0;
        _elapsedDays = 0;

        _tile.RemoveSeed();
    }

    public void ExportTo(FarmSaveData farmData)
    {
        farmData.CropStageIndex = _currentStageIndex;
        farmData.CropElapsedDays = _elapsedDays;
        farmData.CropIsGrowing = _isGrowing;
        farmData.CropHasStarted = _hasStarted;
    }

    public void ImportFrom(FarmSaveData farmData, SeedItemDataSO seed)
    {
        if (!farmData.CropHasStarted)
        {
            return;
        }

        _seedConfig = seed;
        _currentStageIndex = Mathf.Min(farmData.CropStageIndex, _seedConfig.SeedGrowthStage.Count - 1);
        _elapsedDays = farmData.CropElapsedDays;
        _isGrowing = farmData.CropIsGrowing;
        _hasStarted = farmData.CropHasStarted;

        if (_currentStageIndex >= 0 && _currentStageIndex < _seedConfig.SeedGrowthStage.Count)
        {
            ApplyStagePrefab();
        }
        else
        {
            Debug.LogWarning($"[Load] StageIndex 범위 초과: {_currentStageIndex}");
        }
    }

    private void ApplyStagePrefab()
    {
        if(_currentCropObject != null)
        {
            Destroy(_currentCropObject);
        }

        GameObject prefab = _seedConfig.SeedGrowthStage[_currentStageIndex].SeedStageItem;
        _currentCropObject = Instantiate(prefab, _tile.CropSpawnPoint.position, Quaternion.identity, _tile.transform);
    }
}
