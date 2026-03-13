using System.Collections.Generic;
using UnityEngine;

public class FarmTile : MonoBehaviour
{
    [SerializeField] private GameObject _groundObject;
    [SerializeField] private GameObject _farmDryObject;
    [SerializeField] private GameObject _farmWetObject;

    private Dictionary<EFarmTileStateType, GameObject> _stateObjects;

    private void Awake()
    {
        _stateObjects = new Dictionary<EFarmTileStateType, GameObject>
        {
            { EFarmTileStateType.Ground, _groundObject },
            { EFarmTileStateType.FarmDry, _farmDryObject },
            { EFarmTileStateType.FarmWet, _farmWetObject }
        };

        HideAllObject();
        ShowObject(EFarmTileStateType.Ground);
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
