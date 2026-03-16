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

    // 농사 정보
    public EFarmTileStateType FarmState;
    public string SeedId;
    public int CropStageIndex;
    public int CropElapsedDays;
    public bool CropIsGrowing;
    public bool CropHasStarted;
}