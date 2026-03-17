using System;

[Serializable]
public class TerrainCellSaveData
{
    public int X;
    public int Y;
    public int Z;
    public ECellType CellType;
    public int DirtLevel;
    public EGridObjectType ObjectType;
    public int ObjectLevel;

    public FarmSaveData Farm;
}