using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_DinoRaceSelectionPanel : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private Button[] _runnerButtons;
    [SerializeField] private TextMeshProUGUI[] _runnerLabels;
    [SerializeField] private Image[] _runnerHighlights;
    [SerializeField] private TextMeshProUGUI _currentGoldText;
    [SerializeField] private TextMeshProUGUI _betAmountText;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _decreaseBetButton;
    [SerializeField] private Button _increaseBetButton;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _closeButton;

    private Action<int, int> _onConfirm;
    private Action _onClose;
    private int _selectedRunnerIndex = -1;
    private int _betAmount;
    private int _betStepAmount;
    private int _minBetAmount;

    private void Awake()
    {
        int runnerButtonCount = _runnerButtons != null ? _runnerButtons.Length : 0;
        for (int i = 0; i < runnerButtonCount; i++)
        {
            int capturedIndex = i;
            if (_runnerButtons[i] != null)
                _runnerButtons[i].onClick.AddListener(() => SelectRunner(capturedIndex));
        }

        if (_decreaseBetButton != null)
            _decreaseBetButton.onClick.AddListener(() => ChangeBet(-_betStepAmount));
        if (_increaseBetButton != null)
            _increaseBetButton.onClick.AddListener(() => ChangeBet(_betStepAmount));
        if (_confirmButton != null)
            _confirmButton.onClick.AddListener(ConfirmSelection);
        if (_closeButton != null)
            _closeButton.onClick.AddListener(() => _onClose?.Invoke());

        Hide();
    }

    public void Show(
        string[] runnerNames,
        int currentGold,
        int defaultBetAmount,
        int betStepAmount,
        int minBetAmount,
        Action<int, int> onConfirm,
        Action onClose)
    {
        _onConfirm = onConfirm;
        _onClose = onClose;
        _betStepAmount = Mathf.Max(1, betStepAmount);
        _minBetAmount = Mathf.Max(1, minBetAmount);
        _betAmount = Mathf.Max(_minBetAmount, defaultBetAmount);
        _selectedRunnerIndex = runnerNames != null && runnerNames.Length > 0 ? 0 : -1;

        if (_currentGoldText != null)
            _currentGoldText.text = $"Gold: {currentGold}G";

        SetMessage(string.Empty);
        RefreshRunnerButtons(runnerNames);
        RefreshSelectionState();
        RefreshBetAmount();

        if (_root != null)
            _root.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void SetMessage(string message)
    {
        if (_messageText != null)
            _messageText.text = message ?? string.Empty;
    }

    public void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void SelectRunner(int runnerIndex)
    {
        _selectedRunnerIndex = runnerIndex;
        RefreshSelectionState();
    }

    private void ConfirmSelection()
    {
        if (_selectedRunnerIndex < 0)
        {
            SetMessage("어떤 곡룡이 이길지 선택하세요!");
            return;
        }

        _onConfirm?.Invoke(_selectedRunnerIndex, _betAmount);
    }

    private void ChangeBet(int amountDelta)
    {
        _betAmount = Mathf.Max(_minBetAmount, _betAmount + amountDelta);
        RefreshBetAmount();
    }

    private void RefreshRunnerButtons(string[] runnerNames)
    {
        int runnerButtonCount = _runnerButtons != null ? _runnerButtons.Length : 0;
        int runnerLabelCount = _runnerLabels != null ? _runnerLabels.Length : 0;

        for (int i = 0; i < runnerButtonCount; i++)
        {
            bool isActive = runnerNames != null && i < runnerNames.Length;
            if (_runnerButtons[i] != null)
                _runnerButtons[i].gameObject.SetActive(isActive);
            if (i < runnerLabelCount && _runnerLabels[i] != null && isActive)
                _runnerLabels[i].text = runnerNames[i];
        }
    }

    private void RefreshSelectionState()
    {
        int highlightCount = _runnerHighlights != null ? _runnerHighlights.Length : 0;
        for (int i = 0; i < highlightCount; i++)
        {
            if (_runnerHighlights[i] != null)
                _runnerHighlights[i].enabled = i == _selectedRunnerIndex;
        }
    }

    private void RefreshBetAmount()
    {
        if (_betAmountText != null)
            _betAmountText.text = $"Bet: {_betAmount}G";
    }
}
