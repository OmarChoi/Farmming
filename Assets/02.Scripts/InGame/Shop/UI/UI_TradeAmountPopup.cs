using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Cysharp.Threading.Tasks;

public class UI_TradeAmountPopup : MonoBehaviour
{
    [Header("루트")]
    [SerializeField] private GameObject _root;

    [Header("Popup Do Tween")]
    [SerializeField] private UI_PopupDoTween _popupDoTween;

    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI _itemNameText;
    [SerializeField] private TextMeshProUGUI _totalGoldText;
    [SerializeField] private TextMeshProUGUI _confirmButtonText;
    [SerializeField] private TextMeshProUGUI _cancelButtonText;

    [Header("아이콘")]
    [SerializeField] private Image _itemIconImage;

    [Header("수량 입력")]
    [SerializeField] private TMP_InputField _amountInputField;

    [Header("버튼")]
    [SerializeField] private Button _minusButton;
    [SerializeField] private Button _plusButton;
    [SerializeField] private Button _maxButton;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    private ItemDataSO _currentItem;
    private ETradeType _tradeType;
    private int _currentAmount;
    private int _maxAmount;
    private Action<int> _onConfirm;
    private bool _isClosing;
    public bool IsOpen => _root != null && _root.activeSelf;
    public bool IsClosing => _isClosing;

    private void Awake()
    {
        if (_minusButton != null)
        {
            _minusButton.onClick.AddListener(OnClickMinus);
        }
        if (_plusButton != null)
        {
            _plusButton.onClick.AddListener(OnClickPlus);
        }
        if (_maxButton != null)
        {
            _maxButton.onClick.AddListener(OnClickMax);
        }
        if (_confirmButton != null)
        {
            _confirmButton.onClick.AddListener(OnClickConfirm);
        }
        if (_cancelButton != null)
        {
            _cancelButton.onClick.AddListener(OnClickCancel);
        }
        if (_amountInputField != null)
        {
            _amountInputField.onEndEdit.AddListener(OnAmountInputEndEdit);
        }

        CloseImmediate();
    }

    private void OnDestroy()
    {
        if (_minusButton != null)
        {
            _minusButton.onClick.RemoveListener(OnClickMinus);
        }
        if (_plusButton != null)
        {
            _plusButton.onClick.RemoveListener(OnClickPlus);
        }
        if (_maxButton != null)
        {
            _maxButton.onClick.RemoveListener(OnClickMax);
        }
        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveListener(OnClickConfirm);
        }
        if (_cancelButton != null)
        {
            _cancelButton.onClick.RemoveListener(OnClickCancel);
        }
        if (_amountInputField != null)
        {
            _amountInputField.onEndEdit.RemoveListener(OnAmountInputEndEdit);
        }
    }

    public async UniTask OpenAsync(ItemDataSO item, ETradeType tradeType, int maxAmount, Action<int> onConfirm)
    {
        if (IsOpen || _isClosing) return;
        if (item == null || maxAmount <= 0) return;

        _currentItem = item;
        _itemIconImage.sprite = ItemDisplayFormatter.GetIcon(item);
        _tradeType = tradeType;
        _maxAmount = Mathf.Max(1, maxAmount);
        _currentAmount = 1;
        _amountInputField.text = _currentAmount.ToString();
        _confirmButtonText.text = GetConfirmText();
        _cancelButtonText.text = GetCancelText();
        _onConfirm = onConfirm;
        _isClosing = false;

        RefreshUI();

        if (_popupDoTween != null)
        {
            await _popupDoTween.PlayOpenAsync();
        }
        else if (_root != null)
        {
            _root.SetActive(true);
        }
    }

    public async UniTask CloseAsync()
    {
        if (!IsOpen || _isClosing) return;
        _isClosing = true;

        if (_popupDoTween != null)
        {
            await _popupDoTween.PlayCloseAsync();
        }
        else if (_root != null)
        {
            _root.SetActive(false);
        }
        ClearState();
    }

    public void CloseImmediate()
    {
        if (_root != null)
        {
            _root.SetActive(false);
        }
        ClearState();
    }

    private void ClearState()
    {
        _currentItem = null;
        _itemIconImage.sprite = null;
        _onConfirm = null;
        _confirmButtonText.text = string.Empty;
        _cancelButtonText.text = string.Empty;
        _currentAmount = 1;
        _maxAmount = 1;
        _isClosing = false;
    }

    private void OnClickMinus()
    {
        if (_isClosing) return;
        SetAmount(_currentAmount - 1);
    }

    private void OnClickPlus()
    {
        if (_isClosing) return;
        SetAmount(_currentAmount + 1);
    }

    private void OnClickMax()
    {
        if (_isClosing) return;
        SetAmount(_maxAmount);
    }

    private void OnClickConfirm()
    {
        if (_isClosing) return;
        if (_currentItem == null) return;

        _onConfirm?.Invoke(_currentAmount);
        CloseAsync().Forget();
    }

    private void OnClickCancel()
    {
        if (_isClosing) return;
        CloseAsync().Forget();
    }

    private void OnAmountInputEndEdit(string value)
    {
        if (_isClosing) return;

        if (string.IsNullOrWhiteSpace(value))
        {
            SetAmount(1);
            return;
        }

        if (!int.TryParse(value, out int parsedAmount))
        {
            SetAmount(_currentAmount);
            return;
        }

        SetAmount(parsedAmount);
    }

    private void SetAmount(int amount)
    {
        _currentAmount = Mathf.Clamp(amount, 1, _maxAmount);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if(_currentItem == null) return;

        if(_itemNameText != null)
        {
            _itemNameText.text = ItemDisplayFormatter.GetName(_currentItem);
        }
        if(_itemIconImage != null)
        {
            _itemIconImage.sprite = ItemDisplayFormatter.GetIcon(_currentItem);
        }
        if(_amountInputField != null)
        {
            _amountInputField.text = _currentAmount.ToString();
        }
        if(_totalGoldText != null)
        {
            _totalGoldText.text = ItemDisplayFormatter.GetTotalCostText(_currentItem, _tradeType, _currentAmount);
        }
        if(_confirmButtonText != null)
        {
            _confirmButtonText.text = GetConfirmText();
        }
        if(_cancelButtonText != null)
        {
            _cancelButtonText.text = GetCancelText();
        }
    }

    private string GetConfirmText()
    {
        return _tradeType == ETradeType.Buy ? "살래요!" : "팔래요!";
    }

    private string GetCancelText()
    {
        return _tradeType == ETradeType.Buy ? "안 살래요" : "안 팔래요";
    }
}
