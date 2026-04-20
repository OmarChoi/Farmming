using UnityEngine;
using System;

public class PlayerQuestAbility : PlayerAbility, ISaveableAbility
{
    private ETutorialState _tutorialState = ETutorialState.None;
    public ETutorialState TutorialState => _tutorialState;

    public bool IsInitialized { get; private set; }
    public event Action OnInitialized;

    public void SetTutorialState(ETutorialState state)
    {
        _tutorialState = state;
    }

    public void ExportTo(PlayerSaveData saveData)
    {
        if (saveData == null) return;

        saveData.TutorialState = _tutorialState;
        saveData.Quest = QuestManager.Instance != null ? QuestManager.Instance.ExportSaveData() : new QuestSaveData();
    }

    public void ImportFrom(PlayerSaveData saveData)
    {
        if (saveData == null) return;

        _tutorialState = saveData.TutorialState;

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

        if (_owner.IsMine && QuestManager.Instance != null)
        {
            QuestManager.Instance.InitializeEmpty();
        }

        IsInitialized = true;
        OnInitialized?.Invoke();
    }
}
