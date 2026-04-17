using System;
using System.Collections.Generic;

[Serializable]
public class WorldEffectSaveData
{
    public List<WorldEffectEntry> Effects = new();
}

[Serializable]
public class WorldEffectEntry
{
    public string EffectId;
    public byte Kind;
    public int TotalDurationDays;
    public int RemainingDays;
}
