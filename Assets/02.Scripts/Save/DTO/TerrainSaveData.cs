using System;
using System.Collections.Generic;

[Serializable]
public class TerrainSaveData
{
    public List<TerrainCellSaveData> Cells = new();
}