using System;
using System.Collections.Generic;
using UnityEngine;

public class FarmTile : MonoBehaviour
{
    private const string FastFertilizerChildName = "FastFertilizer";

    [SerializeField] private GameObject _farmDryObject;
    [SerializeField] private GameObject _farmWetObject;
    [SerializeField] private Transform _cropSpawnPoint;
    [SerializeField] private GameObject _fastFertilizerObject;

    private Dictionary<EFarmTileStateType, GameObject> _stateObjects;
    private CropGrowth _cropGrowth;

    public FarmTileStateMachine StateMachine { get; private set; }
    public CropGrowth CropGrowth => _cropGrowth;

    public SeedItemDataSO PlantedSeed { get; private set; }
    public bool HasSeed => PlantedSeed != null;
    public bool HasCrop => _cropGrowth != null && _cropGrowth.HasCropObject;
    public bool IsWet => StateMachine.CurrentStateType == EFarmTileStateType.FarmWet;
    public bool IsReadyToSow => StateMachine.CurrentStateType == EFarmTileStateType.FarmDry && !HasSeed;
    public bool HasFastFertilizer => _fastFertilizerObject != null && _fastFertilizerObject.activeSelf;
    public Transform CropSpawnPoint => _cropSpawnPoint;

    public static event Action<FarmTile, SeedItemDataSO> OnSeedPlanted;

    public void Awake()
    {
        StateMachine = GetComponent<FarmTileStateMachine>();
        _cropGrowth = GetComponent<CropGrowth>();
        if (_fastFertilizerObject == null)
        {
            Transform fastFertilizer = transform.Find(FastFertilizerChildName);
            if (fastFertilizer != null)
                _fastFertilizerObject = fastFertilizer.gameObject;
        }

        Init();
    }

    public void Init()
    {

        _stateObjects = new Dictionary<EFarmTileStateType, GameObject>
        {
            { EFarmTileStateType.FarmDry, _farmDryObject },
            { EFarmTileStateType.FarmWet, _farmWetObject }
        };

        HideAllObject();
        ShowObject(EFarmTileStateType.FarmDry);
        ClearFastFertilizer();
    }

    private void Start()
    {
        TimeEvents.OnNetDayStarted += OnMorning;
        TimeEvents.OnNetDayEnded += OnNight;
    }

    private void OnDisable()
    {
        TimeEvents.OnNetDayStarted -= OnMorning;
        TimeEvents.OnNetDayEnded -= OnNight;
    }

    private void OnMorning()
    {
        if (!IsWet)
        {
            return;
        }

        if(HasSeed)
        {
            _cropGrowth.CheckMorningGrowth();
        }

        StateMachine.FarmTransition(EFarmTileStateType.FarmDry, false);

    }

    private void OnNight()
    {
        if(!IsWet)
        {
            return;
        }
        if (HasSeed)
        {
            if (HasFastFertilizer)
            {
                _cropGrowth.CheckFastFertilizerNightGrowth();
            }
            else
            {
                _cropGrowth.CheckNightGrowth();
            }
        }
    }

    public void Interact(SeedItemDataSO seed = null)
    {
        EFarmTileStateType current = StateMachine.CurrentStateType;

        if (current == EFarmTileStateType.FarmDry && !HasSeed)
        {
            if (seed != null)
            {
                PlantSeed(seed);
            }
            else
            {
                Debug.Log("씨앗 없음");
            }
        }
        else if (current == EFarmTileStateType.FarmDry && HasSeed)
        {
            // 수확 가능 상태면 먼저 수확 체크
            if (_cropGrowth.IsHarvestable)
            {
                _cropGrowth.Harvest();
            }
            else
            {
                // 수확 불가면 물 주기
                StateMachine.FarmTransition(EFarmTileStateType.FarmWet);
                if (!_cropGrowth.HasStarted)
                {
                    _cropGrowth.StartGrowth(PlantedSeed);
                }
                else
                {
                    Debug.Log("물 줌 (성장 계속)");
                 }
            }
        }
        else if (current == EFarmTileStateType.FarmWet)
        {
            if (_cropGrowth.IsHarvestable)
            {
                _cropGrowth.Harvest();
            }
            else
            {
                Debug.Log("아직 수확할 수 없음");
            }

        }
    }

    // invokeEvent는 플레이어 행동에 의한 상태 변화일 때만 true입니다. (세이브/로드 등은 false)
    public void PlantSeed(SeedItemDataSO seed, bool invokeEvent = true)
    {
        PlantedSeed = seed;

        _cropGrowth.ShowFirstStage(seed);

        if (invokeEvent)
        {
            OnSeedPlanted?.Invoke(this, seed);
        }
    }

    public void Water()
    {
        if (StateMachine.CurrentStateType != EFarmTileStateType.FarmDry)
        {
            return;
        }

        StateMachine.FarmTransition(EFarmTileStateType.FarmWet);
        if (!_cropGrowth.HasStarted)
        {
            _cropGrowth.StartGrowth(PlantedSeed);
        }
    }

    public bool CanApplyFastFertilizer()
    {
        if (HasSeed || HasCrop || HasFastFertilizer || StateMachine == null)
            return false;

        EFarmTileStateType current = StateMachine.CurrentStateType;
        return current == EFarmTileStateType.FarmDry || current == EFarmTileStateType.FarmWet;
    }

    public bool ApplyFastFertilizer()
    {
        if (!CanApplyFastFertilizer())
            return false;

        if (_fastFertilizerObject != null)
            _fastFertilizerObject.SetActive(true);

        return true;
    }

    public void ClearFastFertilizer()
    {
        if (_fastFertilizerObject != null && _fastFertilizerObject.activeSelf)
            _fastFertilizerObject.SetActive(false);
    }

    public void RemoveSeed()
    {
        PlantedSeed = null;
        StateMachine.FarmTransition(EFarmTileStateType.FarmDry, false);
        Debug.Log("수확완료");
    }

    public void ExportTo(TerrainCellSaveData saveData)
    {
        saveData.Farm = new FarmSaveData
        {
            FarmState = StateMachine.CurrentStateType,
            SeedId = HasSeed ? PlantedSeed.Id : 0,
            HasFastFertilizer = HasFastFertilizer
        };

        if (_cropGrowth != null)
            _cropGrowth.ExportTo(saveData.Farm);
    }

    public void ImportFrom(TerrainCellSaveData saveData, SeedDatabase seedDb)
    {
        if (saveData.Farm == null)
        {
            return;
        }

        if (saveData.Farm.FarmState == EFarmTileStateType.FarmWet)
        { 
            StateMachine.FarmTransition(EFarmTileStateType.FarmWet, false);
        }

        if (saveData.Farm.HasFastFertilizer)
            ApplyFastFertilizer();
        else
            ClearFastFertilizer();

        if (saveData.Farm.SeedId > 0)
        {
            SeedItemDataSO seed = seedDb.GetById(saveData.Farm.SeedId);
            if (seed != null)
            {
                PlantSeed(seed, false);
                _cropGrowth?.ImportFrom(saveData.Farm, seed);
            }
        }
    }

    public void ShowObject(EFarmTileStateType stateType)
    {
        if (_stateObjects.TryGetValue(stateType, out GameObject obj))
        {
            obj.SetActive(true);
        }
    }

    public void HideObject(EFarmTileStateType stateType)
    {
        if (_stateObjects.TryGetValue(stateType, out GameObject obj))
        {
            obj.SetActive(false);
        }
    }

    private void HideAllObject()
    {
        foreach (var obj in _stateObjects.Values)
        {
            obj.SetActive(false);
        }
    }
}
