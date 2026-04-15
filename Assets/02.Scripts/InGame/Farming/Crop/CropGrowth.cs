using UnityEngine;
using UnityEngine.Tilemaps;

public class CropGrowth : MonoBehaviour
{
    [Header("Harvest Ready Effect")]
    [SerializeField] private GameObject _harvestReadyEffectPrefab;
    [SerializeField] private float _harvestReadyEffectYOffset = 0f;

    private SeedItemDataSO _seedConfig;
    private int _currentStageIndex = 0;
    private int _elapsedDays = 0;
    private bool _isGrowing = false;
    private bool _hasStarted = false;
    private GameObject _currentCropObject;
    private GameObject _harvestReadyEffectInstance;

    private FarmTile _tile;

    private PlayerInventoryAbility _inventoryAbility;

    public bool HasStarted => _hasStarted;
    public bool IsHarvestable => !_isGrowing && _hasStarted;
    public bool HasCropObject => _currentCropObject != null;

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
        ClearHarvestReadyEffect();
    }

    private void OnPlayerReady(PlayerInventoryAbility inventoryAbility)
    {
        _inventoryAbility = inventoryAbility;
        Debug.Log("Harvest inventory connected");
    }

    public void ShowFirstStage(SeedItemDataSO seedConfig)
    {
        _seedConfig = seedConfig;
        _currentStageIndex = 0;
        _elapsedDays = 0;
        _isGrowing = false;
        _hasStarted = false;

        ApplyStagePrefab();
        RefreshHarvestReadyEffect();
    }

    public void StartGrowth(SeedItemDataSO seedConfig)
    {
        _seedConfig = seedConfig;
        _isGrowing = true;
        _hasStarted = true;
        _currentStageIndex = 0;
        _elapsedDays = 0;
        RefreshHarvestReadyEffect();
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

    public void CheckFastFertilizerNightGrowth()
    {
        if (!_isGrowing || !_hasStarted || _seedConfig == null)
        {
            return;
        }

        TryGrow();
    }

    private void TryGrow()
    {
        _elapsedDays++;

        SeedGrowthStageData currentStage = _seedConfig.SeedGrowthStage[_currentStageIndex];

        if (_elapsedDays >= currentStage.RequireDays)
        {
            _elapsedDays = 0;

            if (_currentStageIndex + 1 >= _seedConfig.SeedGrowthStage.Count)
            {
                _isGrowing = false;
                Debug.Log("Crop is harvestable");
                RefreshHarvestReadyEffect();
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

        if (!_hasStarted)
        {
            Debug.Log("Growth has not started yet");
            return;
        }

        ClearHarvestReadyEffect();

        if (_currentCropObject != null)
        {
            Destroy(_currentCropObject);
            _currentCropObject = null;
        }

        _hasStarted = false;
        _currentStageIndex = 0;
        _elapsedDays = 0;

        _tile.RemoveSeed();
        ClearFastFertilizerAfterHarvest();
    }

    private void ClearFastFertilizerAfterHarvest()
    {
        if (_tile == null || !_tile.HasFastFertilizer)
            return;

        if (_tile.HasCrop)
            return;

        _tile.ClearFastFertilizer();
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
            ClearHarvestReadyEffect();
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
            RefreshHarvestReadyEffect();
        }
        else
        {
            Debug.LogWarning($"[Load] StageIndex out of range: {_currentStageIndex}");
            ClearHarvestReadyEffect();
        }
    }

    private void ApplyStagePrefab()
    {
        if (_currentCropObject != null)
        {
            Destroy(_currentCropObject);
        }

        GameObject prefab = _seedConfig.SeedGrowthStage[_currentStageIndex].SeedStageItem;
        _currentCropObject = Instantiate(prefab, _tile.CropSpawnPoint.position, Quaternion.identity, _tile.transform);
        RefreshHarvestReadyEffect();
    }

    private void RefreshHarvestReadyEffect()
    {
        if (!IsHarvestable || _currentCropObject == null || _harvestReadyEffectPrefab == null)
        {
            ClearHarvestReadyEffect();
            return;
        }

        Vector3 effectPosition = GetHarvestReadyEffectPosition();

        if (_harvestReadyEffectInstance == null)
        {
            _harvestReadyEffectInstance = Instantiate(_harvestReadyEffectPrefab, effectPosition, Quaternion.identity, _tile.transform);
            return;
        }

        _harvestReadyEffectInstance.transform.SetPositionAndRotation(effectPosition, Quaternion.identity);
        if (!_harvestReadyEffectInstance.activeSelf)
        {
            _harvestReadyEffectInstance.SetActive(true);
        }
    }

    private Vector3 GetHarvestReadyEffectPosition()
    {
        Vector3 basePosition = _tile != null && _tile.CropSpawnPoint != null
            ? _tile.CropSpawnPoint.position
            : transform.position;

        basePosition.y += _harvestReadyEffectYOffset;
        return basePosition;
    }

    private void ClearHarvestReadyEffect()
    {
        if (_harvestReadyEffectInstance == null)
        {
            return;
        }

        Destroy(_harvestReadyEffectInstance);
        _harvestReadyEffectInstance = null;
    }
}
