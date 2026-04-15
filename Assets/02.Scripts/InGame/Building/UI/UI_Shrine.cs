using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Shrine : UIBase
{
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _remainingDaysText;
    [SerializeField] private TextMeshProUGUI _effectText;

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
        RefreshQuestInfo();
        RefreshEffectText();
    }

    private void RefreshQuestInfo()
    {
        var service = WorldEffectQuestService.Instance;
        bool hasActive = service != null && service.HasActiveQuest;

        if (_completeButton != null) _completeButton.interactable = hasActive;

        if (!hasActive)
        {
            if (_titleText != null) _titleText.text = "제단";
            if (_descriptionText != null) _descriptionText.text = "진행 중인 월드 퀘스트가 없습니다.";
            if (_remainingDaysText != null) _remainingDaysText.text = string.Empty;
            return;
        }

        var quest = QuestManager.Instance?.GetQuest(service.ActiveQuestId);
        QuestDataSO data = quest?.QuestData;

        if (_titleText != null) _titleText.text = data != null ? data.QuestName : "월드 퀘스트";
        if (_descriptionText != null) _descriptionText.text = data != null ? data.Description : string.Empty;

        if (_remainingDaysText != null && quest != null)
        {
            int remaining = Mathf.Max(0, quest.ExpireDay - TimeEvents.CurrentDay);
            _remainingDaysText.text = $"남은 기간: {remaining}일";
        }
    }

    private void RefreshEffectText()
    {
        if (_effectText == null) return;

        WorldEffectManager manager = WorldEffectManager.Instance;
        if (manager == null || manager.ActiveEffects == null || manager.ActiveEffects.Count == 0)
        {
            _effectText.text = "활성 월드 효과: 없음";
            return;
        }

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("활성 월드 효과:");
        for (int i = 0; i < manager.ActiveEffects.Count; i++)
        {
            WorldEffectEntry entry = manager.ActiveEffects[i];
            string kind = ((EWorldEffectKind)entry.Kind).ToString();
            string remaining = entry.RemainingDays < 0 ? "영구" : $"{entry.RemainingDays}일";
            builder.AppendLine($"- {entry.EffectId} ({kind}, {remaining})");
        }
        _effectText.text = builder.ToString();
    }
}