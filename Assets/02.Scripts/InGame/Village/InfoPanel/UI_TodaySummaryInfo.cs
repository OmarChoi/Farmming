using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class UI_TodaySummaryInfo : UI_InfoSectionBase
{
    private const int MaxQuestPreviewLines = 3;

    private readonly StringBuilder _builder = new();
    PlayerStamina _stamina;

    public override void Subscribe(PlayerController playerController)
    {
        _stamina = playerController.GetComponent<PlayerStamina>();
        UnsubscribeEvents();
        
        TimeEvents.OnNetDayStarted += Refresh;
        TimeEvents.OnNetDayEnded += Refresh;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnGoldChanged += HandleGoldChanged;
        }
        
        if (_stamina != null)
        {
            _stamina.OnChanged += HandleStaminaChanged;
            _stamina.OnMaxChanged += HandleStaminaMaxChanged;
        }
        
        QuestManager questManager = QuestManager.Instance;
        if (questManager != null)
        {
            questManager.OnQuestAccepted += HandleQuestChanged;
            questManager.OnQuestUpdated += HandleQuestChanged;
            questManager.OnQuestCompleted += HandleQuestChanged;
            questManager.OnQuestRemoved += HandleQuestRemoved;
        }
    }
    
    public override void Unsubscribe()
    {
        UnsubscribeEvents();
    }

    public override void Refresh()
    {
        
    }

    // 각 이벤트의 파라미터를 무시하고 공통 Refresh로 회귀시키기 위한 얇은 어댑터.
    private void HandleGoldChanged(Currency _) => Refresh();
    private void HandleStaminaChanged(float _) => Refresh();
    private void HandleStaminaMaxChanged(float _) => Refresh();
    private void HandleQuestChanged(QuestRuntimeData _) => Refresh();
    private void HandleQuestRemoved(string _) => Refresh();

    private void UnsubscribeEvents()
    {
        TimeEvents.OnNetDayStarted -= Refresh;
        TimeEvents.OnNetDayEnded -= Refresh;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnGoldChanged -= HandleGoldChanged;
        }
        
        if (_stamina != null)
        {
            _stamina.OnChanged -= HandleStaminaChanged;
            _stamina.OnMaxChanged -= HandleStaminaMaxChanged;
        }
        
        QuestManager questManager = QuestManager.Instance;
        if (questManager != null)
        {
            questManager.OnQuestAccepted -= HandleQuestChanged;
            questManager.OnQuestUpdated -= HandleQuestChanged;
            questManager.OnQuestCompleted -= HandleQuestChanged;
            questManager.OnQuestRemoved -= HandleQuestRemoved;
        }
    }
    
}