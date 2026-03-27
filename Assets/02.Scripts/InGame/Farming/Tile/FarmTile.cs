using System.Collections.Generic;
using UnityEngine;

public class FarmTile : MonoBehaviour
{
    [SerializeField] private GameObject _farmDryObject;
    [SerializeField] private GameObject _farmWetObject;
    [SerializeField] private Transform _cropSpawnPoint;

    private Dictionary<EFarmTileStateType, GameObject> _stateObjects;
    private CropGrowth _cropGrowth;

    public FarmTileStateMachine StateMachine { get; private set; }
    public CropGrowth CropGrowth => _cropGrowth;

    public SeedConfig PlantedSeed { get; private set; }
    public bool HasSeed => PlantedSeed != null;
    public bool IsWet => StateMachine.CurrentStateType == EFarmTileStateType.FarmWet;
    public bool IsReadyToSow => StateMachine.CurrentStateType == EFarmTileStateType.FarmDry && !HasSeed;
    public Transform CropSpawnPoint => _cropSpawnPoint;

    public void Awake()
    {
        StateMachine = GetComponent<FarmTileStateMachine>();
        _cropGrowth = GetComponent<CropGrowth>();

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
    }

    private void Start()
    {
        if(TimeSystem.Instance != null)
        {
            TimeSystem.Instance.OnDayStarted += OnMorning;
            TimeSystem.Instance.OnDayEnded += OnNight;
        }
    }

    private void OnDisable()
    {
        if(TimeSystem.Instance != null)
        {
            TimeSystem.Instance.OnDayStarted -= OnMorning;
            TimeSystem.Instance.OnDayEnded -= OnNight;
        }
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

        StateMachine.FarmTransition(EFarmTileStateType.FarmDry);

    }

    private void OnNight()
    {
        if(!IsWet)
        {
            return;
        }
        if (HasSeed)
        {
            _cropGrowth.CheckNightGrowth();
        }
    }

    public void Interact(SeedConfig seed = null)
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

    public void PlantSeed(SeedConfig seed)
    {
        PlantedSeed = seed;
        Debug.Log($"{seed.SeedName} 심음");

        _cropGrowth.ShowFirstStage(seed);
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

    public void RemoveSeed()
    {
        PlantedSeed = null;
        StateMachine.FarmTransition(EFarmTileStateType.FarmDry);
        Debug.Log("수확완료");
    }

    public void ExportTo(TerrainCellSaveData saveData)
    {
        saveData.Farm = new FarmSaveData
        {
            FarmState = StateMachine.CurrentStateType,
            SeedId = HasSeed ? PlantedSeed.SeedId : ""
        };

        if (_cropGrowth != null)
            _cropGrowth.ExportTo(saveData.Farm);
    }

    public void ImportFrom(TerrainCellSaveData saveData, SeedDatabase seedDb)
    {
        Debug.Log($"[Load] FarmTile.ImportFrom - Farm null?: {saveData.Farm == null}, SeedId: {saveData.Farm?.SeedId}, FarmState: {saveData.Farm?.FarmState}");

        if (saveData.Farm == null)
        {
            Debug.LogWarning("[Load] Farm 데이터가 null입니다.");
            return;
        }

        if (saveData.Farm.FarmState == EFarmTileStateType.FarmWet)
            StateMachine.FarmTransition(EFarmTileStateType.FarmWet);

        if (!string.IsNullOrEmpty(saveData.Farm.SeedId))
        {
            SeedConfig seed = seedDb.GetById(saveData.Farm.SeedId);
            Debug.Log($"[Load] SeedDB 조회 결과 - SeedId: {saveData.Farm.SeedId}, seed null?: {seed == null}");

            if (seed != null)
            {
                PlantSeed(seed);

                Debug.Log($"[Load] _cropGrowth null?: {_cropGrowth == null}, CropHasStarted: {saveData.Farm.CropHasStarted}, StageIndex: {saveData.Farm.CropStageIndex}");
                if (_cropGrowth != null)
                    _cropGrowth.ImportFrom(saveData.Farm, seed);
            }
            else
            {
                Debug.LogWarning($"[Load] SeedDB에서 '{saveData.Farm.SeedId}'를 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.Log("[Load] SeedId가 비어있음 - 농작물 없는 밭");
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
