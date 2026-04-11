using System;
using System.Collections.Generic;

[Serializable]
public class QuestSaveData
{
    public List<QuestRuntimeSaveData> ActiveQuests = new();
    public List<string> CompletedMainQuestIds = new();
    public List<string> CompletedSubQuestIds = new();
}

[Serializable]
public class QuestRuntimeSaveData
{
    public string QuestId;
    public int Status;
    public int CurrentAmount;
    public List<QuestItemProgressSaveData> ItemProgressList = new();
}

[Serializable]
public class QuestItemProgressSaveData
{
    public int ItemId;
    public int CurrentAmount;
}
