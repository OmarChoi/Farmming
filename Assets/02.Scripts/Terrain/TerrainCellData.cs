using System;

[Serializable]
public class TerrainCellData
{
    public ECellType CellType { get; private set; }
    public int DirtLevel { get; private set; }
    public EGridObjectType ObjectType { get; private set; }
    public int ObjectLevel { get; private set; }

    public TerrainCellData(ECellType cellType = ECellType.Dirt, int dirtLevel = 1, EGridObjectType objectType = EGridObjectType.None, int objectLevel = 0)
    {
        CellType = cellType;
        DirtLevel = dirtLevel;
        ObjectType = objectType;
        ObjectLevel = objectLevel;
    }

    public bool CanDig(int toolLevel)
    {
        return CellType == ECellType.Dirt && toolLevel >= DirtLevel;
    }

    public void Dig()
    {
        CellType = ECellType.Empty;
        ObjectType = EGridObjectType.None;
    }

    public void PlaceBlock(int dirtLevel = 1)
    {
        CellType = ECellType.Dirt;
        DirtLevel = dirtLevel;
    }

    public bool CanBreak(int toolLevel)
    {
        if (ObjectType == EGridObjectType.None || ObjectType == EGridObjectType.FarmLand) return false;
        return toolLevel >= ObjectLevel;
    }

    public void SetObject(EGridObjectType type, int level = 1)
    {
        ObjectType = type;
        ObjectLevel = level;
    }

    public void RemoveObject()
    {
        ObjectType = EGridObjectType.None;
        ObjectLevel = 0;
    }
}