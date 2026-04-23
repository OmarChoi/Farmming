using Photon.Pun;
using UnityEngine;

public class TimeSystem : MonoBehaviour
{
    public static TimeSystem Instance { get; private set; }

    [SerializeField] private TimeSettingSO _timeSettings;

    private GameClock _clock;
    private float _accumulatedGameMinutes;

    private const float SyncInterval = 10f;
    private float _syncTimer;

    public int CurrentDay => _clock?.CurrentDay ?? 0;
    public GameTime CurrentTime => _clock?.CurrentTime ?? default;
    // 다음 게임 분까지의 진행도(0~1). 분 경계 사이를 프레임 단위로 보간할 때 사용
    public float CurrentMinuteProgress => _accumulatedGameMinutes;
    private bool IsDayTime => _clock is { IsDayTime: true };
    private int ElapsedDays => _clock?.ElapsedDays ?? 0;

    public bool HasTimeAuthority => !PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
    private bool CanUseNetworkSync => PhotonNetwork.IsConnected && PhotonNetwork.InRoom && _sync != null;

    private TimeNetworkSync _sync;

    #region Lifecycle
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

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        Tick(Time.deltaTime);

        if (!HasTimeAuthority) return;
        if (!CanUseNetworkSync) return;

        _syncTimer += Time.deltaTime;
        if (_syncTimer < SyncInterval) return;

        _syncTimer = 0f;
        SyncToRemote();
    }
    #endregion

    #region Sync Registration

    public void RegisterSync(TimeNetworkSync sync)
    {
        _sync = sync;
    }

    public void UnregisterSync(TimeNetworkSync sync)
    {
        if (_sync == sync) _sync = null;
    }

    #endregion

    #region Public API

    public void SkipToNextDay()
    {
        if (!HasTimeAuthority)
        {
            if (!CanUseNetworkSync) return;
            _sync.SendSkipToNextDayRequest();
            return;
        }

        ExecuteAuthoritySkipToNextDay();
    }
    #endregion

    #region Local
    private void Tick(float deltaTime)
    {
        if (!TryCreateClock()) return;
        float gameMinutesPerSecond = IsDayTime
            ? _timeSettings.DayGameMinutesPerSecond
            : _timeSettings.NightGameMinutesPerSecond;
        if (gameMinutesPerSecond <= 0f) return;

        _accumulatedGameMinutes += deltaTime * gameMinutesPerSecond;

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
        _syncTimer = 0f;
        SyncLocalState();
        return true;
    }

    private void SyncLocalState()
    {
        TimeEvents.UpdateState(CurrentDay, CurrentTime, IsDayTime, ElapsedDays);
    }

    private void AdvanceOneMinute()
    {
        int prevDay = CurrentDay;
        GameTime prevTime = CurrentTime;

        _clock.AdvanceMinutes(1);
        SyncLocalState();
        TimeEvents.InvokeMinuteChanged(_clock.CurrentTime);

        if (!HasTimeAuthority) return;
        CheckDayEvent(prevDay, prevTime);
    }

    private void ExecuteSkipToNextDay()
    {
        if (_clock == null || _timeSettings == null) return;

        _clock.SetTime(_clock.CurrentDay + 1, _timeSettings.DayStartTime);
        _accumulatedGameMinutes = 0f;

        SyncLocalState();
    }

    private void ExecuteEvent(TimeEvents.EventType eventType)
    {
        switch (eventType)
        {
            case TimeEvents.EventType.DayChange:
                TimeEvents.InvokeNetDayChanged();
                break;
            case TimeEvents.EventType.DayStart:
                TimeEvents.InvokeNetDayStarted();
                break;
            case TimeEvents.EventType.DayEnd:
                TimeEvents.InvokeNetDayEnded();
                break;
            case TimeEvents.EventType.SunRise:
                TimeEvents.InvokeNetSunRise();
                break;
            case TimeEvents.EventType.SunSet:
                TimeEvents.InvokeNetSunSet();
                break;
        }
    }

    #endregion

    #region Authority

    public void ExecuteAuthoritySkipToNextDay()
    {
        if (!HasTimeAuthority) return;

        int prevDay = CurrentDay;
        GameTime prevTime = CurrentTime;

        ExecuteSkipToNextDay();
        SyncToRemote();

        CheckDayEvent(prevDay, prevTime);
    }

    private void CheckDayEvent(int prevDay, GameTime prevTime)
    {
        bool dayChanged = prevDay != _clock.CurrentDay;

        if (HasCrossed(prevTime, _timeSettings.SunriseTime, dayChanged)) ExecuteAuthorityEvent(TimeEvents.EventType.SunRise);
        if (HasCrossed(prevTime, _timeSettings.SunsetTime, dayChanged)) ExecuteAuthorityEvent(TimeEvents.EventType.SunSet);
        if (HasCrossed(prevTime, _timeSettings.DayEndTime, dayChanged)) ExecuteAuthorityEvent(TimeEvents.EventType.DayEnd);

        if (dayChanged) ExecuteAuthorityEvent(TimeEvents.EventType.DayChange);
        if (HasCrossed(prevTime, _timeSettings.DayStartTime, dayChanged)) ExecuteAuthorityEvent(TimeEvents.EventType.DayStart);
    }

    private bool HasCrossed(GameTime prevTime, GameTime targetTime, bool dayChanged)
    {
        int prevMinutes = prevTime.TotalMinutes;
        int currentMinutes = _clock.CurrentTime.TotalMinutes;
        int targetMinutes = targetTime.TotalMinutes;

        if (!dayChanged)
        {
            return prevMinutes < targetMinutes && currentMinutes >= targetMinutes;
        }
        
        // 날짜가 변경된 경우, 이전 날짜에서 이벤트 시간을 지나지 않았거나, 새 날짜에서 이벤트 시간에 도달했는지 확인합니다.
        return prevMinutes < targetMinutes || currentMinutes >= targetMinutes;
    }

    private void ExecuteAuthorityEvent(TimeEvents.EventType eventType)
    {
        if (!HasTimeAuthority) return;

        ExecuteEvent(eventType);
        BroadcastEventToRemote(eventType);
    }
    #endregion

    #region Network Delegation

    private void SyncToRemote()
    {
        if (!CanUseNetworkSync) return;
        _sync.SendSyncTime(CurrentDay, CurrentTime.Hour, CurrentTime.Minute);
    }

    private void BroadcastEventToRemote(TimeEvents.EventType eventType)
    {
        if (!CanUseNetworkSync) return;
        _sync.SendBroadcastEvent(eventType);
    }

    /// <summary>
    /// 원격 클라이언트 시간 동기화. TimeNetworkSync에서 호출.
    /// </summary>
    public void ApplySyncTime(int day, int hour, int minute)
    {
        if (HasTimeAuthority) return;

        if (_clock == null && !TryCreateClock())
        {
            Debug.LogError("TimeSystem: TryCreateClock() failed");
            return;
        }

        _clock.SetTime(day, new GameTime(hour, minute));
        _accumulatedGameMinutes = 0f;
        _syncTimer = 0f;
        SyncLocalState();
    }

    /// <summary>
    /// 원격 이벤트 실행. TimeNetworkSync에서 호출.
    /// </summary>
    public void ApplyRemoteEvent(TimeEvents.EventType eventType)
    {
        if (HasTimeAuthority) return;
        ExecuteEvent(eventType);
    }

    #endregion

    public void ImportTimeSaveData(TimeSaveData timeSaveData)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        _clock.SetTime
        (
            timeSaveData.Day,
            new GameTime(timeSaveData.Hour, timeSaveData.Minute)
        );
        SyncLocalState();
    }

    public TimeSaveData ExportSaveData()
    {
        return new TimeSaveData
        {
            Day = CurrentDay,
            Hour = CurrentTime.Hour,
            Minute = CurrentTime.Minute
        };
    }
}
