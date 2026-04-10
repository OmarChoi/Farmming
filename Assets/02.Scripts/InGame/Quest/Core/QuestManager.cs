using UnityEngine;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class QuestManager : MonoBehaviour, IQuestProgressService
{
    public static QuestManager Instance { get; private set; }

    [SerializeField] private QuestDatabase _questDatabase;

    public QuestDatabase QuestDatabase => _questDatabase;

    private QuestRewardService _rewardService;
    private QuestRequirementService _requirementService;

    private PlayerInventoryAbility _playerInventory;

    private readonly Dictionary<string, QuestRuntimeData> _activeQuests = new();
    private readonly HashSet<string> _completedMainQuestIds = new();
    private readonly HashSet<string> _completedSubQuestIds = new();

    public IReadOnlyDictionary<string, QuestRuntimeData> ActiveQuests => _activeQuests;
    public bool IsLoaded { get; private set; }
    public static event Action OnQuestDataLoaded;

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
        OnQuestManagerReady?.Invoke();
    }

    private void OnEnable()
    {
        PlayerInventoryAbility.OnLocalPlayerReady += OnPlayerReady;
        GatheringObject.OnGatheringCompleted += HandleGatheringCompleted;
        FarmTileStateMachine.OnTileBecameDry += HandleTileBecameDry;
        FarmTile.OnSeedPlanted += HandleSeedPlanted;
        FarmTileStateMachine.OnTileBecameWet += HandleTileBecameWet;
    }

    private void OnDisable()
    {
        PlayerInventoryAbility.OnLocalPlayerReady -= OnPlayerReady;
        GatheringObject.OnGatheringCompleted -= HandleGatheringCompleted;
        FarmTileStateMachine.OnTileBecameDry -= HandleTileBecameDry;
        FarmTile.OnSeedPlanted -= HandleSeedPlanted;
        FarmTileStateMachine.OnTileBecameWet -= HandleTileBecameWet;
    }

    public QuestSaveData ExportSaveData()
    {
        QuestSaveData saveData = new QuestSaveData();

        foreach (QuestRuntimeData quest in _activeQuests.Values)
        {
            if (quest == null || quest.QuestData == null) continue;

            QuestRuntimeSaveData runtimeSave = new QuestRuntimeSaveData
            {
                QuestId = quest.QuestData.QuestId,
                Status = (int)quest.Status,
                CurrentAmount = quest.CurrentAmount,
                ItemProgressList = quest.GetItemProgressSaveList()
            };

            saveData.ActiveQuests.Add(runtimeSave);
        }

        saveData.CompletedMainQuestIds.AddRange(_completedMainQuestIds);
        saveData.CompletedSubQuestIds.AddRange(_completedSubQuestIds);

        return saveData;
    }

    public void ImportSaveData(QuestSaveData saveData)
    {
        _activeQuests.Clear();
        _completedMainQuestIds.Clear();
        _completedSubQuestIds.Clear();

        if (saveData == null)
        {
            InitializeEmpty();
            return;
        }
        if (_questDatabase == null)
        {
            Debug.LogError("QuestDatabase가 없어 퀘스트 데이터를 복원할 수 없습니다.");
            MarkLoaded();
            return;
        }

        if (saveData.CompletedMainQuestIds != null)
        {
            foreach (string questId in saveData.CompletedMainQuestIds)
            {
                if (string.IsNullOrEmpty(questId)) continue;
                _completedMainQuestIds.Add(questId);
            }
        }

        if (saveData.CompletedSubQuestIds != null)
        {
            foreach (string questId in saveData.CompletedSubQuestIds)
            {
                if (string.IsNullOrEmpty(questId)) continue;
                _completedSubQuestIds.Add(questId);
            }
        }

        if (saveData.ActiveQuests != null)
        {
            foreach (QuestRuntimeSaveData runtimeSave in saveData.ActiveQuests)
            {
                if (runtimeSave == null || string.IsNullOrEmpty(runtimeSave.QuestId)) continue;

                QuestDataSO questData = _questDatabase.GetQuestById(runtimeSave.QuestId);
                if (questData == null) continue;

                QuestRuntimeData runtimeData = new QuestRuntimeData(questData);
                runtimeData.Status = (EQuestStatus)runtimeSave.Status;
                runtimeData.CurrentAmount = runtimeSave.CurrentAmount;
                runtimeData.RestoreItemProgress(runtimeSave.ItemProgressList);

                _activeQuests[runtimeSave.QuestId] = runtimeData;
            }
        }

        MarkLoaded();
    }

    public void MarkLoaded()
    {
        IsLoaded = true;
        OnQuestDataLoaded?.Invoke();
    }

    private void OnPlayerReady(PlayerInventoryAbility ability)
    {
#if UNITY_EDITOR
        Debug.Log("PlayerInventoryAbility 확인");
#endif
        _playerInventory = ability;
        _rewardService = new QuestRewardService(_playerInventory);
        _requirementService = new QuestRequirementService(_playerInventory);
    }

    private void HandleGatheringCompleted(GatheringObject obj)
    {
        if (obj == null || obj.GatheringData == null) return;
        ReportObjectBroken(obj.GatheringData.ObjectName);
    }

    private void HandleTileBecameDry(FarmTile tile)
    {
        ReportFarmDried();
    }

    private void HandleSeedPlanted(FarmTile tile, SeedItemDataSO seed)
    {
        if (seed == null) return;
        ReportSeedPlanted(seed.DisplayName);
    }

    private void HandleTileBecameWet(FarmTile tile)
    {
        ReportFarmWatered();
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

        InitializeQuestProgressOnAccept(runtimeData);

        _activeQuests.Add(questData.QuestId, runtimeData);

        OnQuestAccepted?.Invoke(runtimeData);
        OnQuestUpdated?.Invoke(runtimeData);
        return true;
    }

    private void InitializeQuestProgressOnAccept(QuestRuntimeData quest)
    {
        if (quest == null || quest.QuestData == null) return;

        QuestDataSO data = quest.QuestData;

        switch (data.ObjectiveType)
        {
            case EQuestObjectiveType.CollectItem:
                InitializeCollectItemProgress(quest);
                break;

            case EQuestObjectiveType.DeliverItem:
                InitializeDeliverItemProgress(quest);
                break;
        }

        if (quest.IsObjectiveCompleted() && data.ObjectiveType == EQuestObjectiveType.CollectItem)
        {
            quest.Status = EQuestStatus.CanComplete;
        }
    }

    private void InitializeCollectItemProgress(QuestRuntimeData quest)
    {
        if (quest == null || quest.QuestData == null) return;

        foreach (QuestItemRequirementEntry requirement in quest.QuestData.ItemRequirements)
        {
            if (requirement.Item == null) continue;

            quest.SetItemProgress(requirement.ItemId, 0);
        }
    }

    private void InitializeDeliverItemProgress(QuestRuntimeData quest)
    {
        if (quest == null || quest.QuestData == null || _requirementService == null) return;

        foreach (QuestItemRequirementEntry requirement in quest.QuestData.ItemRequirements)
        {
            if (requirement.Item == null) continue;

            int ownedCount = _requirementService.GetOwnedItemCount(requirement.Item);
            int progress = Mathf.Min(ownedCount, requirement.Amount);

            quest.SetItemProgress(requirement.ItemId, progress);
        }
    }

    public void ReportObjectBroken(string objectId, int amount = 1)
    {
        if (string.IsNullOrEmpty(objectId)) return;
        TryAddSimpleProgress(EQuestObjectiveType.BreakObject, objectId, amount);
    }

    public void ReportItemCollected(int itemId, int amount = 1)
    {
        if (itemId < 0 || amount <= 0) return;
        TryAddItemProgress(itemId, amount);
    }

    public void ReportNpcTalked(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return;
        TryAddSimpleProgress(EQuestObjectiveType.TalkToNpc, npcId, 1);
    }

    public void ReportFarmDried(int amount = 1)
    {
        if (amount <= 0) return;
        TryAddSimpleProgress(EQuestObjectiveType.DryFarmTile, amount);
    }

    public void ReportSeedPlanted(string seedId, int amount = 1)
    {
        if (string.IsNullOrEmpty(seedId)) return;
        TryAddSimpleProgress(EQuestObjectiveType.PlantSeed, seedId, amount);
    }

    public void ReportFarmWatered(int amount = 1)
    {
        if (amount <= 0) return;
        TryAddSimpleProgress(EQuestObjectiveType.WaterFarmTile, amount);
    }

    public bool TryDeliverItemToNpc(string questId, string npcId)
    {
        if (string.IsNullOrEmpty(questId) || string.IsNullOrEmpty(npcId) || _requirementService == null) return false;

        if (!_activeQuests.TryGetValue(questId, out var quest)) return false;
        if (quest.Status != EQuestStatus.InProgress) return false;

        QuestDataSO questData = quest.QuestData;

        if (questData.ObjectiveType != EQuestObjectiveType.DeliverItem) return false;
        if (questData.TargetNpcId != npcId) return false;
        if (!HasValidItemRequirements(questData)) return false;

        if (!quest.AreAllItemRequirementsCompleted()) return false;
        if (!_requirementService.TryConsumeRequirements(questData.ItemRequirements)) return false;

        quest.Status = EQuestStatus.CanComplete;

        OnQuestUpdated?.Invoke(quest);
        return true;
    }

    private void TryAddSimpleProgress(EQuestObjectiveType objectiveType, string targetId, int amount)
    {
        if (_activeQuests.Count == 0 || amount <= 0 || string.IsNullOrEmpty(targetId)) return;

        foreach (QuestRuntimeData quest in _activeQuests.Values)
        {
            if (quest == null || quest.QuestData == null) continue;
            if (quest.Status != EQuestStatus.InProgress) continue;

            QuestDataSO questData = quest.QuestData;

            if (questData.ObjectiveType != objectiveType) continue;
            if (!IsSimpleTargetMatched(questData, objectiveType, targetId)) continue;

            quest.CurrentAmount += amount;

            if (quest.CurrentAmount >= questData.RequiredAmount)
            {
                quest.CurrentAmount = questData.RequiredAmount;
                quest.Status = EQuestStatus.CanComplete;
            }

            OnQuestUpdated?.Invoke(quest);
        }
    }

    private void TryAddSimpleProgress(EQuestObjectiveType objectiveType, int amount)
    {
        if (_activeQuests.Count == 0 || amount <= 0) return;

        foreach (QuestRuntimeData quest in _activeQuests.Values)
        {
            if (quest == null || quest.QuestData == null) continue;
            if (quest.Status != EQuestStatus.InProgress) continue;

            QuestDataSO questData = quest.QuestData;

            if (questData.ObjectiveType != objectiveType) continue;

            quest.CurrentAmount += amount;

            if (quest.CurrentAmount >= questData.RequiredAmount)
            {
                quest.CurrentAmount = questData.RequiredAmount;
                quest.Status = EQuestStatus.CanComplete;
            }

            OnQuestUpdated?.Invoke(quest);
        }
    }

    private void TryAddItemProgress(int itemId, int amount)
    {
        if (_activeQuests.Count == 0) return;
        if (itemId < 0 || amount <= 0) return;

        foreach (QuestRuntimeData quest in _activeQuests.Values)
        {
            if (quest == null || quest.QuestData == null) continue;
            if (quest.Status != EQuestStatus.InProgress) continue;

            QuestDataSO questData = quest.QuestData;

            if (questData.ObjectiveType != EQuestObjectiveType.CollectItem &&
                questData.ObjectiveType != EQuestObjectiveType.DeliverItem)
            {
                continue;
            }
            if (!HasValidItemRequirements(questData)) continue;
            if (!TryGetRequirementAmount(questData, itemId, out int requiredAmount)) continue;

            quest.AddItemProgress(itemId, amount, requiredAmount);

            if (quest.IsObjectiveCompleted() && questData.ObjectiveType == EQuestObjectiveType.CollectItem)
            {
                quest.Status = EQuestStatus.CanComplete;
            }

            OnQuestUpdated?.Invoke(quest);
        }
    }

    private bool IsSimpleTargetMatched(QuestDataSO questData, EQuestObjectiveType objectiveType, string targetId)
    {
        switch (objectiveType)
        {
            case EQuestObjectiveType.BreakObject:
                return questData.TargetId == targetId;

            case EQuestObjectiveType.TalkToNpc:
                return questData.TargetNpcId == targetId;

            case EQuestObjectiveType.PlantSeed:
                return questData.TargetId == targetId;

            default:
                return false;
        }
    }

    private bool HasValidItemRequirements(QuestDataSO questData)
    {
        return questData != null && questData.ItemRequirements != null && questData.ItemRequirements.Count > 0;
    }

    private bool TryGetRequirementAmount(QuestDataSO questData, int itemId, out int requiredAmount)
    {
        requiredAmount = 0;

        if (!HasValidItemRequirements(questData) || itemId < 0) return false;

        foreach (QuestItemRequirementEntry requirement in questData.ItemRequirements)
        {
            if (requirement.Item == null) continue;
            if (requirement.ItemId != itemId) continue;

            requiredAmount = requirement.Amount;
            return true;
        }

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

        _rewardService.GiveReward(quest.QuestData.Reward);
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
        SaveManager.Instance.SaveAsync().Forget();
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

    // 선행 퀘스트 확인용 메서드입니다.
    public bool IsQuestCompleted(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return false;

        if (_completedMainQuestIds.Contains(questId)) return true;
        if (_completedSubQuestIds.Contains(questId)) return true;

        return false;
    }

    public void InitializeEmpty()
    {
        _activeQuests.Clear();
        _completedMainQuestIds.Clear();
        _completedSubQuestIds.Clear();
        MarkLoaded();
    }
}
