using System;
using System.Collections.Generic;

[Serializable]
public class MapSyncData
{
    public TerrainSaveData Terrain;
    public List<BuildingSaveData> Buildings;
    public VillageSaveData Village;
    public WorldEffectSaveData WorldEffects;
    public int MaxHeight;
}
