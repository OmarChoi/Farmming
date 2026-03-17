using UnityEngine;
using UnityEngine.Tilemaps;

public class CropGrowth : MonoBehaviour
{
    private SeedConfig _seedConfig;
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

    // 물을 줬을 때 외부에서 호출
    public void StartGrowth(SeedConfig seedConfig)
    {
        _seedConfig = seedConfig;
        _isGrowing = true;
        _hasStarted = true;
        _currentStageIndex = 0;
        _elapsedDays = 0;
        ApplyStagePrefab();
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

    // 수확 호출 (외부에서 E키 수확 시 호출)
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

        int harvestAmount = Random.Range(_seedConfig.HarvestAmountMin, _seedConfig.HarvestAmountMax + 1);
        Debug.Log($"{_seedConfig.SeedName}: {harvestAmount}수확");

        if (_seedConfig.SeedIcon != null)
        {
            HarvestNotificationManager.Instance.Show(_seedConfig.SeedIcon, _seedConfig.SeedName, harvestAmount);
        }

        if (_inventoryAbility != null && _seedConfig.HarvestItem != null)
        {
            _inventoryAbility.AddItem(_seedConfig.HarvestItem, harvestAmount);
            Debug.Log("인벤토리에 수확물 추가");
        }
        else
        {
            if(_inventoryAbility == null)
            {
                Debug.Log("인벤토리 없음");
            }
            if(_seedConfig.HarvestItem == null)
            {
                Debug.Log("수확물 설정 없음");
            }
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

    public void ImportFrom(FarmSaveData farmData, SeedConfig seed)
    {
        Debug.Log($"[Load] CropGrowth.ImportFrom - CropHasStarted: {farmData.CropHasStarted}, StageIndex: {farmData.CropStageIndex}, IsGrowing: {farmData.CropIsGrowing}");

        if (!farmData.CropHasStarted)
        {
            Debug.Log("[Load] CropHasStarted가 false - 스킵");
            return;
        }

        _seedConfig = seed;
        _currentStageIndex = Mathf.Min(farmData.CropStageIndex, _seedConfig.SeedGrowthStage.Count - 1);
        _elapsedDays = farmData.CropElapsedDays;
        _isGrowing = farmData.CropIsGrowing;
        _hasStarted = farmData.CropHasStarted;

        Debug.Log($"[Load] 상태 복원 완료 - StageIndex: {_currentStageIndex}, StageCount: {_seedConfig.SeedGrowthStage.Count}, _tile null?: {_tile == null}");

        if (_currentStageIndex >= 0 && _currentStageIndex < _seedConfig.SeedGrowthStage.Count)
        {
            Debug.Log($"[Load] ApplyStagePrefab 호출 - CropSpawnPoint null?: {_tile?.CropSpawnPoint == null}");
            ApplyStagePrefab();
            Debug.Log($"[Load] 프리팹 생성 완료 - _currentCropObject null?: {_currentCropObject == null}");
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
