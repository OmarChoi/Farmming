using UnityEngine;

public class FarmDryWaterEffect : IWaterEffect
{
    public bool CanHandle(TerrainCell cell)
    {
        if(cell.FarmTile == null)
        {
            return false;
        }

        if(!cell.FarmTile.gameObject.activeSelf)
        {
            return false;
        }
        return cell.FarmTile.StateMachine.CurrentStateType == EFarmTileStateType.FarmDry;
    }

    public void Apply(TerrainCell cell)
    {
        cell.FarmTile.Interact();
    }
}
