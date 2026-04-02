using UnityEngine;
using System;
using System.Collections.Generic;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    private PlayerInventoryAbility _playerInventory;

    private readonly Dictionary<string, QuestRuntimeData> _activeQuests = new();
    private readonly HashSet<string> _completedMainQuestIds = new();
    private readonly HashSet<string> _completedSubQuestIds = new();
    private Dictionary<EQuestRewardType, IQuestRewardHandler> _rewardHandlers;

    public IReadOnlyDictionary<string, QuestRuntimeData> ActiveQuests => _activeQuests;

    public event Action<QuestRuntimeData> OnQuestAccepted;
    public event Action<QuestRuntimeData> OnQuestUpdated;
    public event Action<QuestRuntimeData> OnQuestCompleted;
    public event Action<string> OnQuestRemoved;

    public static event Action OnQuestManagerReady;

    public List<QuestRuntimeData> GetActiveQuestList()
    {
        return new List<QuestRuntimeData>(_activeQuests.Values);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _rewardHandlers = new Dictionary<EQuestRewardType, IQuestRewardHandler>
        {
            { EQuestRewardType.Gold, new QuestGoldRewardHandler() },
            { EQuestRewardType.Item, new QuestItemRewardHandler(_playerInventory) },
            { EQuestRewardType.Friendship, new QuestFriendshipRewardHandler() }
        };
        OnQuestManagerReady?.Invoke();
    }

    private void OnEnable()
    {
        PlayerInventoryAbility.OnLocalPlayerReady += OnPlayerReady;
        GatheringObject.OnGatheringCompleted += HandleGatheringCompleted;
    }

    private void OnDisable()
    {
        PlayerInventoryAbility.OnLocalPlayerReady -= OnPlayerReady;
        GatheringObject.OnGatheringCompleted -= HandleGatheringCompleted;
    }

    private void OnPlayerReady(PlayerInventoryAbility ability)
    {
#if UNITY_EDITOR
        Debug.Log("PlayerInventoryAbility 확인");
#endif
        _playerInventory = ability;
    }

    private void HandleGatheringCompleted(GatheringObject obj)
    {
        if (obj == null || obj.GatheringData == null) return;
        ReportObjectBroken(obj.GatheringData.ObjectName);
    }

    // 현재 진행 중인 퀘스트가 하나라도 있는지 확인합니다.
    public bool HasActiveQuest()
    {
        return _activeQuests.Count > 0;
    }

    // 받은 퀘스트가 있는 지 확인하는 메서드입니다.
    public bool HasQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return false;
        return _activeQuests.ContainsKey(questId);
    }

    public QuestRuntimeData GetQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return null;
        _activeQuests.TryGetValue(questId, out QuestRuntimeData quest);
        return quest;
    }

    // 수락받을 수 있는 퀘스트인지 확인하는 메서드입니다. (메인, 서브 퀘스트처럼 한 번 클리어한 퀘스트는 클리어 불가능)
    public bool CanAcceptQuest(QuestDataSO questData)
    {
        if (questData == null || string.IsNullOrEmpty(questData.QuestId)) return false;
        if (_activeQuests.ContainsKey(questData.QuestId)) return false;

        switch (questData.QuestCategory)
        {
            case EQuestCategory.Main:
                return !_completedMainQuestIds.Contains(questData.QuestId);

            case EQuestCategory.Sub:
                return !_completedSubQuestIds.Contains(questData.QuestId);

            case EQuestCategory.Daily:
                return DailyQuestManager.Instance == null ||
                       DailyQuestManager.Instance.CanAcceptDailyQuest(questData);

        }
        return true;
    }

    // 이미 같은 퀘스트를 수락 받았는 지 확인하는 메서드입니다.
    public bool AcceptQuest(QuestDataSO questData)
    {
        if (!CanAcceptQuest(questData)) return false;

        QuestRuntimeData runtimeData = new QuestRuntimeData(questData);
        _activeQuests.Add(questData.QuestId, runtimeData);

        OnQuestAccepted?.Invoke(runtimeData);
        return true;
    }

    public void ReportObjectBroken(string objectId, int amount = 1)
    {
        if (string.IsNullOrEmpty(objectId)) return;
        TryAddProgress(EQuestObjectiveType.BreakObject, objectId, amount);
    }

    public void ReportItemCollected(string itemId, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        TryAddProgress(EQuestObjectiveType.CollectItem, itemId, amount);
    }

    public void ReportNpcTalked(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return;
        TryAddProgress(EQuestObjectiveType.TalkToNpc, npcId, 1);
    }

    public bool TryDeliverItemToNpc(string npcId)
    {
        if (string.IsNullOrEmpty(npcId) || _activeQuests.Count == 0) return false;

        foreach (QuestRuntimeData quest in _activeQuests.Values)
        {
            if (quest == null || quest.QuestData == null) continue;
            if (quest.Status != EQuestStatus.InProgress) continue;

            QuestDataSO questData = quest.QuestData;

            if (questData.ObjectiveType != EQuestObjectiveType.DeliverItem) continue;
            if (questData.TargetNpcId != npcId) continue;

            string itemId = questData.TargetItemId;
            int amount = questData.RequiredAmount;

            if (!TryConsumeItem(itemId, amount))
            {
                return false;
            }

            quest.CurrentAmount = amount;
            quest.Status = EQuestStatus.CanComplete;
            OnQuestUpdated?.Invoke(quest);
            return true;
        }

        return false;
    }

    private void TryAddProgress(EQuestObjectiveType objectiveType, string targetId, int amount)
    {
        if (_activeQuests.Count == 0) return;
        if (string.IsNullOrEmpty(targetId)) return;
        if (amount <= 0) return;

        foreach (QuestRuntimeData quest in _activeQuests.Values)
        {
            if (quest == null || quest.QuestData == null) continue;
            if (quest.Status != EQuestStatus.InProgress) continue;

            QuestDataSO questData = quest.QuestData;

            if (questData.ObjectiveType != objectiveType) continue;
            if (!IsTargetMatched(questData, objectiveType, targetId)) continue;

            quest.CurrentAmount += amount;

            if (quest.CurrentAmount >= questData.RequiredAmount)
            {
                quest.CurrentAmount = questData.RequiredAmount;
                quest.Status = EQuestStatus.CanComplete;
            }

            OnQuestUpdated?.Invoke(quest);
        }
    }

    private bool IsTargetMatched(QuestDataSO questData, EQuestObjectiveType objectiveType, string targetId)
    {
        switch (objectiveType)
        {
            case EQuestObjectiveType.BreakObject:
                return questData.TargetObjectId == targetId;

            case EQuestObjectiveType.CollectItem:
                return questData.TargetItemId == targetId;

            case EQuestObjectiveType.TalkToNpc:
                return questData.TargetNpcId == targetId;

            default:
                return false;
        }
    }

    private bool TryConsumeItem(string itemId, int amount)
    {
        if (_playerInventory == null || string.IsNullOrEmpty(itemId) || amount <= 0) return false;

        // todo. 아이템 차감 후 true 반환

        return false;
    }

    // 퀘스트를 완료할 수 있는 지 확인하는 메서드입니다.
    public bool CanCompleteQuest(string questId)
    {
        QuestRuntimeData quest = GetQuest(questId);
        return quest != null && quest.Status == EQuestStatus.CanComplete;
    }

    // 퀘스트가 완료되었는 지 확인하는 메서드입니다.
    public bool CompleteQuest(string questId)
    {
        QuestRuntimeData quest = GetQuest(questId);
        if (quest == null || quest.Status != EQuestStatus.CanComplete) return false;

        GiveReward(quest.QuestData.Reward);
        quest.Status = EQuestStatus.Completed;

        switch (quest.QuestData.QuestCategory)
        {
            case EQuestCategory.Main:
                _completedMainQuestIds.Add(questId);
                break;

            case EQuestCategory.Sub:
                _completedSubQuestIds.Add(questId);
                break;

            case EQuestCategory.Daily:
                DailyQuestManager.Instance?.MarkCompletedToday(questId);
                break;
        }

        OnQuestCompleted?.Invoke(quest);
        RemoveQuest(questId);
        return true;
    }

    // 완료된 퀘스트를 목록에서 비우는 메서드입니다.
    public bool RemoveQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId) || !_activeQuests.ContainsKey(questId)) return false;

        _activeQuests.Remove(questId);
        OnQuestRemoved?.Invoke(questId);
        return true;
    }

    // 일일 퀘스트를 전체 삭제하는 메서드입니다.
    public void RemoveAllDailyQuests()
    {
        List<string> removeKeys = new();

        foreach (var pair in _activeQuests)
        {
            if (pair.Value == null || pair.Value.QuestData == null) continue;
            if (pair.Value.QuestData.QuestCategory != EQuestCategory.Daily) continue;

            removeKeys.Add(pair.Key);
        }

        foreach (string key in removeKeys)
        {
            _activeQuests.Remove(key);
            OnQuestRemoved?.Invoke(key);
        }
    }

    private void GiveReward(QuestRewardData rewardData)
    {
        if (rewardData == null || rewardData.Rewards == null) return;

        foreach (var reward in rewardData.Rewards)
        {
            if (reward == null || !reward.IsValid()) continue;

            if (_rewardHandlers.TryGetValue(reward.RewardType, out var handler))
            {
                handler.HandleReward(reward);
            }
        }
    }

    // 선행 퀘스트 확인용 메서드입니다.
    public bool IsQuestCompleted(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return false;

        if (_completedMainQuestIds.Contains(questId)) return true;
        if (_completedSubQuestIds.Contains(questId)) return true;

        return false;
    }

    public QuestRuntimeData GetCompletableQuestByNpc(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return null;

        foreach (QuestRuntimeData quest in _activeQuests.Values)
        {
            if (quest == null || quest.QuestData == null) continue;
            if (quest.Status != EQuestStatus.CanComplete) continue;

            if (quest.QuestData.CompleteNpcId == npcId)
            {
                return quest;
            }
        }

        return null;
    }

    public QuestRuntimeData GetInProgressQuestByNpc(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return null;

        foreach (QuestRuntimeData quest in _activeQuests.Values)
        {
            if (quest == null || quest.QuestData == null) continue;
            if (quest.Status != EQuestStatus.InProgress) continue;

            QuestDataSO data = quest.QuestData;

            if (data.StartNpcId == npcId || data.CompleteNpcId == npcId || data.TargetNpcId == npcId)
            {
                return quest;
            }
        }

        return null;
    }
}
