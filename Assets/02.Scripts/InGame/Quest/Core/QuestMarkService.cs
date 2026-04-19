using UnityEngine;

public class QuestMarkService : MonoBehaviour
{
    [SerializeField] private QuestManager _questManager;
    [SerializeField] private NpcFriendshipManager _friendshipManager;

    private void Awake()
    {
        if (_questManager == null)
        {
            _questManager = QuestManager.Instance;
        }

        if (_friendshipManager == null)
        {
            _friendshipManager = NpcFriendshipManager.Instance;
        }
    }

    public EWorldMarkVisualType GetNpcQuestMarkVisualType(NpcController npc)
    {
        if (npc == null || _questManager == null)
        {
            return EWorldMarkVisualType.None;
        }

        string npcId = npc.Data.NpcId;
        if (string.IsNullOrEmpty(npcId))
        {
            return EWorldMarkVisualType.None;
        }

        // 1. 완료 가능 우선
        foreach (QuestRuntimeData quest in _questManager.GetActiveQuestList())
        {
            if (quest == null || quest.QuestData == null) continue;

            if (IsQuestCompletableAtNpc(quest, npcId))
            {
                return EWorldMarkVisualType.Complete;
            }
        }

        // 2. 수락 가능
        NpcQuest provider = npc.GetComponent<NpcQuest>();
        if (provider != null && provider.Quests != null)
        {
            foreach (QuestDataSO questData in provider.Quests)
            {
                if (questData == null) continue;
                if (questData.StartNpcId != npcId) continue;

                if (CanOfferQuest(npcId, questData))
                {
                    return EWorldMarkVisualType.Available;
                }
            }
        }

        // 3. 진행 중
        foreach (QuestRuntimeData quest in _questManager.GetActiveQuestList())
        {
            if (quest == null || quest.QuestData == null) continue;
            if (quest.Status != EQuestStatus.InProgress) continue;

            if (IsQuestRelatedToNpc(quest.QuestData, npcId))
            {
                return EWorldMarkVisualType.InProgress;
            }
        }

        return EWorldMarkVisualType.None;
    }

    private bool IsQuestCompletableAtNpc(QuestRuntimeData quest, string npcId)
    {
        if (quest == null || quest.QuestData == null) return false;

        if (quest.Status == EQuestStatus.CanComplete && quest.QuestData.CompleteNpcId == npcId)
        {
            return true;
        }

        bool canDeliverHere =
            quest.Status == EQuestStatus.InProgress &&
            quest.QuestData.ObjectiveType == EQuestObjectiveType.DeliverItem &&
            quest.QuestData.TargetNpcId == npcId &&
            quest.AreAllItemRequirementsCompleted();

        return canDeliverHere;
    }

    private bool IsQuestRelatedToNpc(QuestDataSO questData, string npcId)
    {
        if (questData == null || string.IsNullOrEmpty(npcId)) return false;

        return questData.StartNpcId == npcId ||
               questData.CompleteNpcId == npcId ||
               questData.TargetNpcId == npcId;
    }

    private bool CanOfferQuest(string npcId, QuestDataSO questData)
    {
        if (questData == null || _questManager == null) return false;

        if (!_questManager.CanAcceptQuest(questData)) return false;

        if (questData.PrerequisiteQuests != null)
        {
            foreach (QuestDataSO prerequisite in questData.PrerequisiteQuests)
            {
                if (prerequisite == null || string.IsNullOrEmpty(prerequisite.QuestId)) continue;

                if (!_questManager.IsQuestCompleted(prerequisite.QuestId)) return false;
            }
        }

        if (questData.RequiredFriendship > 0)
        {
            if (_friendshipManager == null) return false;

            int friendship = _friendshipManager.GetFriendship(npcId);
            if (friendship < questData.RequiredFriendship) return false;
        }

        return true;
    }
}
