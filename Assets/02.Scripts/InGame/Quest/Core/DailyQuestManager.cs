using UnityEngine;
using System.Collections.Generic;

public class DailyQuestManager : MonoBehaviour
{
    public static DailyQuestManager Instance { get; private set; }

    private QuestBoardDataSO _dailyQuestBoardData;

    [Header("일일 퀘스트 개수")]
    [SerializeField] private int _dailyQuestCounts = 3;

    private readonly List<QuestDataSO> _todayDailyQuests = new();
    private readonly Dictionary<string, int> _dailyQuestCompletedDays = new();
    public IReadOnlyList<QuestDataSO> TodayDailyQuests => _todayDailyQuests;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        TimeEvents.OnNetDayStarted += HandleDayChanged;
    }

    private void OnDisable()
    {
        TimeEvents.OnNetDayStarted -= HandleDayChanged;
    }

    public void SetDailyQuestBoardData(QuestBoardDataSO boardData)
    {
        _dailyQuestBoardData = boardData;
        RefreshTodayDailyQuests();
    }

    public bool CanAcceptDailyQuest(QuestDataSO questData)
    {
        if (questData == null || string.IsNullOrEmpty(questData.QuestId)) return false;
        return !_dailyQuestCompletedDays.ContainsKey(questData.QuestId);
    }

    public void MarkCompletedToday(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return;
        _dailyQuestCompletedDays[questId] = TimeEvents.CurrentDay;
    }

    private void HandleDayChanged()
    {
        _dailyQuestCompletedDays.Clear();
        QuestManager.Instance?.RemoveAllDailyQuests();
        RefreshTodayDailyQuests();

#if UNITY_EDITOR
        Debug.Log($"일일 퀘스트 초기화");
#endif
    }

    public void RefreshTodayDailyQuests()
    {
        _todayDailyQuests.Clear();

        if (_dailyQuestBoardData == null || _dailyQuestBoardData.AllQuests == null) return;

        List<QuestDataSO> candidates = new();

        foreach (QuestDataSO quest in _dailyQuestBoardData.AllQuests)
        {
            if (quest == null) continue;
            if (quest.QuestCategory != EQuestCategory.Daily) continue;

            candidates.Add(quest);
        }

        if (candidates.Count <= _dailyQuestCounts)
        {
            _todayDailyQuests.AddRange(candidates);
            return;
        }

        for (int i = 0; i < _dailyQuestCounts; i++)
        {
            int randomIndex = Random.Range(0, candidates.Count);
            _todayDailyQuests.Add(candidates[randomIndex]);
            candidates.RemoveAt(randomIndex);
        }
    }
}
