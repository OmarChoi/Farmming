using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class UI_HelperUpgrade : MonoBehaviour
{
    [Header("필요 컴포넌트")]
    [SerializeField] private HelperUpgradeService _helperUpgradeService;
    [SerializeField] private GameObject _helperUpgradeRoot;
    [SerializeField] private Transform _slotParent;
    [SerializeField] private UI_HelperUpgradeSlot _slotPrefab;

    [Header("플레이어 컨트롤러")]
    [SerializeField] private PlayerController _playerController;

    [Header("상세 패널 텍스트")]
    [SerializeField] private TextMeshProUGUI _helperNameText;
    [SerializeField] private TextMeshProUGUI _helperGradeText;
    [SerializeField] private TextMeshProUGUI _helperExpText;
    [SerializeField] private TextMeshProUGUI _helperRangeText;
    [SerializeField] private TextMeshProUGUI _helperMessageText;

    [Header("버튼")]
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private Button _closeButton;

    [Header("팝업 트윈")]
    [SerializeField] private UI_PopupDoTween _popupDoTween;

    private readonly List<UI_HelperUpgradeSlot> _slots = new();
    private readonly List<HelperDataSO> _currentHelpers = new();

    private int _selectedIndex = -1;

    public event Action<HelperDataSO> OnUpgradeRequested;
    public event Action OnCloseRequested;

    private void Awake()
    {
        if (_helperUpgradeService == null)
        {
            _helperUpgradeService = FindFirstObjectByType<HelperUpgradeService>();
        }

        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.RemoveAllListeners();
            _upgradeButton.onClick.AddListener(HandleClickUpgrade);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(HandleClickClose);
        }
    }

    private void OnEnable()
    {
        if (_helperUpgradeService != null)
        {
            _helperUpgradeService.OnUpgradeUiOpened += HandleOpenRequested;
            _helperUpgradeService.OnUpgradeSucceeded += HandleUpgradeSucceeded;
            _helperUpgradeService.OnUpgradeFailed += HandleUpgradeFailed;
            _helperUpgradeService.OnUpgradeUiCloseRequested += CloseUpgradeUi;
        }
    }

    private void OnDisable()
    {
        if (_helperUpgradeService != null)
        {
            _helperUpgradeService.OnUpgradeUiOpened -= HandleOpenRequested;
            _helperUpgradeService.OnUpgradeSucceeded -= HandleUpgradeSucceeded;
            _helperUpgradeService.OnUpgradeFailed -= HandleUpgradeFailed;
            _helperUpgradeService.OnUpgradeUiCloseRequested -= CloseUpgradeUi;
        }
    }

    private void HandleOpenRequested(List<HelperDataSO> helpers)
    {
        OpenUpgradeUi(helpers);
    }

    private void HandleUpgradeSucceeded(HelperDataSO data)
    {
        RefreshAfterUpgrade(data);
    }

    private void HandleUpgradeFailed(HelperDataSO data)
    {
        RefreshSelectedHelperDetail();
        RefreshSlots();
    }

    public async void OpenUpgradeUi(List<HelperDataSO> helpers)
    {
        await OpenAsync(helpers);

        if (_playerController != null)
        {
            _playerController.SetCursorLock(false);
        }
    }

    public async UniTask OpenAsync(List<HelperDataSO> helpers)
    {
        BindHelperList(helpers);
        CreateOrRefreshSlots();

        if (_currentHelpers.Count > 0)
        {
            SelectSlot(0);
        }
        else
        {
            ClearDetail();
        }

        if (_popupDoTween != null)
        {
            await _popupDoTween.PlayOpenAsync();
        }
        else if (_helperUpgradeRoot != null)
        {
            _helperUpgradeRoot.SetActive(true);
        }
    }

    public async void CloseUpgradeUi()
    {
        await CloseAsync();

        if (_playerController != null)
        {
            _playerController.SetCursorLock(true);
        }
    }

    public async UniTask CloseAsync()
    {
        if (_popupDoTween != null)
        {
            await _popupDoTween.PlayCloseAsync();
        }
        else if (_helperUpgradeRoot != null)
        {
            _helperUpgradeRoot.SetActive(false);
        }

        _currentHelpers.Clear();
        _selectedIndex = -1;
    }

    public void RefreshAfterUpgrade(HelperDataSO upgradedHelper)
    {
        RefreshHelperListFromService();
        CreateOrRefreshSlots();

        if (_currentHelpers.Count == 0)
        {
            ClearDetail();
            return;
        }

        int nextIndex = FindHelperIndex(upgradedHelper);
        if (nextIndex < 0)
        {
            nextIndex = Mathf.Clamp(_selectedIndex, 0, _currentHelpers.Count - 1);
        }

        SelectSlot(nextIndex);
    }

    public void RefreshSlots()
    {
        CreateOrRefreshSlots();
        RefreshSelectionVisual();
    }

    public void RefreshSelectedHelperDetail()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _currentHelpers.Count)
        {
            ClearDetail();
            return;
        }

        BindDetail(_currentHelpers[_selectedIndex]);
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= _currentHelpers.Count)
            return;

        _selectedIndex = index;
        RefreshSelectionVisual();
        BindDetail(_currentHelpers[_selectedIndex]);
    }

    public EHelperGrade GetGrade(HelperDataSO data)
    {
        if (_helperUpgradeService == null) return EHelperGrade.Normal;

        return _helperUpgradeService.GetGrade(data);
    }

    public bool CanUpgrade(HelperDataSO data)
    {
        if (_helperUpgradeService == null) return false;

        return _helperUpgradeService.CanUpgrade(data);
    }

    private void BindHelperList(List<HelperDataSO> helpers)
    {
        _currentHelpers.Clear();

        if (helpers == null) return;

        _currentHelpers.AddRange(helpers);
    }

    private void RefreshHelperListFromService()
    {
        _currentHelpers.Clear();

        if (_helperUpgradeService == null) return;

        _currentHelpers.AddRange(_helperUpgradeService.GetUpgradeableTargetList());
    }

    private int FindHelperIndex(HelperDataSO target)
    {
        if (target == null) return -1;

        for (int i = 0; i < _currentHelpers.Count; i++)
        {
            if (_currentHelpers[i] == target) return i;
        }

        return -1;
    }

    private void CreateOrRefreshSlots()
    {
        int slotCount = _currentHelpers.Count;

        while (_slots.Count < slotCount)
        {
            UI_HelperUpgradeSlot newSlot = Instantiate(_slotPrefab, _slotParent);
            newSlot.Init(this, _slots.Count);
            _slots.Add(newSlot);
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            bool isActive = i < slotCount;
            _slots[i].gameObject.SetActive(isActive);

            if (isActive)
            {
                bool isSelected = i == _selectedIndex;
                _slots[i].Refresh(_currentHelpers[i], isSelected);
            }
        }
    }

    private void RefreshSelectionVisual()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (!_slots[i].gameObject.activeSelf) continue;
            _slots[i].SetSelected(i == _selectedIndex);
        }
    }

    private void BindDetail(HelperDataSO data)
    {
        if (data == null || _helperUpgradeService == null)
        {
            ClearDetail();
            return;
        }

        EHelperGrade grade = _helperUpgradeService.GetGrade(data);
        int exp = _helperUpgradeService.GetExperience(data);
        int maxExp = _helperUpgradeService.GetMaxExp(data, grade);
        bool canUpgrade = _helperUpgradeService.CanUpgrade(data);

        _helperNameText.text = HelperUpgradeTextFormatter.GetHelperName(data);
        _helperGradeText.text = HelperUpgradeTextFormatter.GetGradeText(grade);
        _helperExpText.text = HelperUpgradeTextFormatter.GetExpText(grade, exp, maxExp);
        _helperRangeText.text = HelperUpgradeTextFormatter.GetRangeText(data, grade, canUpgrade);

        if (_upgradeButton != null)
        {
            _upgradeButton.interactable = canUpgrade;
        }

        EHelperUpgradeBlockReason blockReason = _helperUpgradeService.GetBlockReasonType(data);
        _helperMessageText.text = HelperUpgradeTextFormatter.GetUpgradeMessage(
            canUpgrade,
            HelperUpgradeTextFormatter.GetBlockReasonText(blockReason)
        );
    }

    private void ClearDetail()
    {
        _helperNameText.text = "";
        _helperGradeText.text = "";
        _helperExpText.text = "";
        _helperRangeText.text = "";
        _helperMessageText.text = "표시할 helper가 없습니다.";

        if (_upgradeButton != null)
        {
            _upgradeButton.interactable = false;
        }
    }

    private void HandleClickUpgrade()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _currentHelpers.Count) return;

        OnUpgradeRequested?.Invoke(_currentHelpers[_selectedIndex]);
    }

    private void HandleClickClose()
    {
        OnCloseRequested?.Invoke();
    }
}
