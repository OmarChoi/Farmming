using System;

[Serializable]
public class TerrainCellDelta
{
    public int X;
    public int Y;
    public int Z;
    public bool RemoveCell;
    public TerrainCellSaveData Cell;
}
