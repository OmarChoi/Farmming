using UnityEngine;
using System;

public class PlayerQuestAbility : PlayerAbility, ISaveableAbility
{
    private ETutorialState _tutorialState = ETutorialState.None;
    public ETutorialState TutorialState => _tutorialState;

    public bool IsInitialized { get; private set; }
    public event Action OnInitialized;

    private int _lastCheckedDailyQuestDay = -1;
    private string _lastCheckedWorldEffectQuestId;

    public int LastCheckedDailyQuestDay => _lastCheckedDailyQuestDay;
    public string LastCheckedWorldEffectQuestId => _lastCheckedWorldEffectQuestId;

    public void SetTutorialState(ETutorialState state)
    {
        _tutorialState = state;
    }

    public void MarkDailyQuestBoardChecked(int day)
    {
        _lastCheckedDailyQuestDay = day;
    }

    public void MarkWorldEffectQuestChecked(string questId)
    {
        _lastCheckedWorldEffectQuestId = questId;
    }

    public bool HasCheckedDailyQuestBoardToday()
    {
        return _lastCheckedDailyQuestDay == TimeEvents.CurrentDay;
    }

    public bool HasCheckedWorldEffectQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return false;
        return _lastCheckedWorldEffectQuestId == questId;
    }

    public void ExportTo(PlayerSaveData saveData)
    {
        if (saveData == null) return;

        saveData.TutorialState = _tutorialState;
        saveData.Quest = QuestManager.Instance != null ? QuestManager.Instance.ExportSaveData() : new QuestSaveData();

        saveData.LastCheckedDailyQuestDay = _lastCheckedDailyQuestDay;
        saveData.LastCheckedWorldEffectQuestId = _lastCheckedWorldEffectQuestId;
    }

    public void ImportFrom(PlayerSaveData saveData)
    {
        if (saveData == null) return;

        _tutorialState = saveData.TutorialState;

        _lastCheckedDailyQuestDay = saveData.LastCheckedDailyQuestDay;
        _lastCheckedWorldEffectQuestId = saveData.LastCheckedWorldEffectQuestId;

        if (!_owner.IsMine) return;
        if (QuestManager.Instance == null) return;

        if (saveData.Quest != null)
        {
            QuestManager.Instance.ImportSaveData(saveData.Quest);
        }
        else
        {
            QuestManager.Instance.InitializeEmpty();
        }

        IsInitialized = true;
        OnInitialized?.Invoke();
    }

    public void InitializeEmptyState()
    {
        _tutorialState = ETutorialState.None;
        _lastCheckedDailyQuestDay = -1;
        _lastCheckedWorldEffectQuestId = null;

        if (_owner.IsMine && QuestManager.Instance != null)
        {
            QuestManager.Instance.InitializeEmpty();
        }

        IsInitialized = true;
        OnInitialized?.Invoke();
    }
}
