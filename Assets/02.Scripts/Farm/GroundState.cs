using UnityEngine;

public class GroundState : IFarmTileState
{
    public EFarmTileStateType StateType => EFarmTileStateType.Ground;

    public void EnterState(FarmTile tile)
    {
        Debug.Log("Enter Ground");
        tile.ShowObject(EFarmTileStateType.Ground);
    }

    public void ExitState(FarmTile tile)
    {
        Debug.Log("Exit Ground");
        tile.HideObject(EFarmTileStateType.Ground);
    }
}
