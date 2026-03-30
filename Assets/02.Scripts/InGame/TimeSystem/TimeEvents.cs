using System;

public static class TimeEvents
{
    public static int CurrentDay { get; private set; }
    public static GameTime CurrentTime { get; private set; }
    public static bool IsDayTime { get; private set; }
    public static int ElapsedDays { get; private set; }

    public static event Action<GameTime> OnMinuteChanged;
    public static event Action<int> OnDayChanged;
    public static event Action OnDayStarted;
    public static event Action OnDayEnded;

    internal static void UpdateState(int day, GameTime time, bool isDayTime, int elapsedDays)
    {
        CurrentDay = day;
        CurrentTime = time;
        IsDayTime = isDayTime;
        ElapsedDays = elapsedDays;
    }

    internal static void InvokeMinuteChanged(GameTime time) => OnMinuteChanged?.Invoke(time);
    internal static void InvokeDayChanged(int day) => OnDayChanged?.Invoke(day);
    internal static void InvokeDayStarted() => OnDayStarted?.Invoke();
    internal static void InvokeDayEnded() => OnDayEnded?.Invoke();
}
