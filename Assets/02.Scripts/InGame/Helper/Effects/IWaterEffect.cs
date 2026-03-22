using UnityEngine;

public interface IWaterEffect
{
    bool CanHandle(TerrainCell cell);

    public void Apply(TerrainCell cell); 
}
