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

    public bool HasStarted => _hasStarted;
    public bool IsHarvestable => !_isGrowing && _hasStarted;

    private void Awake()
    {
        _tile = GetComponent<FarmTile>();
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
            _currentStageIndex++;

            if(_currentStageIndex >= _seedConfig.SeedGrowthStage.Count)
            {
                _isGrowing = false;
                Debug.Log("수확가능");
                return;
            }

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

        Debug.Log($"{_seedConfig.SeedName}수확");

        if(_currentCropObject != null)
        {
            Destroy(_currentCropObject);
            _currentCropObject = null;
        }

        _hasStarted = false;
        _currentStageIndex = 0;
        _elapsedDays = 0;

        _tile.RemoveSeed();
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
