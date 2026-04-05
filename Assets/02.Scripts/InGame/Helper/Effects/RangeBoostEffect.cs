using UnityEngine;
using System;

public class RangeBoostEffect : HelperAbility
{
    public bool IsActive { get; private set; }
    public int RemainingMinutes { get; private set; }

    public event Action OnActivated;
    public event Action OnDeactivated;

    public void Activate(int durationGameMinutes = 60)
    {
        RemainingMinutes = durationGameMinutes;

        if (IsActive) return; // 이미 활성화 중이면 시간만 갱신

        IsActive = true;
        TimeEvents.OnMinuteChanged += OnMinuteChanged;
        OnActivated?.Invoke();
    }

    private void OnMinuteChanged(GameTime time)
    {
        if(!IsActive) return;

        RemainingMinutes--;
        if(RemainingMinutes <= 0)
        {
            Deactivate();
        }
    }

    private void Deactivate()
    {
        IsActive = false;
        RemainingMinutes = 0;
        TimeEvents.OnMinuteChanged -= OnMinuteChanged;
        OnDeactivated?.Invoke();
    }

    private void OnDestroy()
    {
        if(IsActive)
        {
            TimeEvents.OnMinuteChanged -= OnMinuteChanged;
        }
    }
}
