using System;
using UnityEngine;

[Serializable]
public class QuestRuntimeData
{
    public QuestDataSO QuestData;
    public EQuestStatus Status;
    public int CurrentAmount;

    public QuestRuntimeData(QuestDataSO questData)
    {
        QuestData = questData;
        Status = EQuestStatus.InProgress;
        CurrentAmount = 0;
    }

    public bool IsObjectiveCompleted()
    {
        return CurrentAmount >= QuestData.RequiredAmount;
    }
}
