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

    public SeedConfig PlantedSeed { get; private set; }
    public bool HasSeed => PlantedSeed != null;
    public bool IsWet => StateMachine.CurrentStateType == EFarmTileStateType.FarmWet;
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
        if(DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.OnMorningStart += OnMorning;
            DayNightCycle.Instance.OnNightStart += OnNight;
        }
    }

    private void OnDisable()
    {
        if(DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.OnMorningStart -= OnMorning;
            DayNightCycle.Instance.OnNightStart -= OnNight;
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
            StateMachine.FarmTransition(EFarmTileStateType.FarmWet);

            if (!_cropGrowth.HasStarted)
            {
                _cropGrowth.StartGrowth(PlantedSeed);
            }
            else
            {
                Debug.Log("물 줌(성장 계속)");
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
    }

    public void RemoveSeed()
    {
        PlantedSeed = null;
        StateMachine.FarmTransition(EFarmTileStateType.FarmDry);
        Debug.Log("수확완료");
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
