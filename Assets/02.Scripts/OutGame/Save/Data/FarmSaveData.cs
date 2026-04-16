using System;

[Serializable]
public class FarmSaveData
{
    public EFarmTileStateType FarmState;
    public int SeedId;
    public bool HasFastFertilizer;
    public int CropStageIndex;
    public int CropElapsedDays;
    public bool CropIsGrowing;
    public bool CropHasStarted;
}
