using Photon.Pun;
using UnityEngine;

public class TimeSystem : MonoBehaviourPunCallbacks
{
    [SerializeField] private TimeSettingSO _timeSettings;

    private GameClock _clock;
    private float _accumulatedGameMinutes;

    private const float SyncInterval = 10f;
    private float _syncTimer;

    private int CurrentDay => _clock?.CurrentDay ?? 0;
    public GameTime CurrentTime => _clock?.CurrentTime ?? default;
    private bool IsDayTime => _clock is { IsDayTime: true };
    private int ElapsedDays => _clock?.ElapsedDays ?? 0;

    // 오프라인이거나 아직 룸에 입장하기 전이면 로컬 인스턴스를 권한 주체로 간주한다.
    // 네트워크 통신이 없어도 시간 진행과 일자 기반 이벤트가 계속 동작해야 한다.
    private bool HasTimeAuthority => !PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
    private bool CanUseNetworkSync => PhotonNetwork.IsConnected && PhotonNetwork.InRoom && photonView != null;

    #region Lifecycle
    private void Awake()
    {
        TryCreateClock();
    }

    private void Update()
    {
        Tick(Time.deltaTime);

        // 권한 주체만 일정 주기로 현재 시간을 원격 클라이언트에 동기화한다.
        if (!HasTimeAuthority) return;
        if (!CanUseNetworkSync) return;

        _syncTimer += Time.deltaTime;
        if (_syncTimer < SyncInterval) return;

        _syncTimer = 0f;
        SyncToRemote();
    }
    #endregion

    #region Public API

    public void SkipToNextDay()
    {
        if (!HasTimeAuthority)
        {
            if (!CanUseNetworkSync) return;

            photonView.RPC(nameof(RPC_RequestSkipToNextDay), RpcTarget.MasterClient);
            return;
        }

        ExecuteAuthoritySkipToNextDay();
    }
    #endregion

    #region Local
    private void Tick(float deltaTime)
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

        // 권한 주체만 일자/시간 경계 이벤트 발생 여부를 판정한다.
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

    private void ExecuteAuthoritySkipToNextDay()
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

        // 날짜가 바뀐 경우, 두 구간을 함께 고려해서 판정한다.
        // 1) prevMinutes < targetMinutes:
        //    이전 날짜에서 아직 지나지 않은 이벤트인지 확인한다.
        // 2) currentMinutes >= targetMinutes:
        //    새 날짜에서 이미 도달한 이벤트인지 확인한다.
        // 두 조건이 모두 false인 경우는
        // "이전 날짜에서 이미 처리했고, 새 날짜에서는 아직 도달하지 않은 이벤트"이므로 제외한다.
        return prevMinutes < targetMinutes || currentMinutes >= targetMinutes;
    }

    private void ExecuteAuthorityEvent(TimeEvents.EventType eventType)
    {
        if (!HasTimeAuthority) return;

        ExecuteEvent(eventType);
        BroadcastEventToRemote(eventType);
    }
    #endregion

    #region Network
    [PunRPC]
    private void RPC_RequestSkipToNextDay()
    {
        if (!HasTimeAuthority) return;
        ExecuteAuthoritySkipToNextDay();
    }

    [PunRPC]
    private void RPC_SyncTime(int day, int hour, int minute)
    {
        // 권한 주체가 아닌 클라이언트는 권한 주체 기준 시간으로 동기화한다.
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

    private void SyncToRemote()
    {
        if (!CanUseNetworkSync) return;

        photonView.RPC
        (
            nameof(RPC_SyncTime),
            RpcTarget.Others,
            CurrentDay,
            CurrentTime.Hour,
            CurrentTime.Minute
        );
    }

    private void BroadcastEventToRemote(TimeEvents.EventType eventType)
    {
        if (!CanUseNetworkSync) return;

        photonView.RPC(nameof(RPC_ExecuteEvent), RpcTarget.Others, (byte)eventType);
    }

    [PunRPC]
    private void RPC_ExecuteEvent(byte eventTypeByte)
    {
        if (HasTimeAuthority) return;
        ExecuteEvent((TimeEvents.EventType)eventTypeByte);
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