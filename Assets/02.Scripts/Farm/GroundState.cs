using UnityEngine;

public class GroundState : MonoBehaviour
{
    public EFarmTileStateType StateType => EFarmTileStateType.Ground;

    public void EnterState(FarmTile tile)
    {
        Debug.Log("Enter Ground");
        tile.ShowObject(EFarmTileStateType.Ground);
    }

    public void UpdateState(FarmTile tile)
    {
        Debug.Log("Update Ground");
        tile.HideObject(EFarmTileStateType.Ground);
    }
}
