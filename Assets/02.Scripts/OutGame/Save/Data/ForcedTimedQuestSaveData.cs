using System;

[Serializable]
public class ForcedTimedQuestSaveData
{
    public string ActiveQuestId;
    public int AcceptedDay;
    public int ExpireDay;
    public int LastTriggerDay;
}