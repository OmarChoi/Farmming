using System;

public static class TimeEvents
{
    public enum EventType : byte
    {
        DayChange,
        DayStart,
        DayEnd,
        SunRise,
        SunSet,
    }
    
    public static int CurrentDay { get; private set; }
    public static GameTime CurrentTime { get; private set; }
    public static bool IsDayTime { get; private set; }
    public static int ElapsedDays { get; private set; }

    public static event Action<GameTime> OnMinuteChanged;

    // 00시 기준 날짜가 바뀌는 것을 인지하기 위한 이벤트 입니다.
    public static event Action OnNetDayChanged;
    
    // 하루의 시작 기준 시간이 됐는지 알리기 위한 이벤트입니다.
    public static event Action OnNetDayStarted;
    
    // 하루가 끝나는 기준 시간이 됐는지 알리기 위한 이벤트입니다.
    public static event Action OnNetDayEnded;
    public static event Action OnNetSunRise;
    public static event Action OnNetSunSet;

    internal static void UpdateState(int day, GameTime time, bool isDayTime, int elapsedDays)
    {
        CurrentDay = day;
        CurrentTime = time;
        IsDayTime = isDayTime;
        ElapsedDays = elapsedDays;
    }
    internal static void InvokeMinuteChanged(GameTime time) => OnMinuteChanged?.Invoke(time);
    internal static void InvokeNetDayChanged() => OnNetDayChanged?.Invoke();
    internal static void InvokeNetDayStarted() => OnNetDayStarted?.Invoke();
    internal static void InvokeNetDayEnded() => OnNetDayEnded?.Invoke();
    internal static void InvokeNetSunRise() => OnNetSunRise?.Invoke();
    internal static void InvokeNetSunSet() => OnNetSunSet?.Invoke();
}
