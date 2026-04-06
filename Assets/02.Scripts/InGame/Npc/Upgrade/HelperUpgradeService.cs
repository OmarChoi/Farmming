using UnityEngine;
using System;
using System.Collections.Generic;

public class HelperUpgradeService : MonoBehaviour
{
    [Header("컴포넌트 참조")]
    [SerializeField] private UI_HelperUpgrade _uiHelperUpgrade;
    [SerializeField] private NpcDialogueController _dialogueController;

    private PlayerHelperInventoryAbility _helperInventoryAbility;
    private NpcInteractionContext _currentContext;

    public event Action<HelperDataSO> OnHelperUpgraded;

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
            Debug.LogWarning("아직 로컬 플레이어 인벤토리가 준비되지 않았습니다.");
#endif
            return;
        }

        List<HelperDataSO> helpers = GetUpgradeableTargetList();
        if (helpers.Count == 0)
        {
#if UNITY_EDITOR
            Debug.Log("업그레이드 표시할 helper가 없습니다.");
#endif
            return;
        }

        SortHelpers(helpers);

        _dialogueController?.Close();
        _uiHelperUpgrade?.OpenUpgradeUi(helpers);
    }

    public List<HelperDataSO> GetUpgradeableTargetList()
    {
        List<HelperDataSO> result = new();

        if (_helperInventoryAbility == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("PlayerHelperInventoryAbility가 아직 연결되지 않았습니다.");
#endif
            return result;
        }

        result.AddRange(_helperInventoryAbility.GetOwnedHelpers());
        return result;
    }

    public bool CanUpgrade(HelperDataSO data)
    {
        if (_helperInventoryAbility == null || data == null) return false;

        EHelperGrade grade = _helperInventoryAbility.GetHelperGrade(data);
        if (grade == EHelperGrade.Legendary) return false;

        int exp = _helperInventoryAbility.GetHelperExperience(data);
        int maxExp = _helperInventoryAbility.GetMaxExpByGrade(data, grade);

        if (maxExp <= 0) return false;

        return exp >= maxExp;
    }

    public string GetBlockReason(HelperDataSO data)
    {
        if (data == null) return "helper 정보가 없습니다.";

        if (_helperInventoryAbility == null) return "helper 인벤토리 정보를 확인할 수 없습니다.";

        EHelperGrade grade = _helperInventoryAbility.GetHelperGrade(data);

        if (grade == EHelperGrade.Legendary) return "이미 최고 등급입니다.";

        if (!CanUpgrade(data)) return "경험치가 부족합니다.";

        return string.Empty;
    }

    public void TryUpgrade(HelperDataSO data)
    {
        if (data == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("업그레이드 대상 helper가 없습니다.");
#endif
            return;
        }

        if (_helperInventoryAbility == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("_helperInventoryAbility가 없습니다.");
#endif
            return;
        }

        if (!CanUpgrade(data))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"업그레이드 불가 - {GetBlockReason(data)}");
#endif
            _uiHelperUpgrade?.RefreshSelectedHelperDetail();
            _uiHelperUpgrade?.RefreshSlots();
            return;
        }

        bool success = _helperInventoryAbility.TryUpgradeHelper(data);
        if (!success)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"업그레이드 실패 - {data.HelperId}");
#endif
            _uiHelperUpgrade?.RefreshSelectedHelperDetail();
            _uiHelperUpgrade?.RefreshSlots();
            return;
        }

#if UNITY_EDITOR
        Debug.Log($"업그레이드 완료 - {data.HelperId}, 현재 등급: {_helperInventoryAbility.GetHelperGrade(data)}");
#endif

        OnHelperUpgraded?.Invoke(data);
        _uiHelperUpgrade?.RefreshAfterUpgrade(data);
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

        switch (grade)
        {
            case EHelperGrade.Normal:
                return data.NormalRange;
            case EHelperGrade.Epic:
                return data.EpicRange;
            case EHelperGrade.Legendary:
                return data.LegendaryRange;
            default:
                return data.NormalRange;
        }
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

    private void CloseUpgradeUi()
    {
        _uiHelperUpgrade?.CloseUpgradeUi();
        _currentContext?.InteractionComponent?.EndInteraction();
        _currentContext = null;
    }
}
