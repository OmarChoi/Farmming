using UnityEngine;

public interface IWaterEffect
{
    bool CanHandle(TerrainCell cell);

    void Apply(TerrainCell cell); 
}
