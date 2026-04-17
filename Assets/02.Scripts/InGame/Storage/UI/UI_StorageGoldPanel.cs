using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_StorageGoldPanel : MonoBehaviour
{
    [Header("잔액 표시")]
    [SerializeField] private TMP_Text _storageText;

    [Header("금액 입력")]
    [SerializeField] private Slider _amountSlider;
    [SerializeField] private TMP_Text _amountText;
    [SerializeField] private Button _plus100Button;
    [SerializeField] private Button _plus1000Button;
    [SerializeField] private Button _maxButton;

    [Header("액션")]
    [SerializeField] private Button _depositButton;
    [SerializeField] private Button _withdrawButton;

    private StorageDomain _storage;
    private StorageTransferService _transferService;
    private bool _isBound;

    private void Awake()
    {
        if (_plus100Button != null) _plus100Button.onClick.AddListener(() => AddAmount(100));
        if (_plus1000Button != null) _plus1000Button.onClick.AddListener(() => AddAmount(1000));
        if (_maxButton != null) _maxButton.onClick.AddListener(SetAmountToMax);
        if (_depositButton != null) _depositButton.onClick.AddListener(OnDepositClicked);
        if (_withdrawButton != null) _withdrawButton.onClick.AddListener(OnWithdrawClicked);
        if (_amountSlider != null) _amountSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDestroy()
    {
        if (_plus100Button != null) _plus100Button.onClick.RemoveAllListeners();
        if (_plus1000Button != null) _plus1000Button.onClick.RemoveAllListeners();
        if (_maxButton != null) _maxButton.onClick.RemoveAllListeners();
        if (_depositButton != null) _depositButton.onClick.RemoveAllListeners();
        if (_withdrawButton != null) _withdrawButton.onClick.RemoveAllListeners();
        if (_amountSlider != null) _amountSlider.onValueChanged.RemoveAllListeners();

        UnbindCurrency();
        UnbindStorage();
    }

    public void Bind(StorageDomain storage, StorageTransferService transferService)
    {
        if (storage == null || transferService == null) return;

        UnbindStorage();
        _storage = storage;
        _transferService = transferService;

        if (_storage != null)
            _storage.OnGoldChanged += OnStorageGoldChanged;

        BindCurrency();
        _isBound = true;

        ResetAmount();
        RefreshAll();
    }

    public void Unbind()
    {
        if (!_isBound) return;
        _isBound = false;

        UnbindCurrency();
        UnbindStorage();
        _transferService = null;
    }

    private void BindCurrency()
    {
        var currency = CurrencyManager.Instance;
        if (currency != null)
            currency.OnGoldChanged += OnWalletGoldChanged;
    }

    private void UnbindCurrency()
    {
        var currency = CurrencyManager.Instance;
        if (currency != null)
            currency.OnGoldChanged -= OnWalletGoldChanged;
    }

    private void UnbindStorage()
    {
        if (_storage != null)
            _storage.OnGoldChanged -= OnStorageGoldChanged;
        _storage = null;
    }

    private void OnWalletGoldChanged(Currency _) => RefreshAll();
    private void OnStorageGoldChanged(int _) => RefreshAll();

    private void RefreshAll()
    {
        int wallet = GetWalletGold();
        int storage = _storage != null ? _storage.Gold : 0;

        if (_storageText != null) _storageText.text = $"{storage:N0} G";

        if (_amountSlider != null)
        {
            int max = Mathf.Max(wallet, storage);
            _amountSlider.maxValue = max;
            if (_amountSlider.value > max)
                _amountSlider.value = max;
        }

        RefreshActionButtons();
        RefreshAmountText();
    }

    private void RefreshActionButtons()
    {
        int amount = GetAmount();
        int wallet = GetWalletGold();
        int storage = _storage != null ? _storage.Gold : 0;

        if (_depositButton != null)
            _depositButton.interactable = amount > 0 && amount <= wallet;
        if (_withdrawButton != null)
            _withdrawButton.interactable = amount > 0 && amount <= storage;
    }

    private void RefreshAmountText()
    {
        if (_amountText != null)
            _amountText.text = $"{GetAmount():N0} G";
    }

    private void OnSliderChanged(float _)
    {
        RefreshAmountText();
        RefreshActionButtons();
    }

    private void AddAmount(int delta)
    {
        if (_amountSlider == null) return;
        _amountSlider.value = Mathf.Clamp(_amountSlider.value + delta, 0, _amountSlider.maxValue);
    }

    private void SetAmountToMax()
    {
        if (_amountSlider == null) return;
        _amountSlider.value = _amountSlider.maxValue;
    }

    private void ResetAmount()
    {
        if (_amountSlider != null)
            _amountSlider.value = 0f;
    }

    private int GetAmount()
    {
        return _amountSlider != null ? Mathf.RoundToInt(_amountSlider.value) : 0;
    }

    private int GetWalletGold()
    {
        var currency = CurrencyManager.Instance;
        return currency != null ? (int)currency.GetGold() : 0;
    }

    private void OnDepositClicked()
    {
        int amount = GetAmount();
        if (amount <= 0 || _transferService == null) return;
        if (_transferService.DepositGold(amount))
            ResetAmount();
    }

    private void OnWithdrawClicked()
    {
        int amount = GetAmount();
        if (amount <= 0 || _transferService == null) return;
        if (_transferService.WithdrawGold(amount)) 
            ResetAmount();
    }
}