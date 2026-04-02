using UnityEngine;
using System;
using System.Collections.Generic;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("플레이어 컴포넌트")]
    [SerializeField] private PlayerInventoryAbility _playerInventory;

    [Header("일일 퀘스트 개수")]
    [SerializeField] private int _dailyQuestCounts = 3;
    private QuestBoardDataSO _dailyQuestBoardData;

    private readonly Dictionary<string, QuestRuntimeData> _activeQuests = new();
    private readonly HashSet<string> _completedMainQuestIds = new();
    private readonly HashSet<string> _completedSubQuestIds = new();
    private readonly Dictionary<string, int> _dailyQuestCompletedDays = new();

    private Dictionary<EQuestRewardType, IQuestRewardHandler> _rewardHandlers;

    public IReadOnlyDictionary<string, QuestRuntimeData> ActiveQuests => _activeQuests;
    public List<QuestRuntimeData> GetActiveQuestList()
    {
        return new List<QuestRuntimeData>(_activeQuests.Values);
    }

    private readonly List<QuestDataSO> _todayDailyQuests = new();
    public IReadOnlyList<QuestDataSO> TodayDailyQuests => _todayDailyQuests;

    public event Action<QuestRuntimeData> OnQuestAccepted;
    public event Action<QuestRuntimeData> OnQuestUpdated;
    public event Action<QuestRuntimeData> OnQuestCompleted;
    public event Action<string> OnQuestRemoved;

    public static event Action OnQuestManagerReady;

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
        GatheringObject.OnGatheringCompleted += HandleGatheringCompleted;
        TimeEvents.OnNetDayChanged += HandleDayChanged;
    }

    private void OnDisable()
    {
        GatheringObject.OnGatheringCompleted -= HandleGatheringCompleted;
        TimeEvents.OnNetDayChanged -= HandleDayChanged;
    }

    public void SetDailyQuestBoardData(QuestBoardDataSO boardData)
    {
        _dailyQuestBoardData = boardData;
        RefreshTodayDailyQuests(_dailyQuestBoardData, _dailyQuestCounts);
    }

    private void HandleGatheringCompleted(GatheringObject obj)
    {
        if (obj == null || obj.GatheringData == null) return;
        AddProgress(EQuestObjectiveType.BreakObject, obj.GatheringData.ObjectName);
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
        if (questData == null) return false;
        if (string.IsNullOrEmpty(questData.QuestId)) return false;

        if (_activeQuests.ContainsKey(questData.QuestId)) return false;

        switch (questData.QuestCategory)
        {
            case EQuestCategory.Main:
                return !_completedMainQuestIds.Contains(questData.QuestId);

            case EQuestCategory.Sub:
                return !_completedSubQuestIds.Contains(questData.QuestId);

            case EQuestCategory.Daily:
                int currentDay = TimeEvents.CurrentDay;

                if (_dailyQuestCompletedDays.TryGetValue(questData.QuestId, out int completedDay))
                {
                    return completedDay != currentDay;
                }
                return true;
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

    // 현재 퀘스트의 진행도를 확인하는 메서드입니다.
    public void AddProgress(EQuestObjectiveType objectiveType, string targetId, int amount = 1)
    {
        if (_activeQuests.Count == 0) return;
        if (string.IsNullOrEmpty(targetId)) return;

        foreach (QuestRuntimeData quest in _activeQuests.Values)
        {
            if (quest == null) continue;
            if (quest.Status != EQuestStatus.InProgress) continue;
            if (quest.QuestData == null) continue;

            QuestDataSO questData = quest.QuestData;

            if (questData.ObjectiveType != objectiveType) continue;
            if (questData.TargetId != targetId) continue;

            quest.CurrentAmount += amount;

            if (quest.CurrentAmount >= questData.RequiredAmount)
            {
                quest.CurrentAmount = questData.RequiredAmount;
                quest.Status = EQuestStatus.CanComplete;
            }

            OnQuestUpdated?.Invoke(quest);
        }
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
        if (quest == null) return false;
        if (quest.Status != EQuestStatus.CanComplete) return false;

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
                _dailyQuestCompletedDays[questId] = TimeEvents.CurrentDay;
                break;
        }

        OnQuestCompleted?.Invoke(quest);
        RemoveQuest(questId);
        return true;
    }

    // 완료된 퀘스트를 목록에서 비우는 메서드입니다.
    public bool RemoveQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return false;
        if (!_activeQuests.ContainsKey(questId)) return false;

        _activeQuests.Remove(questId);
        OnQuestRemoved?.Invoke(questId);
        return true;
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

    private void HandleDayChanged()
    {
        ResetDailyQuests();
        RefreshTodayDailyQuests(_dailyQuestBoardData, _dailyQuestCounts);

#if UNITY_EDITOR
        Debug.Log($"일일 퀘스트 초기화");
#endif
    }

    public void ResetDailyQuests()
    {
        List<string> removeKeys = new();

        foreach (var pair in _activeQuests)
        {
            if (pair.Value == null || pair.Value.QuestData == null) continue;

            if (pair.Value.QuestData.QuestCategory == EQuestCategory.Daily)
            {
                removeKeys.Add(pair.Key);
            }
        }

        foreach (string key in removeKeys)
        {
            _activeQuests.Remove(key);
            OnQuestRemoved?.Invoke(key);
        }
    }

    public void RefreshTodayDailyQuests(QuestBoardDataSO boardData, int selectCount = 2)
    {
        _todayDailyQuests.Clear();

        if (boardData == null || boardData.AllQuests == null) return;

        List<QuestDataSO> candidates = new();

        foreach (QuestDataSO quest in boardData.AllQuests)
        {
            if (quest == null) continue;
            if (quest.QuestCategory != EQuestCategory.Daily) continue;

            candidates.Add(quest);
        }

        if (candidates.Count <= selectCount)
        {
            _todayDailyQuests.AddRange(candidates);
            return;
        }

        for (int i = 0; i < selectCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
            _todayDailyQuests.Add(candidates[randomIndex]);
            candidates.RemoveAt(randomIndex);
        }
    }
}
