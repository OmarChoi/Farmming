using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class QuestRuntimeData
{
    public QuestDataSO QuestData;
    public EQuestStatus Status;

    public int CurrentAmount;

    [SerializeField] private List<QuestItemProgressEntry> _itemProgressList = new();
    private Dictionary<int, int> _itemProgressMap;

    public QuestRuntimeData(QuestDataSO questData)
    {
        QuestData = questData;
        Status = EQuestStatus.InProgress;
        CurrentAmount = 0;

        InitializeItemProgress();
    }

    public void InitializeItemProgress()
    {
        _itemProgressMap = new Dictionary<int, int>();

        if (QuestData == null || QuestData.ItemRequirements == null) return;

        foreach (var requirement in QuestData.ItemRequirements)
        {
            if (requirement.Item == null) continue;

            int itemId = requirement.ItemId;
            if (itemId < 0) continue;

            if (!_itemProgressMap.ContainsKey(itemId))
            {
                _itemProgressMap[itemId] = 0;
            }
        }

        SyncListFromMap();
    }

    public int GetItemProgress(int itemId)
    {
        EnsureMap();

        if (itemId < 0) return 0;
        return _itemProgressMap.TryGetValue(itemId, out int value) ? value : 0;
    }

    public void AddItemProgress(int itemId, int amount, int maxAmount)
    {
        EnsureMap();

        if (itemId < 0 || amount <= 0) return;

        if (!_itemProgressMap.ContainsKey(itemId))
        {
            _itemProgressMap[itemId] = 0;
        }

        _itemProgressMap[itemId] += amount;

        if (_itemProgressMap[itemId] > maxAmount)
        {
            _itemProgressMap[itemId] = maxAmount;
        }

        SyncListFromMap();
    }

    public bool IsObjectiveCompleted()
    {
        if (QuestData == null) return false;

        switch (QuestData.ObjectiveType)
        {
            case EQuestObjectiveType.BreakObject:
            case EQuestObjectiveType.TalkToNpc:
                return CurrentAmount >= QuestData.RequiredAmount;

            case EQuestObjectiveType.CollectItem:
            case EQuestObjectiveType.DeliverItem:
                return AreAllItemRequirementsCompleted();

            default:
                return false;
        }
    }

    public bool AreAllItemRequirementsCompleted()
    {
        if (QuestData == null || QuestData.ItemRequirements == null || QuestData.ItemRequirements.Count == 0) return false;

        EnsureMap();

        foreach (var requirement in QuestData.ItemRequirements)
        {
            if (requirement.Item == null) return false;

            int itemId = requirement.ItemId;
            int current = GetItemProgress(itemId);

            if (current < requirement.Amount)
            {
                return false;
            }
        }

        return true;
    }

    private void EnsureMap()
    {
        if (_itemProgressMap != null) return;

        _itemProgressMap = new Dictionary<int, int>();

        foreach (var entry in _itemProgressList)
        {
            if (entry.ItemId < 0) continue;
            _itemProgressMap[entry.ItemId] = entry.CurrentAmount;
        }
    }

    private void SyncListFromMap()
    {
        _itemProgressList.Clear();

        foreach (var pair in _itemProgressMap)
        {
            _itemProgressList.Add(new QuestItemProgressEntry(pair.Key, pair.Value));
        }
    }
}
