using System;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class UI_Shrine : UIBase
{

    [SerializeField] private GameObject _emptyStateText;
    [SerializeField] private GameObject _questInfo;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _remainingDaysText;
    [SerializeField] private TextMeshProUGUI _successEffectText;
    [SerializeField] private TextMeshProUGUI _failureEffectText;

    [Header("Image")]
    [SerializeField] private Image _itemIconImage;

    [Header("Buttons")]
    [SerializeField] private Button _completeButton;
    [SerializeField] private Button _closeButton;

    private ShrineBuilding _shrine;
    private Action _onClose;
    private bool _suppressCloseNotify;

    public void Configure(ShrineBuilding shrine, Action onClose)
    {
        _shrine = shrine;
        _onClose = onClose;
    }

    public void CloseFromOwner()
    {
        if (!IsOpen) return;

        _suppressCloseNotify = true;
        RequestClose();
    }

    protected override void OnOpen()
    {
        if (_completeButton != null)
        {
            _completeButton.onClick.RemoveAllListeners();
            _completeButton.onClick.AddListener(OnCompleteClicked);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(RequestClose);
        }

        WorldEffectEvents.OnEffectsChanged -= Refresh;
        WorldEffectEvents.OnEffectsChanged += Refresh;

        Refresh();
    }

    protected override void OnClose()
    {
        WorldEffectEvents.OnEffectsChanged -= Refresh;

        Action onClose = _onClose;
        bool notify = !_suppressCloseNotify;
        _shrine = null;
        _onClose = null;
        _suppressCloseNotify = false;

        if (notify) onClose?.Invoke();
    }

    private void OnCompleteClicked()
    {
        _shrine?.RequestCompletion();
        Refresh();
    }

    private void RequestClose()
    {
        if (UIController.Instance != null)
        {
            UIController.Instance.CloseAsync<UI_Shrine>().Forget();
            return;
        }
        CloseAsync().Forget();
    }

    private void Refresh()
    {
        if (!UpdateActiveState()) return;

        RefreshQuestInfo();
        RefreshEffectText();
    }

    private bool UpdateActiveState()
    {
        var service = WorldEffectQuestService.Instance;
        bool hasActive = service != null && service.HasActiveQuest;

        if (_completeButton != null) _completeButton.interactable = hasActive;
        _emptyStateText.SetActive(!hasActive);
        _questInfo.SetActive(hasActive);

        return hasActive;
    }

    private void RefreshQuestInfo()
    {
        QuestRuntimeData quest =
            QuestManager.Instance?.GetQuest(WorldEffectQuestService.Instance.ActiveQuestId);
        QuestDataSO data = quest?.QuestData;
        if (data == null) return;

        var requireItemInfo = data.ItemRequirements;
        if (requireItemInfo == null || requireItemInfo.Count == 0) return;

        _itemIconImage.sprite = requireItemInfo[0].Item.Icon;
        _descriptionText.text = data.Description;

        int remaining = Mathf.Max(0, quest.ExpireDay - TimeEvents.CurrentDay);
        _remainingDaysText.text = $"남은 기간: {remaining}일";
    }

    private void RefreshEffectText()
    {
        var service = WorldEffectQuestService.Instance;
        var rules = service.FindRuleByQuestId(service.ActiveQuestId);
        if (rules == null) return;

        _successEffectText.SetText($"성공시 : {rules.SuccessEffect.Description}");
        _failureEffectText.SetText($"실패시 : {rules.FailureEffect.Description}");
    }
}