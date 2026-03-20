using System;

[Serializable]
public class FarmSaveData
{
    public EFarmTileStateType FarmState;
    public string SeedId;
    public int CropStageIndex;
    public int CropElapsedDays;
    public bool CropIsGrowing;
    public bool CropHasStarted;
}