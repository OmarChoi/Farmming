using UnityEngine;

public class TimeSystem : MonoBehaviour
{
    [SerializeField] private TimeSettingSO _timeSettings;

    private GameClock _clock;
    private float _accumulatedGameMinutes;

    public int CurrentDay => _clock?.CurrentDay ?? 0;
    public GameTime CurrentTime => _clock?.CurrentTime ?? default;
    public bool IsDayTime => _clock is { IsDayTime: true };
    public int ElapsedDays => _clock?.ElapsedDays ?? 0;

    private void Awake()
    {
        TryCreateClock();
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Initialize(TimeSettingSO timeSettings)
    {
        _timeSettings = timeSettings;
        TryCreateClock(true);
    }

    public void SkipToNextDay()
    {
        if (_clock == null || _timeSettings == null) return;

        int previousDay = _clock.CurrentDay;
        bool wasDayTime = _clock.IsDayTime;

        _clock.SetTime(previousDay + 1, _timeSettings.WakeUpTime);
        _accumulatedGameMinutes = 0f;
        SyncState();

        TimeEvents.InvokeDayChanged(_clock.CurrentDay);

        if (wasDayTime)
        {
            TimeEvents.InvokeDayEnded();
        }
        TimeEvents.InvokeDayStarted();
    }

    public void Tick(float deltaTime)
    {
        if (!TryCreateClock()) return;
        if (_timeSettings.GameMinutesPerSecond <= 0f) return;

        _accumulatedGameMinutes += deltaTime * _timeSettings.GameMinutesPerSecond;

        int elapsedMinutes = (int)_accumulatedGameMinutes;
        if (elapsedMinutes <= 0) return;

        _accumulatedGameMinutes -= elapsedMinutes;

        for (int i = 0; i < elapsedMinutes; i++)
        {
            AdvanceOneMinute();
        }
    }

    private bool TryCreateClock(bool resetClock = false)
    {
        if (!resetClock && _clock != null) return true;
        if (_timeSettings == null) return false;

        _clock = new GameClock
        (
            _timeSettings.DefaultDay,
            _timeSettings.DefaultTime,
            _timeSettings.DayStartTime,
            _timeSettings.DayEndTime
        );

        _accumulatedGameMinutes = 0f;
        SyncState();
        return true;
    }

    private void SyncState()
    {
        TimeEvents.UpdateState(CurrentDay, CurrentTime, IsDayTime, ElapsedDays);
    }

    private void AdvanceOneMinute()
    {
        bool wasDayTime = _clock.IsDayTime;
        int previousDay = _clock.CurrentDay;

        _clock.AdvanceMinutes(1);
        SyncState();

        TimeEvents.InvokeMinuteChanged(_clock.CurrentTime);

        if (previousDay != _clock.CurrentDay)
        {
            TimeEvents.InvokeDayChanged(_clock.CurrentDay);
        }

        if (!wasDayTime && _clock.IsDayTime)
        {
            TimeEvents.InvokeDayStarted();
        }
        else if (wasDayTime && !_clock.IsDayTime)
        {
            TimeEvents.InvokeDayEnded();
        }
    }
}