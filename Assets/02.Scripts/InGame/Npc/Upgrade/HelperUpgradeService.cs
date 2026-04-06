using UnityEngine;
using System;
using System.Collections.Generic;

public class HelperUpgradeService : MonoBehaviour
{
    [Header("컴포넌트 참조")]
    [SerializeField] private PlayerHelperInteractionAbility _playerHelperInteraction;
    [SerializeField] private UI_HelperUpgrade _uiHelperUpgrade;
    [SerializeField] private NpcDialogueController _dialogueController;

    private NpcInteractionContext _currentContext;

    public event Action<HelperController> OnHelperUpgraded;

    private void Awake()
    {
        if (_playerHelperInteraction == null)
        {
            _playerHelperInteraction = FindFirstObjectByType<PlayerHelperInteractionAbility>();
        }
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
        if (_uiHelperUpgrade != null)
        {
            _uiHelperUpgrade.OnUpgradeRequested += TryUpgrade;
            _uiHelperUpgrade.OnCloseRequested += CloseUpgradeUi;
        }
    }

    private void OnDisable()
    {
        if (_uiHelperUpgrade != null)
        {
            _uiHelperUpgrade.OnUpgradeRequested -= TryUpgrade;
            _uiHelperUpgrade.OnCloseRequested -= CloseUpgradeUi;
        }
    }

    public void BeginUpgradeInteraction(NpcInteractionContext context)
    {
        _currentContext = context;

        List<HelperController> helpers = GetUpgradeableTargetList();
        if (helpers.Count == 0)
        {
#if UNITY_EDITOR
            Debug.Log("업그레이드 표시할 helper가 없습니다.");
#endif
            return;
        }

        _dialogueController?.Close();
        _uiHelperUpgrade?.Open(helpers);
    }

    public List<HelperController> GetUpgradeableTargetList()
    {
        List<HelperController> result = new();

        if (_playerHelperInteraction == null) return result;

        if (_playerHelperInteraction.CurrentHelper != null)
        {
            result.Add(_playerHelperInteraction.CurrentHelper);
        }

        if (_playerHelperInteraction.BackHelper != null &&
            _playerHelperInteraction.BackHelper != _playerHelperInteraction.CurrentHelper)
        {
            result.Add(_playerHelperInteraction.BackHelper);
        }

        SortHelpers(result);
        return result;
    }

    public bool CanUpgrade(HelperController helper)
    {
        if (helper == null) return false;
        if (helper.Experience == null) return false;
        if (helper.Grade == null) return false;

        return helper.Experience.IsReadyToUpgrade;
    }

    public string GetBlockReason(HelperController helper)
    {
        if (helper == null) return "helper 정보가 없습니다.";

        if (helper.Grade == null || helper.Experience == null) return "helper 상태 정보를 확인할 수 없습니다.";

        if (helper.Grade.CurrentGrade == EHelperGrade.Legendary) return "이미 최고 등급입니다.";

        if (!helper.Experience.IsReadyToUpgrade) return "경험치가 부족합니다.";

        return string.Empty;
    }

    public void TryUpgrade(HelperController helper)
    {
        if (helper == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("곡룡 업그레이드 대상이 없습니다.");
#endif
            return;
        }

        if (!CanUpgrade(helper))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"업그레이드 불가 - {GetBlockReason(helper)}");
#endif
            _uiHelperUpgrade?.RefreshSelectedHelperDetail();
            _uiHelperUpgrade?.RefreshSlots();
            return;
        }

        helper.PerformUpgrade();

#if UNITY_EDITOR
        Debug.Log($"업그레이드 완료 - {helper.HelperId}, 현재 등급: {helper.Grade.CurrentGrade}");
#endif

        OnHelperUpgraded?.Invoke(helper);
        _uiHelperUpgrade?.RefreshAfterUpgrade(helper);
    }

    private void SortHelpers(List<HelperController> helpers)
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

    private float GetExpRatio(HelperController helper)
    {
        if (helper == null || helper.Experience == null) return 0f;
        if (helper.Experience.MaxExp <= 0) return 0f;

        return (float)helper.Experience.CurrentExp / helper.Experience.MaxExp;
    }

    private void CloseUpgradeUi()
    {
        _uiHelperUpgrade?.Close();
        _currentContext?.InteractionComponent?.EndInteraction();
        _currentContext = null;
    }
}
