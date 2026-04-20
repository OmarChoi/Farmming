using UnityEngine;
using System;
using System.Collections.Generic;

public class QuestMarkService : MonoBehaviour
{
    [Header("참조 컴포넌트")]
    [SerializeField] private QuestManager _questManager;
    [SerializeField] private NpcFriendshipManager _friendshipManager;

    private PlayerQuestAbility _localPlayerQuestAbility;

    public event Action OnMarkStateChanged;

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

    private void OnEnable()
    {
        GameSceneInit.OnLocalPlayerSceneReady += HandleLocalPlayerReady;
        QuestManager.OnQuestManagerReady += HandleQuestManagerReady;
    }

    private void OnDisable()
    {
        GameSceneInit.OnLocalPlayerSceneReady -= HandleLocalPlayerReady;
        QuestManager.OnQuestManagerReady -= HandleQuestManagerReady;
    }

    private void HandleLocalPlayerReady(PlayerController player)
    {
        if (player == null) return;
        _localPlayerQuestAbility = player.GetAbility<PlayerQuestAbility>();
    }

    private PlayerQuestAbility GetLocalPlayerQuestAbility()
    {
        return _localPlayerQuestAbility;
    }

    private void HandleQuestManagerReady()
    {
        _questManager = QuestManager.Instance;
    }

    public void MarkQuestBoardChecked()
    {
        PlayerQuestAbility playerQuestAbility = GetLocalPlayerQuestAbility();
        if (playerQuestAbility == null) return;

        int currentDay = TimeEvents.CurrentDay;
        if (playerQuestAbility.LastCheckedDailyQuestDay == currentDay) return;

        playerQuestAbility.MarkDailyQuestBoardChecked(currentDay);
        OnMarkStateChanged?.Invoke();
    }

    public void MarkShrineChecked()
    {
        PlayerQuestAbility playerQuestAbility = GetLocalPlayerQuestAbility();
        if (playerQuestAbility == null) return;
        if (WorldEffectQuestService.Instance == null) return;

        string activeQuestId = WorldEffectQuestService.Instance.ActiveQuestId;
        if (string.IsNullOrEmpty(activeQuestId)) return;
        if (playerQuestAbility.LastCheckedWorldEffectQuestId == activeQuestId) return;

        playerQuestAbility.MarkWorldEffectQuestChecked(activeQuestId);
        OnMarkStateChanged?.Invoke();
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

        EWorldMarkVisualType highestPriorityStatus = EWorldMarkVisualType.None;

        // 1. 활성 퀘스트 목록을 한 번만 순회하여 상태를 확인합니다.
        foreach (QuestRuntimeData quest in _questManager.GetActiveQuestList())
        {
            if (quest == null || quest.QuestData == null) continue;

            if (IsQuestCompletableAtNpc(quest, npcId))
            {
                return EWorldMarkVisualType.CanComplete; // 최고 우선순위
            }

            if (highestPriorityStatus < EWorldMarkVisualType.InProgress &&
                quest.Status == EQuestStatus.InProgress &&
                IsQuestRelatedToNpc(quest.QuestData, npcId))
            {
                highestPriorityStatus = EWorldMarkVisualType.InProgress;
            }
        }

        // 2. 진행 중인 퀘스트가 없을 때만 수락 가능한 퀘스트를 확인합니다.
        if (highestPriorityStatus == EWorldMarkVisualType.None)
        {
            NpcQuest provider = npc.GetComponent<NpcQuest>();
            if (provider != null && provider.Quests != null)
            {
                foreach (QuestDataSO questData in provider.Quests)
                {
                    if (questData == null || questData.StartNpcId != npcId) continue;

                    if (CanOfferQuest(npcId, questData))
                    {
                        return EWorldMarkVisualType.Available;
                    }
                }
            }
        }

        return highestPriorityStatus;
    }

    public EWorldMarkVisualType GetQuestBoardMarkVisualType(QuestBoardDataSO boardData)
    {
        if (_questManager == null || DailyQuestManager.Instance == null || boardData == null)
        {
            return EWorldMarkVisualType.None;
        }

        if (boardData.AllQuests == null || boardData.AllQuests.Count == 0)
        {
            return EWorldMarkVisualType.None;
        }

        IReadOnlyList<QuestDataSO> todayQuests = DailyQuestManager.Instance.TodayDailyQuests;
        if (todayQuests == null || todayQuests.Count == 0)
        {
            return EWorldMarkVisualType.None;
        }

        PlayerQuestAbility playerQuestAbility = GetLocalPlayerQuestAbility();
        bool hasCheckedToday = playerQuestAbility != null &&
                               playerQuestAbility.HasCheckedDailyQuestBoardToday();

        HashSet<QuestDataSO> boardQuestSet = new HashSet<QuestDataSO>(boardData.AllQuests);
        bool hasNewQuest = false;

        foreach (QuestDataSO questData in todayQuests)
        {
            if (questData == null) continue;
            if (!boardQuestSet.Contains(questData)) continue;

            QuestRuntimeData activeQuest = _questManager.GetQuest(questData.QuestId);

            if (activeQuest != null && activeQuest.Status == EQuestStatus.CanComplete)
            {
                return EWorldMarkVisualType.CanComplete;
            }

            if (_questManager.CanAcceptQuest(questData))
            {
                hasNewQuest = true;
            }
        }

        if (hasNewQuest && !hasCheckedToday)
        {
            return EWorldMarkVisualType.Updated;
        }

        return EWorldMarkVisualType.None;
    }

    public EWorldMarkVisualType GetShrineQuestMarkVisualType()
    {
        if (_questManager == null || WorldEffectQuestService.Instance == null)
        {
            return EWorldMarkVisualType.None;
        }

        string activeQuestId = WorldEffectQuestService.Instance.ActiveQuestId;
        if (string.IsNullOrEmpty(activeQuestId))
        {
            return EWorldMarkVisualType.None;
        }

        QuestRuntimeData quest = _questManager.GetQuest(activeQuestId);
        if (quest != null && quest.Status == EQuestStatus.CanComplete)
        {
            return EWorldMarkVisualType.CanComplete;
        }

        PlayerQuestAbility playerQuestAbility = GetLocalPlayerQuestAbility();
        bool hasChecked = playerQuestAbility != null &&
                          playerQuestAbility.HasCheckedWorldEffectQuest(activeQuestId);

        return hasChecked ? EWorldMarkVisualType.None : EWorldMarkVisualType.Updated;
    }

    private bool IsQuestCompletableAtNpc(QuestRuntimeData quest, string npcId)
    {
        if (quest == null || quest.QuestData == null) return false;

        if (quest.Status == EQuestStatus.CanComplete && quest.QuestData.CompleteNpcId == npcId) return true;

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
