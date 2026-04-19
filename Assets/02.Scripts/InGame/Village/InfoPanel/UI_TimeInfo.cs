using TMPro;
using UnityEngine;

public class UI_TimeInfo : UI_InfoSectionBase
{
    [Header("Time Infos")]
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private TextMeshProUGUI _dateText;
    
    public override void Subscribe(PlayerController playerController)
    {
        TimeEvents.OnMinuteChanged += HandleMinuteChanged;
        TimeEvents.OnNetDayChanged += HandleDayChanged;
    }
    
    public override void Unsubscribe()
    {
        TimeEvents.OnMinuteChanged -= HandleMinuteChanged;
        TimeEvents.OnNetDayChanged -= HandleDayChanged;
    }
    
    public override void Refresh()
    {
        ApplyTime(TimeEvents.CurrentTime);
        ApplyDay(TimeEvents.CurrentDay);
    }

    private void HandleMinuteChanged(GameTime time)
    {
        ApplyTime(time);
    }

    private void HandleDayChanged()
    {
        ApplyDay(TimeEvents.CurrentDay);
    }

    private void ApplyTime(GameTime time)
    {
        if (_timeText == null) return;
        _timeText.SetText(time.ToString());
    }

    private void ApplyDay(int cumulativeDay)
    {
        if (_dateText == null) return;
        _dateText.SetText(GameTime.FormatMonthDay(cumulativeDay));
    }
}
