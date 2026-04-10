using System;
using System.Collections.Generic;
using UnityEngine;

public class HelperUpgradeService : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private UI_HelperUpgrade _uiHelperUpgrade;
    [SerializeField] private NpcDialogueController _dialogueController;
    [SerializeField] private EvolutionManager _evolutionManager;

    private PlayerHelperInventoryAbility _helperInventoryAbility;
    private NpcInteractionContext _currentContext;

    public event Action<HelperDataSO> OnHelperUpgraded;
    public event Action<HelperDataSO> OnUpgradeSucceeded;
    public event Action<HelperDataSO> OnUpgradeFailed;
    public event Action OnUpgradeUiCloseRequested;
    public event Action<List<HelperDataSO>> OnUpgradeUiOpened;

    private void Awake()
    {
        if (_uiHelperUpgrade == null)
            _uiHelperUpgrade = FindFirstObjectByType<UI_HelperUpgrade>();
        if (_dialogueController == null)
            _dialogueController = FindFirstObjectByType<NpcDialogueController>();
        if (_evolutionManager == null)
            _evolutionManager = FindFirstObjectByType<EvolutionManager>();
    }

    private void OnEnable()
    {
        PlayerHelperInventoryAbility.OnLocalPlayerReady += HandleLocalPlayerReady;

        if (_uiHelperUpgrade != null)
        {
            _uiHelperUpgrade.OnCloseRequested += CloseUpgradeUi;
            _uiHelperUpgrade.OnUpgradeRequested += TryUpgrade;
        }
    }

    private void OnDisable()
    {
        PlayerHelperInventoryAbility.OnLocalPlayerReady -= HandleLocalPlayerReady;

        if (_uiHelperUpgrade != null)
        {
            _uiHelperUpgrade.OnCloseRequested -= CloseUpgradeUi;
            _uiHelperUpgrade.OnUpgradeRequested -= TryUpgrade;
        }
    }

    private void HandleLocalPlayerReady(PlayerHelperInventoryAbility inventoryAbility)
    {
        _helperInventoryAbility = inventoryAbility;
    }

    public bool HasInventory()
    {
        return _helperInventoryAbility != null;
    }

    public void BeginUpgradeInteraction(NpcInteractionContext context)
    {
        _currentContext = context;

        if (_helperInventoryAbility == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("Local PlayerHelperInventoryAbility is not ready yet.");
#endif
            return;
        }

        List<HelperDataSO> helpers = GetUpgradeableTargetList();
        if (helpers.Count == 0)
        {
#if UNITY_EDITOR
            Debug.Log("No helpers available for upgrade UI.");
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
            Debug.LogWarning("PlayerHelperInventoryAbility is not connected.");
#endif
            return result;
        }

        result.AddRange(_helperInventoryAbility.GetOwnedHelpers());
        return result;
    }

    public bool CanUpgrade(HelperDataSO data)
    {
        if (_helperInventoryAbility == null || data == null)
            return false;

        EHelperGrade grade = _helperInventoryAbility.GetHelperGrade(data);
        if (grade == EHelperGrade.Legendary)
            return false;

        int exp = _helperInventoryAbility.GetHelperExperience(data);
        int maxExp = _helperInventoryAbility.GetMaxExpByGrade(data, grade);
        if (maxExp <= 0)
            return false;

        return exp >= maxExp;
    }

    public EHelperUpgradeBlockReason GetBlockReasonType(HelperDataSO data)
    {
        if (data == null) return EHelperUpgradeBlockReason.InvalidData;
        if (_helperInventoryAbility == null) return EHelperUpgradeBlockReason.InventoryNotReady;

        EHelperGrade grade = _helperInventoryAbility.GetHelperGrade(data);
        if (grade == EHelperGrade.Legendary) return EHelperUpgradeBlockReason.MaxGrade;
        if (!CanUpgrade(data)) return EHelperUpgradeBlockReason.NotEnoughExperience;

        return EHelperUpgradeBlockReason.None;
    }

    public void TryUpgrade(HelperDataSO data)
    {
        if (data == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("Upgrade target helper is null.");
#endif
            return;
        }

        if (_helperInventoryAbility == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("PlayerHelperInventoryAbility is missing.");
#endif
            return;
        }

        if (!CanUpgrade(data))
        {
#if UNITY_EDITOR
            Debug.LogWarning("Upgrade blocked.");
#endif
            OnUpgradeFailed?.Invoke(data);
            return;
        }

        EHelperGrade currentGrade = _helperInventoryAbility.GetHelperGrade(data);
        if (TryPlayEvolutionCutscene(data, currentGrade))
            return;

        CompleteUpgrade(data);
    }

    private bool TryPlayEvolutionCutscene(HelperDataSO data, EHelperGrade currentGrade)
    {
        if (_evolutionManager == null || data == null)
            return false;
        if (currentGrade >= EHelperGrade.Legendary)
            return false;

        HelperController liveHelper = FindLiveHelperForEvolution(data);
        bool started = _evolutionManager.BeginEvolution(
            data,
            currentGrade,
            liveHelper,
            completed =>
            {
                if (!completed)
                {
                    OnUpgradeFailed?.Invoke(data);
                    return;
                }

                CompleteUpgrade(data);
            });

        if (!started)
            return false;

        CloseUpgradeUi();
        return true;
    }

    private HelperController FindLiveHelperForEvolution(HelperDataSO data)
    {
        if (_helperInventoryAbility == null || data == null)
            return null;

        HelperController activeMainHelper = _helperInventoryAbility.ActiveMainHelper;
        if (activeMainHelper != null && activeMainHelper.HelperId == data.HelperId)
            return activeMainHelper;

        return null;
    }

    private void CompleteUpgrade(HelperDataSO data)
    {
        bool success = _helperInventoryAbility.TryUpgradeHelper(data);
        if (!success)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"Upgrade failed - {data.HelperId}");
#endif
            OnUpgradeFailed?.Invoke(data);
            return;
        }

        _helperInventoryAbility.RespawnHelper(data);

#if UNITY_EDITOR
        Debug.Log($"Upgrade complete - {data.HelperId}, current grade: {_helperInventoryAbility.GetHelperGrade(data)}");
#endif

        OnHelperUpgraded?.Invoke(data);
        OnUpgradeSucceeded?.Invoke(data);
    }

    public EHelperGrade GetGrade(HelperDataSO data)
    {
        if (_helperInventoryAbility == null || data == null)
            return EHelperGrade.Normal;

        return _helperInventoryAbility.GetHelperGrade(data);
    }

    public int GetExperience(HelperDataSO data)
    {
        if (_helperInventoryAbility == null || data == null)
            return 0;

        return _helperInventoryAbility.GetHelperExperience(data);
    }

    public int GetMaxExp(HelperDataSO data, EHelperGrade grade)
    {
        if (_helperInventoryAbility == null || data == null)
            return 0;

        return _helperInventoryAbility.GetMaxExpByGrade(data, grade);
    }

    public int GetRange(HelperDataSO data, EHelperGrade grade)
    {
        if (data == null)
            return 0;

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
        if (_helperInventoryAbility == null || data == null)
            return 0f;

        EHelperGrade grade = _helperInventoryAbility.GetHelperGrade(data);
        int maxExp = _helperInventoryAbility.GetMaxExpByGrade(data, grade);
        if (maxExp <= 0)
            return 0f;

        int currentExp = _helperInventoryAbility.GetHelperExperience(data);
        return (float)currentExp / maxExp;
    }

    private void CloseUpgradeUi()
    {
        OnUpgradeUiCloseRequested?.Invoke();
        _currentContext?.InteractionComponent?.EndInteraction();
        _currentContext = null;
    }
}
