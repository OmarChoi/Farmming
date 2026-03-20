using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public TerrainSaveData Terrain = new();
    public List<PlayerSaveData> Players = new();
}