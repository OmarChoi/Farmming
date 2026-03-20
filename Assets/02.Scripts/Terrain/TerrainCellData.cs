using System;

[Serializable]
public class TerrainCellData
{
    public ECellType CellType { get; private set; }
    public ETileType TileType { get; private set; }
    public int DirtLevel { get; private set; }
    public EGridObjectType ObjectType { get; private set; }
    public int ObjectLevel { get; private set; }
    public bool IsIndestructible { get; private set; }

    public TerrainCellData(
        ECellType cellType = ECellType.Dirt,
        ETileType tileType = ETileType.VillageDirt,
        int dirtLevel = 1,
        EGridObjectType objectType = EGridObjectType.None,
        int objectLevel = 0,
        bool isIndestructible = false)
    {
        CellType = cellType;
        TileType = tileType;
        DirtLevel = dirtLevel;
        ObjectType = objectType;
        ObjectLevel = objectLevel;
        IsIndestructible = isIndestructible;
    }

    public bool CanDig(int toolLevel)
    {
        if (IsIndestructible) return false;
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