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
    private HelperController _helper;

    public int SlotIndex => _slotIndex;
    public HelperController Helper => _helper;

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

    public void Refresh(HelperController helper, bool isSelected)
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

        _helperNameText.text = helper.HelperId;
        _helperGradeText.text = helper.Grade.CurrentGrade.ToString();
        _upgradeReadyText.text = helper.Experience.IsReadyToUpgrade ? "업그레이드 가능!" : "업그레이드 불가능";

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
