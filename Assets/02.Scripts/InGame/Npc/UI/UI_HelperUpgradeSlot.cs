using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_HelperUpgradeSlot : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI _helperNameText;
    [SerializeField] private TextMeshProUGUI _helperGradeText;
    [SerializeField] private TextMeshProUGUI _upgradeReadyText;

    [Header("선택 표시")]
    [SerializeField] private GameObject _selectedHighlight;

    [Header("버튼")]
    [SerializeField] private Button _selectButton;

    private UI_HelperUpgrade _uiHelperUpgrade;
    private int _slotIndex;
    private HelperDataSO _helper;

    public int SlotIndex => _slotIndex;
    public HelperDataSO Helper => _helper;

    private void Awake()
    {
        if (_selectButton != null)
        {
            _selectButton.onClick.RemoveAllListeners();
            _selectButton.onClick.AddListener(HandleClickSlot);
        }
    }

    public void Init(UI_HelperUpgrade uiHelperUpgrade, int index)
    {
        _uiHelperUpgrade = uiHelperUpgrade;
        _slotIndex = index;
    }

    public void Refresh(HelperDataSO helper, bool isSelected)
    {
        _helper = helper;

        if (helper == null)
        {
            _helperNameText.text = "";
            _helperGradeText.text = "";
            _upgradeReadyText.text = "";
            SetSelected(false);
            return;
        }

        EHelperGrade grade = _uiHelperUpgrade != null
            ? _uiHelperUpgrade.GetGrade(helper)
            : EHelperGrade.Normal;

        bool canUpgrade = _uiHelperUpgrade != null && _uiHelperUpgrade.CanUpgrade(helper);

        _helperNameText.text = string.IsNullOrEmpty(helper.HelperName) ? helper.HelperId : helper.HelperName;
        _helperGradeText.text = grade.ToString();
        _upgradeReadyText.text = canUpgrade ? "업그레이드 가능!" : "업그레이드 불가능";

        SetSelected(isSelected);
    }

    public void SetSelected(bool isSelected)
    {
        if (_selectedHighlight != null)
        {
            _selectedHighlight.SetActive(isSelected);
        }
    }

    private void HandleClickSlot()
    {
        _uiHelperUpgrade?.SelectSlot(_slotIndex);
    }
}
