using UnityEngine;

public class FarmWetState : MonoBehaviour
{
    public EFarmTileStateType StateType => EFarmTileStateType.FarmWet;

    public void EnterState(FarmTile tile)
    {
        Debug.Log("Enter FarmWet");
        tile.ShowObject(EFarmTileStateType.FarmWet);
    }

    public void UpdateState(FarmTile tile)
    {
        Debug.Log("Update FarmWet");
        tile.HideObject(EFarmTileStateType.FarmWet);
    }
}
