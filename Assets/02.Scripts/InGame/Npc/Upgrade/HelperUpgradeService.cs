using System;
using System.Collections.Generic;
using UnityEngine;

public class HelperUpgradeService : MonoBehaviour
{
    [Header("참조 컴포넌트")]
    [SerializeField] private UI_HelperUpgrade _uiHelperUpgrade;
    [SerializeField] private NpcDialogueController _dialogueController;
    [SerializeField] private EvolutionManager _evolutionManager;

    private PlayerInventoryAbility _inventoryAbility;
    private PlayerHelperInventoryAbility _helperInventoryAbility;
    private NpcInteractionContext _currentContext;

    public event Action<HelperDataSO> OnHelperUpgraded;
    public event Action<HelperDataSO> OnUpgradeSucceeded;
    public event Action<HelperDataSO> OnUpgradeFailed;
    public event Action OnUpgradeUiCloseRequested;
    public event Action<List<HelperDataSO>> OnUpgradeUiOpened;

    private struct ConsumedItemRecord
    {
        public ItemDataSO Item;
        public int Amount;
    }

    private void Awake()
    {
        if (_uiHelperUpgrade == null)
        {
            _uiHelperUpgrade = FindFirstObjectByType<UI_HelperUpgrade>();
        }
        if (_dialogueController == null)
        {
            _dialogueController = FindFirstObjectByType<NpcDialogueController>();
        }
        if (_evolutionManager == null)
        {
            _evolutionManager = FindFirstObjectByType<EvolutionManager>();
        }
    }

    private void OnEnable()
    {
        if (_uiHelperUpgrade != null)
        {
            _uiHelperUpgrade.OnCloseRequested += CloseUpgradeUi;
            _uiHelperUpgrade.OnUpgradeRequested += TryUpgrade;
        }
    }

    private void OnDisable()
    {
        if (_uiHelperUpgrade != null)
        {
            _uiHelperUpgrade.OnCloseRequested -= CloseUpgradeUi;
            _uiHelperUpgrade.OnUpgradeRequested -= TryUpgrade;
        }
    }

    public void BeginUpgradeInteraction(NpcInteractionContext context)
    {
        _currentContext = context;
        PlayerController interactor = context?.Interactor;

        if (interactor == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("상호작용 플레이어가 없습니다.");
#endif
            return;
        }

        _helperInventoryAbility = interactor.GetAbility<PlayerHelperInventoryAbility>();
        _inventoryAbility = interactor.GetAbility<PlayerInventoryAbility>();
        if (_helperInventoryAbility == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("PlayerHelperInventoryAbility가 없습니다.");
#endif
            return;
        }
        if (_inventoryAbility == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("PlayerInventoryAbility가 없습니다.");
#endif
            return;
        }

        List<HelperDataSO> helpers = GetUpgradeableTargetList();
        if (helpers.Count == 0)
        {
#if UNITY_EDITOR
            Debug.Log("헬퍼가 없습니다.");
#endif
            return;
        }

        SortHelpers(helpers);

        _dialogueController?.Close();
        OnUpgradeUiOpened?.Invoke(helpers);
    }

    public List<HelperDataSO> GetUpgradeableTargetList()
    {
        List<HelperDataSO> result = new();

        if (_helperInventoryAbility == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("PlayerHelperInventoryAbility가 연결되지 않았습니다.");
#endif
            return result;
        }

        result.AddRange(_helperInventoryAbility.GetOwnedHelpers());
        return result;
    }

    public IReadOnlyList<HelperUpgradeCostEntry> GetUpgradeItemCosts(HelperDataSO data)
    {
        if (data == null) return Array.Empty<HelperUpgradeCostEntry>();

        EHelperGrade grade = GetGrade(data);
        return data.GetUpgradeCosts(grade);
    }

    public int GetUpgradeGoldCost(HelperDataSO data)
    {
        if (data == null) return 0;

        EHelperGrade grade = GetGrade(data);
        return data.GetUpgradeGoldCost(grade);
    }

    public int GetOwnedGold()
    {
        if (CurrencyManager.Instance == null) return 0;
        return CurrencyManager.Instance.CurrentGold;
    }

    public bool HasRequiredUpgradeItemCost(HelperDataSO data)
    {
        if (data == null || _inventoryAbility == null) return false;

        IReadOnlyList<HelperUpgradeCostEntry> costs = GetUpgradeItemCosts(data);
        for (int i = 0; i < costs.Count; i++)
        {
            HelperUpgradeCostEntry cost = costs[i];
            if (cost.Item == null || cost.Amount <= 0) continue;

            if (_inventoryAbility.GetItemCount(cost.Item) < cost.Amount) return false;
        }

        return true;
    }

    public bool HasRequiredGoldCost(HelperDataSO data)
    {
        if (data == null) return false;
        if (CurrencyManager.Instance == null) return false;

        int goldCost = GetUpgradeGoldCost(data);
        return CurrencyManager.Instance.CurrentGold >= goldCost;
    }

    public bool CanUpgrade(HelperDataSO data)
    {
        if (_helperInventoryAbility == null || _inventoryAbility == null || data == null) return false;

        EHelperGrade grade = _helperInventoryAbility.GetHelperGrade(data);
        if (grade == EHelperGrade.Legendary) return false;

        int exp = _helperInventoryAbility.GetHelperExperience(data);
        int maxExp = _helperInventoryAbility.GetMaxExpByGrade(data, grade);
        if (maxExp <= 0 || exp < maxExp) return false;

        if (!HasRequiredUpgradeItemCost(data)) return false;
        if (!HasRequiredGoldCost(data)) return false;

        return true;
    }

    public EHelperUpgradeBlockReason GetBlockReasonType(HelperDataSO data)
    {
        if (data == null) return EHelperUpgradeBlockReason.InvalidData;
        if (_helperInventoryAbility == null) return EHelperUpgradeBlockReason.InventoryNotReady;

        EHelperGrade grade = _helperInventoryAbility.GetHelperGrade(data);
        if (grade == EHelperGrade.Legendary) return EHelperUpgradeBlockReason.MaxGrade;

        int exp = _helperInventoryAbility.GetHelperExperience(data);
        int maxExp = _helperInventoryAbility.GetMaxExpByGrade(data, grade);
        if (maxExp <= 0 || exp < maxExp) return EHelperUpgradeBlockReason.NotEnoughExperience;

        if (!HasRequiredUpgradeItemCost(data)) return EHelperUpgradeBlockReason.NotEnoughItemCost;
        if (!HasRequiredGoldCost(data)) return EHelperUpgradeBlockReason.NotEnoughGoldCost;

        return EHelperUpgradeBlockReason.None;
    }

    public void TryUpgrade(HelperDataSO data)
    {
        if (data == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("업그레이드 대상 헬퍼가 없습니다.");
#endif
            return;
        }

        if (_helperInventoryAbility == null || _inventoryAbility == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("PlayerHelperInventoryAbility 또는 PlayerInventoryAbility가 없습니다.");
#endif
            return;
        }

        EHelperUpgradeBlockReason blockReason = GetBlockReasonType(data);
        if (blockReason != EHelperUpgradeBlockReason.None)
        {
            OnUpgradeFailed?.Invoke(data);
            return;
        }

        EHelperGrade currentGrade = _helperInventoryAbility.GetHelperGrade(data);
        if (TryPlayEvolutionCutscene(data, currentGrade)) return;

        TryFinalizeUpgrade(data);
    }

    private bool TryPlayEvolutionCutscene(HelperDataSO data, EHelperGrade currentGrade)
    {
        if (_evolutionManager == null || data == null || currentGrade >= EHelperGrade.Legendary) return false;

        HelperController liveHelper = FindLiveHelperForEvolution(data);
        bool started = _evolutionManager.BeginEvolution(data, currentGrade, liveHelper, completed
            =>
            {
                if (!completed)
                {
                    OnUpgradeFailed?.Invoke(data);
                    return;
                }

                TryFinalizeUpgrade(data);
            });

        if (!started) return false;

        CloseUpgradeUi();
        return true;
    }

    private HelperController FindLiveHelperForEvolution(HelperDataSO data)
    {
        if (_helperInventoryAbility == null || data == null) return null;

        HelperController activeMainHelper = _helperInventoryAbility.ActiveMainHelper;
        if (activeMainHelper != null && activeMainHelper.HelperId == data.HelperId) return activeMainHelper;

        return null;
    }
    private void TryFinalizeUpgrade(HelperDataSO data)
    {
        if (!TryConsumeUpgradeCost(data))
        {
            OnUpgradeFailed?.Invoke(data);
            return;
        }

        CompleteUpgrade(data);
    }

    private void CompleteUpgrade(HelperDataSO data)
    {
        bool success = _helperInventoryAbility.TryUpgradeHelper(data);
        if (!success)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"업그레이드 실패 - {data.HelperId}");
#endif
            OnUpgradeFailed?.Invoke(data);
            return;
        }

        _helperInventoryAbility.RespawnHelper(data);

#if UNITY_EDITOR
        Debug.Log($"업그레이드 완료 - {data.HelperId}, 현재 등급: {_helperInventoryAbility.GetHelperGrade(data)}");
#endif

        OnHelperUpgraded?.Invoke(data);
        OnUpgradeSucceeded?.Invoke(data);
    }

    public EHelperGrade GetGrade(HelperDataSO data)
    {
        if (_helperInventoryAbility == null || data == null) return EHelperGrade.Normal;

        return _helperInventoryAbility.GetHelperGrade(data);
    }

    public int GetExperience(HelperDataSO data)
    {
        if (_helperInventoryAbility == null || data == null) return 0;

        return _helperInventoryAbility.GetHelperExperience(data);
    }

    public int GetMaxExp(HelperDataSO data, EHelperGrade grade)
    {
        if (_helperInventoryAbility == null || data == null) return 0;

        return _helperInventoryAbility.GetMaxExpByGrade(data, grade);
    }

    public int GetRange(HelperDataSO data, EHelperGrade grade)
    {
        if (data == null) return 0;

        return grade switch
        {
            EHelperGrade.Normal => data.NormalRange,
            EHelperGrade.Epic => data.EpicRange,
            EHelperGrade.Legendary => data.LegendaryRange,
            _ => data.NormalRange
        };
    }

    private void SortHelpers(List<HelperDataSO> helpers)
    {
        helpers.Sort((a, b) =>
        {
            if (a == null) return 1;
            if (b == null) return -1;

            bool aReady = CanUpgrade(a);
            bool bReady = CanUpgrade(b);

            int readyCompare = bReady.CompareTo(aReady);
            if (readyCompare != 0) return readyCompare;

            float aRatio = GetExpRatio(a);
            float bRatio = GetExpRatio(b);

            int ratioCompare = bRatio.CompareTo(aRatio);
            if (ratioCompare != 0) return ratioCompare;

            return string.Compare(a.HelperId, b.HelperId, StringComparison.Ordinal);
        });
    }

    private float GetExpRatio(HelperDataSO data)
    {
        if (_helperInventoryAbility == null || data == null) return 0f;

        EHelperGrade grade = _helperInventoryAbility.GetHelperGrade(data);
        int maxExp = _helperInventoryAbility.GetMaxExpByGrade(data, grade);
        if (maxExp <= 0) return 0f;

        int currentExp = _helperInventoryAbility.GetHelperExperience(data);
        return (float)currentExp / maxExp;
    }

    private bool TryConsumeUpgradeCost(HelperDataSO data)
    {
        if (data == null || _inventoryAbility == null || CurrencyManager.Instance == null) return false;

        IReadOnlyList<HelperUpgradeCostEntry> costs = GetUpgradeItemCosts(data);
        int goldCost = GetUpgradeGoldCost(data);

        // 1. 아이템을 사전 검증합니다. 부족한 아이템이 있으면 바로 실패 처리합니다.
        for (int i = 0; i < costs.Count; i++)
        {
            HelperUpgradeCostEntry cost = costs[i];
            if (cost.Item == null || cost.Amount <= 0) continue;

            if (_inventoryAbility.GetItemCount(cost.Item) < cost.Amount) return false;
        }

        // 2. 골드를 사전 검증합니다. 부족한 골드가 있으면 바로 실패 처리합니다.
        if (CurrencyManager.Instance.CurrentGold < goldCost) return false;

        bool goldSpent = false;
        List<ConsumedItemRecord> consumedItems = new();

        try
        {
            // 3. 실패 가능성이 있는 골드를 먼저 차감합니다.
            if (goldCost > 0)
            {
                goldSpent = CurrencyManager.Instance.TrySpendGold(goldCost);
                if (!goldSpent) return false;
            }

            // 4. 아이템을 차감합니다.
            for (int i = 0; i < costs.Count; i++)
            {
                HelperUpgradeCostEntry cost = costs[i];
                if (cost.Item == null || cost.Amount <= 0) continue;

                bool removed = _inventoryAbility.RemoveItem(cost.Item, cost.Amount);
                if (!removed)
                {
                    RollbackConsumedCosts(consumedItems, goldSpent, goldCost);
                    return false;
                }

                consumedItems.Add(new ConsumedItemRecord
                {
                    Item = cost.Item,
                    Amount = cost.Amount
                });
            }

            return true;
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"업그레이드 비용 소모 중 예외 발생: {ex}");
#endif
            RollbackConsumedCosts(consumedItems, goldSpent, goldCost);
            return false;
        }
    }

    // 비용 소모 중에 실패가 발생했을 때, 이미 소모된 아이템과 골드를 롤백하는 메서드입니다.
    private void RollbackConsumedCosts(List<ConsumedItemRecord> consumedItems, bool goldSpent, int goldCost)
    {
        // 보기 편하게 아이템 롤백을 역순으로 처리합니다.
        for (int i = consumedItems.Count - 1; i >= 0; i--)
        {
            ConsumedItemRecord record = consumedItems[i];
            if (record.Item == null || record.Amount <= 0) continue;

            _inventoryAbility.AddItem(record.Item, record.Amount);
        }

        if (goldSpent && goldCost > 0 && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGold(goldCost);
        }
    }

    public int GetOwnedItemCostCount(ItemDataSO item)
    {
        if (_inventoryAbility == null || item == null) return 0;
        return _inventoryAbility.GetItemCount(item);
    }

    private void CloseUpgradeUi()
    {
        OnUpgradeUiCloseRequested?.Invoke();
        _currentContext?.InteractionComponent?.EndInteraction();
        _currentContext = null;
    }
}
