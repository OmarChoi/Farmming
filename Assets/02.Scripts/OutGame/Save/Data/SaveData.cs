using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public TerrainSaveData Terrain = new();
    public List<PlayerSaveData> Players = new();
    public List<BuildingSaveData> Buildings = new();
    public TimeSaveData Time = new();
    public VillageSaveData Village = new();
    public WorldEffectSaveData WorldEffects = new();
    public ForcedTimedQuestSaveData ForcedTimedQuest = new();
}
