using UnityEngine;

public class FarmDryState : MonoBehaviour
{
    public EFarmTileStateType StateType => EFarmTileStateType.FarmDry;

    public void EnterState(FarmTile tile)
    {
        Debug.Log("Enter FarmDry");
        tile.ShowObject(EFarmTileStateType.FarmDry);
    }

    public void UpdateState(FarmTile tile)
    {
        Debug.Log("Update FarmDry");
        tile.HideObject(EFarmTileStateType.FarmDry);
    }
}
