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
    private GameTime CurrentTime => _clock?.CurrentTime ?? default;
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
        }
    }
    
    #endregion

    #region Master Client Only

    private void ExecuteMasterClientSkipToNextDay()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        ExecuteSkipToNextDay();
        SyncToRemote();
        
        ExecuteMasterClientEvent(TimeEvents.EventType.DayEnd);
        ExecuteMasterClientEvent(TimeEvents.EventType.DayChange);
        ExecuteMasterClientEvent(TimeEvents.EventType.DayStart);
    }
    
    private bool HasPassedTime(GameTime previous, GameTime current, GameTime target)
    {
        return previous < target && current >= target;
    }
    
    private void CheckDayEvent(int prevDay, GameTime prevTime)
    {
        // 이전 시간과 현재 시간 사이에 DayStart 시간이 존재하면 DayStart 이벤트 호출
        bool passedDayStart = HasPassedTime(prevTime, _clock.CurrentTime, _timeSettings.DayStartTime);
        if (passedDayStart)
        {
            ExecuteMasterClientEvent(TimeEvents.EventType.DayStart);
        }
        
        // 이전 시간과 날짜가 다르면 DayChanged 이벤트 호출
        bool dayChanged = prevDay != _clock.CurrentDay;
        bool dayEndsAtMidnight = _timeSettings.DayEndTime.Equals(new GameTime(0, 0));
        if (dayChanged)
        {
            if (dayEndsAtMidnight)
            {
                ExecuteMasterClientEvent(TimeEvents.EventType.DayEnd);
                ExecuteMasterClientEvent(TimeEvents.EventType.DayChange);
                return;
            }

            ExecuteMasterClientEvent(TimeEvents.EventType.DayChange);
        }
        
        // 이전 시간과 현재 시간 사이에 DayEnd 시간이 존재하면 DayEnd 이벤트 호출
        bool passedDayEnd = HasPassedTime(prevTime, _clock.CurrentTime, _timeSettings.DayEndTime);
        if (passedDayEnd)
        {
            ExecuteMasterClientEvent(TimeEvents.EventType.DayEnd);
        }
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

        photonView.RPC(nameof(RPC_ExecuteEvent), RpcTarget.Others, eventType);
    }

    [PunRPC]
    private void RPC_ExecuteEvent(TimeEvents.EventType eventType)
    {
        if (PhotonNetwork.IsMasterClient) return;
        ExecuteEvent(eventType);
    }
    #endregion
}
