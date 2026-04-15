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

        SubscribeQuestEvents(true);

        Refresh();
    }

    protected override void OnClose()
    {
        SubscribeQuestEvents(false);

        Action onClose = _onClose;
        bool notify = !_suppressCloseNotify;
        _shrine = null;
        _onClose = null;
        _suppressCloseNotify = false;

        if (notify) onClose?.Invoke();
    }

    private void SubscribeQuestEvents(bool subscribe)
    {
        var manager = QuestManager.Instance;
        if (manager == null) return;

        manager.OnQuestAccepted -= OnQuestChanged;
        manager.OnQuestUpdated -= OnQuestChanged;
        manager.OnQuestCompleted -= OnQuestChanged;
        manager.OnQuestRemoved -= OnQuestRemoved;

        if (!subscribe) return;

        manager.OnQuestAccepted += OnQuestChanged;
        manager.OnQuestUpdated += OnQuestChanged;
        manager.OnQuestCompleted += OnQuestChanged;
        manager.OnQuestRemoved += OnQuestRemoved;
    }

    private void OnQuestChanged(QuestRuntimeData quest)
    {
        if (quest?.QuestData == null) return;
        if (!IsForcedQuest(quest.QuestData.QuestId)) return;
        Refresh();
    }

    private void OnQuestRemoved(string questId)
    {
        if (!IsForcedQuest(questId)) return;
        Refresh();
    }

    private bool IsForcedQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return false;
        var service = WorldEffectQuestService.Instance;
        return service != null && service.FindRuleByQuestId(questId) != null;
    }

    private void OnCompleteClicked()
    {
        _shrine?.RequestCompletion();
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

        ItemDataSO representative = data.RepresentativeItem;
        if (representative == null) return;

        _itemIconImage.sprite = representative.Icon;
        _descriptionText.text = data.Description;
        _remainingDaysText.text = $"남은 기간: {quest.RemainingDays}일";
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