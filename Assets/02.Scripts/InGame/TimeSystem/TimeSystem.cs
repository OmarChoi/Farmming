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

    public void Initialize(TimeSettingSO timeSettings)
    {
        _timeSettings = timeSettings;
        TryCreateClock(true);
    }

    public void SkipToNextDay()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_RequestSkipToNextDay), RpcTarget.MasterClient);
            return;
        }

        ExecuteSkipToNextDay();
    }

    [PunRPC]
    private void RPC_RequestSkipToNextDay()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ExecuteSkipToNextDay();
    }

    private void ExecuteSkipToNextDay()
    {
        if (_clock == null || _timeSettings == null) return;

        _clock.SetTime(_clock.CurrentDay + 1, _timeSettings.WakeUpTime);
        _accumulatedGameMinutes = 0f;
        SyncState();
        SyncToRemote();
    }

    [PunRPC]
    private void RPC_SyncTime(int day, int hour, int minute, bool isDayTime, int elapsedDays)
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
        SyncState();
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
            CurrentTime.Minute,
            IsDayTime,
            ElapsedDays
        );
    }

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
        SyncState();
        return true;
    }

    private void SyncState()
    {
        int previousDay = TimeEvents.CurrentDay;
        bool wasDayTime = TimeEvents.IsDayTime;

        TimeEvents.UpdateState(CurrentDay, CurrentTime, IsDayTime, ElapsedDays);

        if (previousDay != CurrentDay)
        {
            TimeEvents.InvokeDayChanged(CurrentDay);
        }

        if (!wasDayTime && IsDayTime)
        {
            TimeEvents.InvokeDayStarted();
        }
        else if (wasDayTime && !IsDayTime)
        {
            TimeEvents.InvokeDayEnded();
        }
    }

    private void AdvanceOneMinute()
    {
        _clock.AdvanceMinutes(1);
        SyncState();
        TimeEvents.InvokeMinuteChanged(_clock.CurrentTime);
    }
}