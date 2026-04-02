using UnityEngine;

public class NpcQuestService : MonoBehaviour
{
    [SerializeField] private NpcDialogueController _dialogueController;

    private void Awake()
    {
        if (_dialogueController == null)
        {
            _dialogueController = FindFirstObjectByType<NpcDialogueController>();
        }
    }

    public void ExecuteQuestInteraction(NpcInteractionContext context)
    {
        if (context == null || context.Npc == null || QuestManager.Instance == null) return;

        string npcId = context.NpcId;

        // 1. 완료 가능한 퀘스트를 우선 확인합니다.
        QuestRuntimeData completableQuest = QuestManager.Instance.GetCompletableQuestByNpc(npcId);
        if (completableQuest != null)
        {
            HandleCompleteQuest(context, completableQuest);
            return;
        }

        // 2. 수락 가능한 퀘스트를 확인합니다.
        QuestDataSO acceptableQuest = FindAcceptableQuest(context);
        if (acceptableQuest != null)
        {
            HandleAcceptQuest(context, acceptableQuest);
            return;
        }

        // 3. 진행 중인 퀘스트를 확인합니다.
        QuestRuntimeData inProgressQuest = QuestManager.Instance.GetInProgressQuestByNpc(npcId);
        if (inProgressQuest != null)
        {
            HandleInProgressQuest(context, inProgressQuest);
            return;
        }

        // 4. 관련 퀘스트가 없을 경우 실행합니다.
        HandleNoQuest(context);
    }

    private QuestDataSO FindAcceptableQuest(NpcInteractionContext context)
    {
        if (context == null || context.Npc == null) return null;

        NpcQuest provider = context.Npc.GetComponent<NpcQuest>();
        if (provider == null || provider.Quests == null) return null;

        string npcId = context.NpcId;

        foreach (QuestDataSO questData in provider.Quests)
        {
            if (questData == null || questData.StartNpcId != npcId) continue;
            if (!CanOfferQuest(context, questData)) continue;

            return questData;
        }

        return null;
    }

    private bool CanOfferQuest(NpcInteractionContext context, QuestDataSO questData)
    {
        if (context == null || context.Npc == null || questData == null) return false;
        if (QuestManager.Instance == null) return false;

        // 1. 수락 가능한 퀘스트인지 확인합니다.
        if (!QuestManager.Instance.CanAcceptQuest(questData))
        {
            return false;
        }

        // 2. 선행 퀘스트 조건을 확인합니다.
        if (!ArePrerequisiteQuestsSatisfied(questData))
        {
            return false;
        }

        // 3. 호감도 조건을 확인합니다.
        if (!IsFriendshipSatisfied(context.NpcId, questData.RequiredFriendship))
        {
            return false;
        }

        return true;
    }

    private bool ArePrerequisiteQuestsSatisfied(QuestDataSO questData)
    {
        if (questData == null || QuestManager.Instance == null) return false;
        if (questData.PrerequisiteQuests == null || questData.PrerequisiteQuests.Count == 0) return true;

        foreach (QuestDataSO prerequisite in questData.PrerequisiteQuests)
        {
            if (prerequisite == null || string.IsNullOrEmpty(prerequisite.QuestId)) continue;

            if (!QuestManager.Instance.IsQuestCompleted(prerequisite.QuestId))
            {
                return false;
            }
        }
        return true;
    }

    private bool IsFriendshipSatisfied(string npcId, int requiredFriendship)
    {
        if (requiredFriendship <= 0) return true;
        if (string.IsNullOrEmpty(npcId) || NpcFriendshipManager.Instance == null) return false;

        int currentFriendship = NpcFriendshipManager.Instance.GetFriendship(npcId);
        return currentFriendship >= requiredFriendship;
    }

    private void HandleAcceptQuest(NpcInteractionContext context, QuestDataSO questData)
    {
        bool accepted = QuestManager.Instance.AcceptQuest(questData);
        if (!accepted) return;

        if (_dialogueController != null)
        {
            if (questData.AcceptDialogue != null)
            {
                _dialogueController.StartDialogue(questData.AcceptDialogue);
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log($"퀘스트 수락: {questData.QuestName}");
#endif
            }
        }
    }

    private void HandleCompleteQuest(NpcInteractionContext context, QuestRuntimeData quest)
    {
        if (quest == null || quest.QuestData == null) return;

        string questId = quest.QuestData.QuestId;
        bool completed = QuestManager.Instance.CompleteQuest(questId);
        if (!completed) return;

        if (_dialogueController != null)
        {
            if (quest.QuestData.CompleteDialogue != null)
            {
                _dialogueController.StartDialogue(quest.QuestData.CompleteDialogue);
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log($"퀘스트 완료: {quest.QuestData.QuestName}");
#endif
            }
        }
    }

    private void HandleInProgressQuest(NpcInteractionContext context, QuestRuntimeData quest)
    {
        if (quest == null || quest.QuestData == null) return;

        // 배달 퀘스트라면, 이 NPC에게 아이템 전달을 시도합니다.
        if (quest.QuestData.ObjectiveType == EQuestObjectiveType.DeliverItem && quest.QuestData.TargetNpcId == context.NpcId)
        {
            bool delivered = QuestManager.Instance.TryDeliverItemToNpc(context.NpcId);
            if (delivered)
            {
                if (_dialogueController != null && quest.QuestData.InProgressDialogue != null)
                {
                    _dialogueController.StartDialogue(quest.QuestData.InProgressDialogue);
                }
                return;
            }
        }

        if (_dialogueController != null)
        {
            if (quest.QuestData.InProgressDialogue != null)
            {
                _dialogueController.StartDialogue(quest.QuestData.InProgressDialogue);
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log($"진행 중 퀘스트: {quest.QuestData.QuestName}");
#endif
            }
        }
    }

    private void HandleNoQuest(NpcInteractionContext context)
    {
        if (context == null || context.Npc == null) return;

        NpcQuest provider = context.Npc.GetComponent<NpcQuest>();

        // NPC 전용 퀘스트는 있지만 아직 조건이 안 맞는 경우도 있을 수 있으니 확인합니다.
        if (provider != null && provider.Quests != null && provider.Quests.Count > 0)
        {
            QuestDataSO firstQuest = provider.Quests[0];
            if (firstQuest != null && firstQuest.NoQuestDialogue != null && _dialogueController != null)
            {
                _dialogueController.StartDialogue(firstQuest.NoQuestDialogue);
                return;
            }
        }
#if UNITY_EDITOR
        Debug.Log("관련된 퀘스트가 없습니다.");
#endif
    }
}
