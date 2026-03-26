using System;
using UnityEngine;

public class TimeSystem : MonoBehaviour, ITimeReader
{
    public static TimeSystem Instance { get; private set; }

    [SerializeField] private TimeSettingSO _timeSettings;

    private GameClock _clock;
    private float _accumulatedGameMinutes;

    public int CurrentDay => _clock?.CurrentDay ?? 0;
    public GameTime CurrentTime => _clock?.CurrentTime ?? default;
    public bool IsDayTime => _clock is { IsDayTime: true };

    public event Action<GameTime> OnMinuteChanged;
    public event Action<int> OnDayChanged;
    public event Action OnDayStarted;
    public event Action OnDayEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        TryCreateClock();
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Initialize(TimeSettingSO timeSettings)
    {
        _timeSettings = timeSettings;
        TryCreateClock(resetClock: true);
    }

    public void Tick(float deltaTime)
    {
        if (!TryCreateClock())
        {
            return;
        }

        if (_timeSettings.GameMinutesPerSecond <= 0f)
        {
            return;
        }

        _accumulatedGameMinutes += deltaTime * _timeSettings.GameMinutesPerSecond;

        int elapsedMinutes = (int)_accumulatedGameMinutes;
        if (elapsedMinutes <= 0)
        {
            return;
        }

        _accumulatedGameMinutes -= elapsedMinutes;

        for (int i = 0; i < elapsedMinutes; i++)
        {
            AdvanceOneMinute();
        }
    }

    // resetClock : _clock이 이미 있어도 새 설정값으로 다시 생성
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
        return true;
    }

    private void AdvanceOneMinute()
    {
        bool wasDayTime = _clock.IsDayTime;
        int previousDay = _clock.CurrentDay;

        _clock.AdvanceMinutes(1);

        OnMinuteChanged?.Invoke(_clock.CurrentTime);

        if (previousDay != _clock.CurrentDay)
        {
            OnDayChanged?.Invoke(_clock.CurrentDay);
        }

        if (!wasDayTime && _clock.IsDayTime)
        {
            OnDayStarted?.Invoke();
        }
        else if (wasDayTime && !_clock.IsDayTime)
        {
            OnDayEnded?.Invoke();
        }
    }
}
