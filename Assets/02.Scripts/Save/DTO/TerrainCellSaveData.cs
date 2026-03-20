using System;

[Serializable]
public class TerrainCellSaveData
{
    public int X;
    public int Y;
    public int Z;
    public ECellType CellType;
    public ETileType TileType;
    public int DirtLevel;
    public EGridObjectType ObjectType;
    public int ObjectLevel;
    public bool IsIndestructible;

    public FarmSaveData Farm;
}