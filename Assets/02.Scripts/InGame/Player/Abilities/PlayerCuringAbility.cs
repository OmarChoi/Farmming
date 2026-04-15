using UnityEngine;

public class PlayerCuringAbility : PlayerAbility, ISaveableAbility
{
    public int LastCuredDay { get; private set; } = -1;

    public bool HasReceivedCuringToday()
    {
        return LastCuredDay == TimeEvents.CurrentDay;
    }

    public bool CanReceiveCuringToday()
    {
        return LastCuredDay != TimeEvents.CurrentDay;
    }

    public void MarkCuredToday()
    {
        LastCuredDay = TimeEvents.CurrentDay;
    }

    public void ExportTo(PlayerSaveData saveData)
    {
        if (saveData == null) return;
        saveData.LastCuredDay = LastCuredDay;
    }

    public void ImportFrom(PlayerSaveData saveData)
    {
        if (saveData == null) return;
        LastCuredDay = saveData.LastCuredDay;
    }
}
