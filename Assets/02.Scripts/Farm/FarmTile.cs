using System.Collections.Generic;
using UnityEngine;

public class FarmTile : MonoBehaviour
{
    [SerializeField] private GameObject _groundObject;
    [SerializeField] private GameObject _farmDryObject;
    [SerializeField] private GameObject _farmWetObject;

    public FarmTileStateMachine StateMachine { get; set; }

    public SeedConfig PlantedSeed { get; private set; }
    public bool HasSeed => PlantedSeed != null;

    private Dictionary<EFarmTileStateType, GameObject> _stateObjects;

    public void Initialize()
    {
        StateMachine = GetComponent<FarmTileStateMachine>();

        _stateObjects = new Dictionary<EFarmTileStateType, GameObject>
        {
            { EFarmTileStateType.Ground, _groundObject },
            { EFarmTileStateType.FarmDry, _farmDryObject },
            { EFarmTileStateType.FarmWet, _farmWetObject }
        };

        HideAllObject();
        ShowObject(EFarmTileStateType.Ground);
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
