using System;

public class GameClock
{
    private readonly int _dayStartMinutes;
    private readonly int _dayEndMinutes;

    public int CurrentDay { get; private set; }
    public GameTime CurrentTime { get; private set; }
    public bool IsDayTime { get; private set; }
    public int ElapsedDays => CurrentDay - 1;

    public GameClock(int defaultDay, GameTime defaultTime, GameTime dayStartTime, GameTime dayEndTime)
    {
        CurrentDay = Math.Max(1, defaultDay);
        _dayStartMinutes = NormalizeBoundaryMinutes(dayStartTime);
        _dayEndMinutes = NormalizeBoundaryMinutes(dayEndTime);
        CurrentTime = GameTime.FromWrappedMinutes(defaultTime.TotalMinutes);
        IsDayTime = EvaluateIsDayTime(CurrentTime.TotalMinutes);
    }

    public void SetTime(int day, GameTime time)
    {
        CurrentDay = Math.Max(1, day);
        CurrentTime = GameTime.FromWrappedMinutes(time.TotalMinutes);
        IsDayTime = EvaluateIsDayTime(CurrentTime.TotalMinutes);
    }

    public void AdvanceMinutes(int minutes)
    {
        if (minutes <= 0)
        {
            return;
        }

        int totalMinutes = CurrentTime.TotalMinutes + minutes;
        CurrentDay += totalMinutes / GameTime.MinutesPerDay;
        CurrentTime = GameTime.FromWrappedMinutes(totalMinutes);
        IsDayTime = EvaluateIsDayTime(CurrentTime.TotalMinutes);
    }

    private static int NormalizeBoundaryMinutes(GameTime time)
    {
        return time.TotalMinutes == GameTime.MinutesPerDay
            ? GameTime.MinutesPerDay
            : GameTime.WrapTotalMinutes(time.TotalMinutes);
    }

    private bool EvaluateIsDayTime(int currentMinutes)
    {
        if (_dayStartMinutes == _dayEndMinutes)
        {
            return false;
        }

        if (_dayEndMinutes == GameTime.MinutesPerDay)
        {
            return currentMinutes >= _dayStartMinutes;
        }

        if (_dayStartMinutes < _dayEndMinutes)
        {
            return currentMinutes >= _dayStartMinutes && currentMinutes < _dayEndMinutes;
        }

        return currentMinutes >= _dayStartMinutes || currentMinutes < _dayEndMinutes;
    }
}
