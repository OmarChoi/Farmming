using System.Collections.Generic;
using UnityEngine;

public class FarmTileStateMachine : MonoBehaviour
{
    private IFarmTileState _currentState;
    private FarmTile _tile;

    private Dictionary<EFarmTileStateType, IFarmTileState> _states;

    public EFarmTileStateType CurrentStateType => _currentState.StateType;

    private void Awake()
    {
        _tile = GetComponent<FarmTile>();

        _states = new Dictionary<EFarmTileStateType, IFarmTileState>()
        {
            { EFarmTileStateType.Ground, new GroundState()},
            { EFarmTileStateType.FarmDry, new FarmDryState()},
            { EFarmTileStateType.FarmWet, new FarmWetState()}
        };
    }

    private void Start()
    {
        _currentState = _states[EFarmTileStateType.Ground];
        _currentState.EnterState(_tile);
    }

    public void FarmTransition(EFarmTileStateType nextStateType)
    {
        if(!CanFarmTransition(nextStateType))
        {
            Debug.Log("전환불가");
            return;
        }

        _currentState.ExitState(_tile);
        _currentState = _states[nextStateType];
        _currentState.EnterState(_tile);
    }

    private bool CanFarmTransition(EFarmTileStateType next)
    {
        return(_currentState.StateType, next) switch
        {
            (EFarmTileStateType.Ground, EFarmTileStateType.FarmDry) => true,
            (EFarmTileStateType.FarmDry, EFarmTileStateType.FarmWet) => true,
            (EFarmTileStateType.FarmWet, EFarmTileStateType.FarmDry) => true,
            (EFarmTileStateType.FarmWet, EFarmTileStateType.Ground) => true,
            _ => false
        };
    }
}
