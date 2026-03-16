using System;

[Serializable]
public class TerrainCellData
{
    public CellType CellType;
    public int DirtLevel;
    public GridObjectType ObjectType;
    public int ObjectLevel;

    public TerrainCellData(CellType cellType = CellType.Dirt, int dirtLevel = 1, GridObjectType objectType = GridObjectType.None, int objectLevel = 0)
    {
        CellType = cellType;
        DirtLevel = dirtLevel;
        ObjectType = objectType;
        ObjectLevel = objectLevel;
    }

    public bool CanDig(int toolLevel)
    {
        return CellType == CellType.Dirt && toolLevel >= DirtLevel;
    }

    public void Dig()
    {
        CellType = CellType.Empty;
        ObjectType = GridObjectType.None;
    }

    public void PlaceBlock(int dirtLevel = 1)
    {
        CellType = CellType.Dirt;
        DirtLevel = dirtLevel;
    }

    public bool CanBreak(int toolLevel)
    {
        if (ObjectType == GridObjectType.None || ObjectType == GridObjectType.FarmLand) return false;
        return toolLevel >= ObjectLevel;
    }

    public void SetObject(GridObjectType type, int level = 1)
    {
        ObjectType = type;
        ObjectLevel = level;
    }

    public void RemoveObject()
    {
        ObjectType = GridObjectType.None;
        ObjectLevel = 0;
    }
}