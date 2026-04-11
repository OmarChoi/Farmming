using System;
using System.Collections.Generic;
using UnityEngine;

public class FarmTileStateMachine : MonoBehaviour
{
    private IFarmTileState _currentState;
    private FarmTile _tile;

    private Dictionary<EFarmTileStateType, IFarmTileState> _states;

    public EFarmTileStateType CurrentStateType => _currentState.StateType;

    public static event Action<FarmTile> OnTileBecameDry;
    public static event Action<FarmTile> OnTileBecameWet;

    private void Awake()
    {
        _tile = GetComponent<FarmTile>();

        _states = new Dictionary<EFarmTileStateType, IFarmTileState>()
        {
            { EFarmTileStateType.FarmDry, new FarmDryState()},
            { EFarmTileStateType.FarmWet, new FarmWetState()}
        };

        _currentState = _states[EFarmTileStateType.FarmDry];
        _currentState.EnterState(_tile);
    }

    // invokeEvent는 플레이어 행동에 의한 상태 변화일 때만 true입니다. (세이브/로드 등은 false)
    public void FarmTransition(EFarmTileStateType nextStateType, bool invokeEvent = true)
    {
        if(!CanFarmTransition(nextStateType))
        {
            Debug.Log("전환불가");
            return;
        }

        _currentState.ExitState(_tile);
        _currentState = _states[nextStateType];

        if (invokeEvent)
        {
            if (nextStateType == EFarmTileStateType.FarmDry)
            {
                OnTileBecameDry?.Invoke(_tile);
            }
            else if (nextStateType == EFarmTileStateType.FarmWet)
            {
                OnTileBecameWet?.Invoke(_tile);
            }
        }

        _currentState.EnterState(_tile);
    }

    private bool CanFarmTransition(EFarmTileStateType next)
    {
        return(_currentState.StateType, next) switch
        {
            (EFarmTileStateType.FarmDry, EFarmTileStateType.FarmWet) => true,
            (EFarmTileStateType.FarmWet, EFarmTileStateType.FarmDry) => true,
            _ => false
        };
    }
}
