using System;
using System.Collections.Generic;

public interface IQuestProgressService
{
    List<QuestRuntimeData> GetActiveQuestList();

    bool CanAcceptQuest(QuestDataSO questData);
    bool AcceptQuest(QuestDataSO questData);

    public bool HasQuest(string questId);

    public bool CanCompleteQuest(string questId);

    bool CompleteQuest(string questId);
    bool TryDeliverItemToNpc(string questId, string npcId);

    bool IsQuestCompleted(string questId);

    event Action<QuestRuntimeData> OnQuestAccepted;
    event Action<QuestRuntimeData> OnQuestUpdated;
    event Action<QuestRuntimeData> OnQuestCompleted;
    event Action<string> OnQuestRemoved;

}
