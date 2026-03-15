using UnityEngine;

public class FarmDryState : IFarmTileState
{
    public EFarmTileStateType StateType => EFarmTileStateType.FarmDry;

    public void EnterState(FarmTile tile)
    {
        Debug.Log("Enter FarmDry");
        tile.ShowObject(EFarmTileStateType.FarmDry);
    }

    public void ExitState(FarmTile tile)
    {
        Debug.Log("Exit FarmDry");
        tile.HideObject(EFarmTileStateType.FarmDry);
    }
}
