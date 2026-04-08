using UnityEngine;
using System.Collections.Generic;

public class NpcQuestService : MonoBehaviour
{
    [SerializeField] private NpcDialogueController _dialogueController;
    [SerializeField] private QuestManager _questManager;
    [SerializeField] private NpcFriendshipManager _npcFriendshipManager;

    private IQuestProgressService _questProgressService;
    private IFriendshipService _friendshipService;

    private void Awake()
    {
        if (_dialogueController == null)
        {
            _dialogueController = FindFirstObjectByType<NpcDialogueController>();
        }
        if (_questManager == null)
        {
            _questManager = FindFirstObjectByType<QuestManager>();
        }
        if (_npcFriendshipManager == null)
        {
            _npcFriendshipManager = FindFirstObjectByType<NpcFriendshipManager>();
        }

        _questProgressService = _questManager;
        _friendshipService = _npcFriendshipManager;
    }

    public void ExecuteQuestInteraction(NpcInteractionContext context)
    {
        if (context == null || context.Npc == null || _questProgressService == null) return;

        List<NpcQuestEntry> entries = FindQuestEntries(context);

        if (entries.Count > 0)
        {
            ShowQuestEntries(context, entries);
            return;
        }

        HandleNoQuest(context);
    }

    private List<NpcQuestEntry> FindQuestEntries(NpcInteractionContext context)
    {
        List<NpcQuestEntry> result = new();
        HashSet<string> addedQuestIds = new();

        if (context == null || context.Npc == null || _questProgressService == null)
        {
            return result;
        }

        string npcId = context.NpcId;
        List<QuestRuntimeData> activeQuests = _questProgressService.GetActiveQuestList();

        // 1. 완료 가능한 퀘스트를 먼저 넣습니다.
        foreach (QuestRuntimeData quest in activeQuests)
        {
            if (quest == null || quest.QuestData == null) continue;
            if (quest.Status != EQuestStatus.CanComplete) continue;
            if (quest.QuestData.CompleteNpcId != npcId) continue;
            if (!addedQuestIds.Add(quest.QuestData.QuestId)) continue;

            result.Add(new NpcQuestEntry(quest.QuestData, quest, ENpcQuestEntryType.Completable));
        }

        // 2. 수락 가능한 퀘스트를 넣습니다.
        List<QuestDataSO> acceptableQuests = FindAcceptableQuests(context);
        foreach (QuestDataSO questData in acceptableQuests)
        {
            if (questData == null) continue;
            if (!addedQuestIds.Add(questData.QuestId)) continue;

            result.Add(new NpcQuestEntry(questData, null, ENpcQuestEntryType.Acceptable));
        }

        // 3. 진행 중인 퀘스트를 넣습니다.
        foreach (QuestRuntimeData quest in activeQuests)
        {
            if (quest == null || quest.QuestData == null) continue;
            if (quest.Status != EQuestStatus.InProgress) continue;
            if (!IsQuestRelatedToNpc(quest.QuestData, npcId)) continue;
            if (!addedQuestIds.Add(quest.QuestData.QuestId)) continue;

            bool canDeliverHere =
                quest.QuestData.ObjectiveType == EQuestObjectiveType.DeliverItem &&
                quest.QuestData.TargetNpcId == npcId &&
                quest.AreAllItemRequirementsCompleted();

            if (canDeliverHere)
            {
                result.Add(new NpcQuestEntry(quest.QuestData, quest, ENpcQuestEntryType.Completable));
            }
            else
            {
                result.Add(new NpcQuestEntry(quest.QuestData, quest, ENpcQuestEntryType.InProgress));
            }
        }

        return result;
    }

    private bool IsQuestRelatedToNpc(QuestDataSO questData, string npcId)
    {
        if (questData == null || string.IsNullOrEmpty(npcId)) return false;

        return questData.StartNpcId == npcId ||
               questData.CompleteNpcId == npcId ||
               questData.TargetNpcId == npcId;
    }

    private List<QuestDataSO> FindAcceptableQuests(NpcInteractionContext context)
    {
        List<QuestDataSO> result = new();

        if (context == null || context.Npc == null) return result;

        NpcQuest provider = context.Npc.Quest;
        if (provider == null || provider.Quests == null) return result;

        string npcId = context.NpcId;

        foreach (QuestDataSO questData in provider.Quests)
        {
            if (questData == null) continue;
            if (questData.StartNpcId != npcId) continue;
            if (!CanOfferQuest(context, questData)) continue;

            result.Add(questData);
        }

        return result;
    }

    private bool CanOfferQuest(NpcInteractionContext context, QuestDataSO questData)
    {
        if (context == null || context.Npc == null || questData == null) return false;
        if (_questProgressService == null) return false;

        // 1. 수락 가능한 퀘스트인지 확인합니다.
        if (!_questProgressService.CanAcceptQuest(questData))
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
        if (questData == null || _questProgressService == null) return false;
        if (questData.PrerequisiteQuests == null || questData.PrerequisiteQuests.Count == 0) return true;

        foreach (QuestDataSO prerequisite in questData.PrerequisiteQuests)
        {
            if (prerequisite == null || string.IsNullOrEmpty(prerequisite.QuestId)) continue;

            if (!_questProgressService.IsQuestCompleted(prerequisite.QuestId))
            {
                return false;
            }
        }
        return true;
    }

    private bool IsFriendshipSatisfied(string npcId, int requiredFriendship)
    {
        if (requiredFriendship <= 0) return true;
        if (string.IsNullOrEmpty(npcId) || _friendshipService == null) return false;

        int currentFriendship = _friendshipService.GetFriendship(npcId);
        return currentFriendship >= requiredFriendship;
    }

    private void ShowQuestEntries(NpcInteractionContext context, List<NpcQuestEntry> entries)
    {
        if (_dialogueController == null) return;

        List<NpcDialogueChoiceData> choices = new();

        if (entries != null)
        {
            foreach (NpcQuestEntry entry in entries)
            {
                if (entry == null || entry.QuestData == null) continue;

                NpcQuestEntry capturedEntry = entry;
                string buttonText = BuildQuestEntryButtonText(capturedEntry);

                choices.Add(new NpcDialogueChoiceData(buttonText, () => OnClickQuestEntry(context, capturedEntry)));
            }
        }
        choices.Add(new NpcDialogueChoiceData("돌아가기", () => _dialogueController.ShowDefaultChoices()));
        _dialogueController.ShowQuestChoices(choices);
    }

    private string BuildQuestEntryButtonText(NpcQuestEntry entry)
    {
        if (entry == null || entry.QuestData == null) return "퀘스트";

        switch (entry.EntryType)
        {
            case ENpcQuestEntryType.Completable:
                return $"<{entry.QuestData.QuestName}> [완료]";

            case ENpcQuestEntryType.InProgress:
                return $"<{entry.QuestData.QuestName}> [진행중]";

            case ENpcQuestEntryType.Acceptable:
                return $"<{entry.QuestData.QuestName}> [수락]";

            default:
                return entry.QuestData.QuestName;
        }
    }

    private void OnClickQuestEntry(NpcInteractionContext context, NpcQuestEntry entry)
    {
        if (entry == null || entry.QuestData == null) return;

        switch (entry.EntryType)
        {
            case ENpcQuestEntryType.Acceptable:
                HandleAcceptQuestEntry(context, entry.QuestData);
                break;

            case ENpcQuestEntryType.Completable:
                {
                    QuestRuntimeData runtimeQuest = entry.RuntimeData;
                    if (runtimeQuest == null || runtimeQuest.QuestData == null) return;

                    // 배달 퀘스트는 UI상 [완료]로 보여도 실제 상태는 아직 InProgress일 수 있습니다.
                    // 이 경우 먼저 전달 처리를 시도해서 내부 상태를 CanComplete로 맞춘 뒤 완료 처리합니다.
                    if (runtimeQuest.Status != EQuestStatus.CanComplete &&
                        runtimeQuest.QuestData.ObjectiveType == EQuestObjectiveType.DeliverItem &&
                        runtimeQuest.QuestData.TargetNpcId == context.NpcId)
                    {
                        bool delivered = _questProgressService.TryDeliverItemToNpc(runtimeQuest.QuestData.QuestId, context.NpcId);
                        if (!delivered) return;
                    }

                    HandleCompleteQuest(context, runtimeQuest);
                    break;
                }

            case ENpcQuestEntryType.InProgress:
                HandleInProgressQuest(context, entry.RuntimeData);
                break;
        }
    }

    private void HandleAcceptQuestEntry(NpcInteractionContext context, QuestDataSO questData)
    {
        if (questData == null || _dialogueController == null) return;

        if (questData.IsForcedAccept)
        {
            AcceptQuestWithResultDialogue(context, questData);
            return;
        }
        if (questData.AcceptDialogue != null)
        {
            _dialogueController.StartDialogue(questData.AcceptDialogue, EDialogueUiState.Quest,
            () => ShowAcceptConfirmChoices(context, questData));
        }
        else
        {
            ShowAcceptConfirmChoices(context, questData);
        }
    }

    private void ShowAcceptConfirmChoices(NpcInteractionContext context, QuestDataSO questData)
    {
        if (_dialogueController == null || questData == null) return;

        List<NpcDialogueChoiceData> choices = new()
        {
            new NpcDialogueChoiceData("네", () => AcceptQuestWithResultDialogue(context, questData)),
            new NpcDialogueChoiceData("아니요", () => DeclineQuestWithDialogue(context, questData))
        };

        _dialogueController.ShowQuestChoices(choices, false);
    }

    private void AcceptQuestWithResultDialogue(NpcInteractionContext context, QuestDataSO questData)
    {
        if (questData == null || _questProgressService == null) return;

        bool accepted = _questProgressService.AcceptQuest(questData);
        if (!accepted) return;

        if (_dialogueController != null && questData.AcceptResultDialogue != null)
        {
            _dialogueController.StartDialogue(questData.AcceptResultDialogue, EDialogueUiState.Quest);
        }
    }

    private void DeclineQuestWithDialogue(NpcInteractionContext context, QuestDataSO questData)
    {
        if (questData == null || _dialogueController == null) return;

        if (questData.DeclineDialogue != null)
        {
            _dialogueController.StartDialogue(questData.DeclineDialogue, EDialogueUiState.Quest);
        }
        else
        {
            ShowQuestEntries(context, FindQuestEntries(context));
        }
    }

    private void HandleCompleteQuest(NpcInteractionContext context, QuestRuntimeData quest)
    {
        if (quest == null || quest.QuestData == null) return;

        string questId = quest.QuestData.QuestId;
        bool completed = _questProgressService.CompleteQuest(questId);
        if (!completed) return;

        if (_dialogueController != null && quest.QuestData.CompleteDialogue != null)
        {
            _dialogueController.StartDialogue(quest.QuestData.CompleteDialogue, EDialogueUiState.Quest);
        }
    }

    private void HandleInProgressQuest(NpcInteractionContext context, QuestRuntimeData quest)
    {
        if (quest == null || quest.QuestData == null) return;

        // 배달 퀘스트라면, 이 NPC에게 아이템 전달을 시도합니다.
        if (quest.QuestData.ObjectiveType == EQuestObjectiveType.DeliverItem && quest.QuestData.TargetNpcId == context.NpcId)
        {
            bool delivered = _questProgressService.TryDeliverItemToNpc(quest.QuestData.QuestId, context.NpcId);
            if (delivered)
            {
                HandleCompleteQuest(context, quest);
                return;
            }
        }

        if (_dialogueController != null && quest.QuestData.InProgressDialogue != null)
        {
            _dialogueController.StartDialogue(quest.QuestData.InProgressDialogue, EDialogueUiState.Quest);
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
                _dialogueController.StartDialogue(firstQuest.NoQuestDialogue, EDialogueUiState.Quest);
                return;
            }
        }
    }

    public void ExecuteTutorialQuestInteraction(NpcInteractionContext context, QuestDataSO questData)
    {
        if (context == null || context.Npc == null || questData == null || _questProgressService == null) return;

        if (!CanOfferQuest(context, questData))
        {
            HandleNoQuest(context);
            return;
        }

        HandleAcceptQuestEntry(context, questData);
    }
}
