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

    #region Lifecycle
    private void Awake()
    {
        TryCreateClock();
    }

    private void Update()
    {
        Tick(Time.deltaTime);

        // Master Client 시간으로 동기화
        if (!PhotonNetwork.IsMasterClient) return;

        _syncTimer += Time.deltaTime;
        if (!(_syncTimer >= SyncInterval)) return;

        _syncTimer = 0f;
        SyncToRemote();
    }
    #endregion

    #region Public API
    public void SkipToNextDay()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_RequestSkipToNextDay), RpcTarget.MasterClient);
            return;
        }

        ExecuteMasterClientSkipToNextDay();
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

        // Master Client만 이벤트 발생 여부를 확인
        if (!PhotonNetwork.IsMasterClient) return;
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

    #region Master Client Only
    private void ExecuteMasterClientSkipToNextDay()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int prevDay = CurrentDay;
        GameTime prevTime = CurrentTime;

        ExecuteSkipToNextDay();
        SyncToRemote();

        CheckDayEvent(prevDay, prevTime);
    }

    private void CheckDayEvent(int prevDay, GameTime prevTime)
    {
        bool dayChanged = prevDay != _clock.CurrentDay;

        if (HasCrossed(prevTime, _timeSettings.SunriseTime, dayChanged)) ExecuteMasterClientEvent(TimeEvents.EventType.SunRise);
        if (HasCrossed(prevTime, _timeSettings.SunsetTime, dayChanged)) ExecuteMasterClientEvent(TimeEvents.EventType.SunSet);
        if (HasCrossed(prevTime, _timeSettings.DayEndTime, dayChanged)) ExecuteMasterClientEvent(TimeEvents.EventType.DayEnd);

        if (dayChanged) ExecuteMasterClientEvent(TimeEvents.EventType.DayChange);
        if (HasCrossed(prevTime, _timeSettings.DayStartTime, dayChanged)) ExecuteMasterClientEvent(TimeEvents.EventType.DayStart);
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

        // 날짜가 바뀐 경우, 두 구간을 합쳐서 판정:
        //   1) prevMinutes < targetMinutes  → 이전 날에 아직 지나지 않은 이벤트 (예: 23:59에서 자정을 넘긴 경우 23:55 이벤트)
        //   2) currentMinutes >= targetMinutes → 새 날에 이미 도달한 이벤트 (예: SkipToNextDay로 07:00에 도착 시 06:00 이벤트)
        // 둘 다 false인 경우(prevMinutes >= target AND currentMinutes < target)는
        // "이전 날에 이미 처리됐고 새 날에는 아직 안 된" 이벤트이므로 정확히 제외됨
        return prevMinutes < targetMinutes || currentMinutes >= targetMinutes;
    }

    private void ExecuteMasterClientEvent(TimeEvents.EventType eventType)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        ExecuteEvent(eventType);
        BroadcastEventToRemote(eventType);
    }
    #endregion

    #region Network
    [PunRPC]
    private void RPC_RequestSkipToNextDay()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ExecuteMasterClientSkipToNextDay();
    }

    // Master Client가 아닌 Client들이 Master Client 기준 시간으로 세팅하는 함수
    [PunRPC]
    private void RPC_SyncTime(int day, int hour, int minute)
    {
        if (PhotonNetwork.IsMasterClient) return;

        if (_clock == null)
        {
            if (!TryCreateClock())
            {
                Debug.LogError("TimeSystem: TryCreateClock() failed");
                return;
            }
        }

        _clock.SetTime(day, new GameTime(hour, minute));
        _accumulatedGameMinutes = 0f;
        SyncLocalState();
    }

    private void SyncToRemote()
    {
        if (!PhotonNetwork.InRoom) return;

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
        if (!PhotonNetwork.InRoom) return;

        photonView.RPC(nameof(RPC_ExecuteEvent), RpcTarget.Others, (byte)eventType);
    }

    [PunRPC]
    private void RPC_ExecuteEvent(byte eventTypeByte)
    {
        if (PhotonNetwork.IsMasterClient) return;
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