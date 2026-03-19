using UnityEngine;

public abstract class FarmBaseAbility : HelperAbility, IHelperAction
{
    protected FarmTile GetFarmTile(TerrainCell cell)
    {
        if(cell == null)
        {
            return null;
        }

        if(cell.FarmTile != null && cell.FarmTile.gameObject.activeSelf)
        {
            return cell.FarmTile;
        }

        return null;
    }

    public abstract void InteractPrimary(TerrainCell cell);
    public abstract void InteractSecondary(TerrainCell cell);
}
