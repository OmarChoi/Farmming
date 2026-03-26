using System;
using UnityEngine;

[Serializable]
public struct GameTime : IEquatable<GameTime>, IComparable<GameTime>
{
    public const int HoursPerDay = 24;
    public const int MinutesPerHour = 60;
    public const int MinutesPerDay = HoursPerDay * MinutesPerHour;

    [SerializeField, Range(0, 24)] private int _hour;
    [SerializeField, Range(0, 59)] private int _minute;

    public int Hour => _hour;
    public int Minute => _minute;

    public int TotalMinutes => _hour == HoursPerDay ? MinutesPerDay : (_hour * MinutesPerHour) + _minute;

    public GameTime(int hour, int minute)
    {
        _hour = 0;
        _minute = 0;

        if (hour == HoursPerDay && minute == 0)
        {
            _hour = HoursPerDay;
            return;
        }

        int wrappedMinutes = WrapTotalMinutes((hour * MinutesPerHour) + minute);
        _hour = wrappedMinutes / MinutesPerHour;
        _minute = wrappedMinutes % MinutesPerHour;
    }

    public override string ToString()
    {
        return $"{Hour:00}시 {Minute:00}분";
    }

    public bool Equals(GameTime other)
    {
        return Hour == other.Hour && Minute == other.Minute;
    }

    public override bool Equals(object obj)
    {
        return obj is GameTime other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Hour, Minute);
    }

    public int CompareTo(GameTime other)
    {
        return TotalMinutes.CompareTo(other.TotalMinutes);
    }

    public GameTime NormalizeClock()
    {
        return FromWrappedMinutes(TotalMinutes);
    }

    public GameTime NormalizeBoundary()
    {
        return _hour == HoursPerDay ? new GameTime(HoursPerDay, 0) : FromWrappedMinutes(TotalMinutes);
    }

    public static int WrapTotalMinutes(int totalMinutes)
    {
        int wrappedMinutes = totalMinutes % MinutesPerDay;  // 24시간이 넘어가면 00시부터 시작
        return wrappedMinutes < 0 ? wrappedMinutes + MinutesPerDay : wrappedMinutes;
    }

    public static GameTime FromWrappedMinutes(int totalMinutes)
    {
        int wrappedMinutes = WrapTotalMinutes(totalMinutes);
        return new GameTime(wrappedMinutes / MinutesPerHour, wrappedMinutes % MinutesPerHour);
    }

    public static GameTime operator +(GameTime time, int minutes) => FromWrappedMinutes(time.TotalMinutes + minutes);
    public static GameTime operator -(GameTime time, int minutes) => FromWrappedMinutes(time.TotalMinutes - minutes);
    public static bool operator ==(GameTime left, GameTime right) => left.Equals(right);
    public static bool operator !=(GameTime left, GameTime right) => !left.Equals(right);
    public static bool operator <(GameTime left, GameTime right) => left.CompareTo(right) < 0;
    public static bool operator >(GameTime left, GameTime right) => left.CompareTo(right) > 0;
    public static bool operator <=(GameTime left, GameTime right) => left.CompareTo(right) <= 0;
    public static bool operator >=(GameTime left, GameTime right) => left.CompareTo(right) >= 0;
}