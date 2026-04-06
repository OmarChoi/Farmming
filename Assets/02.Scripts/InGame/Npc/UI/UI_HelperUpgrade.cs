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
    private readonly List<HelperController> _currentHelpers = new();

    private int _selectedIndex = -1;

    public event Action<HelperController> OnUpgradeRequested;
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

    public bool IsOpen()
    {
        return _helperUpgradeRoot != null && _helperUpgradeRoot.activeSelf;
    }

    public async UniTask OpenAsync(List<HelperController> helpers)
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

    public void Open(List<HelperController> helpers)
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

        if (_helperUpgradeRoot != null)
        {
            _helperUpgradeRoot.SetActive(true);
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

    public void Close()
    {
        if (_helperUpgradeRoot != null)
        {
            _helperUpgradeRoot.SetActive(false);
        }

        _currentHelpers.Clear();
        _selectedIndex = -1;
    }

    public void RefreshAfterUpgrade(HelperController upgradedHelper)
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

    private void BindHelperList(List<HelperController> helpers)
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

    private int FindHelperIndex(HelperController target)
    {
        if (target == null) return -1;

        for (int i = 0; i < _currentHelpers.Count; i++)
        {
            if (_currentHelpers[i] == target)
                return i;
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

    private void BindDetail(HelperController helper)
    {
        if (helper == null)
        {
            ClearDetail();
            return;
        }

        _helperNameText.text = helper.HelperId;
        _helperGradeText.text = helper.Grade.CurrentGrade.ToString();
        _helperExpText.text = $"{helper.Experience.CurrentExp} / {helper.Experience.MaxExp}";
        _helperRangeText.text = helper.Grade.GetRange().ToString();

        bool canUpgrade = _helperUpgradeService != null && _helperUpgradeService.CanUpgrade(helper);

        if (_upgradeButton != null)
        {
            _upgradeButton.interactable = canUpgrade;
        }

        _helperMessageText.text = canUpgrade
            ? "업그레이드가 가능합니다."
            : _helperUpgradeService != null
                ? _helperUpgradeService.GetBlockReason(helper)
                : "업그레이드할 수 없습니다.";
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
