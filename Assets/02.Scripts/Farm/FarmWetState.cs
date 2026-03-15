using UnityEngine;

public class FarmWetState : IFarmTileState
{
    public EFarmTileStateType StateType => EFarmTileStateType.FarmWet;

    public void EnterState(FarmTile tile)
    {
        Debug.Log("Enter FarmWet");
        tile.ShowObject(EFarmTileStateType.FarmWet);
    }

    public void ExitState(FarmTile tile)
    {
        Debug.Log("Exit FarmWet");
        tile.HideObject(EFarmTileStateType.FarmWet);
    }
}
