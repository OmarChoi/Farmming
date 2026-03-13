using UnityEngine;

public interface IFarmTileState
{
    EFarmTileStateType StateType { get;}
    public void EnterState(FarmTile tile);
    public void UpdateState(FarmTile tile);
}
