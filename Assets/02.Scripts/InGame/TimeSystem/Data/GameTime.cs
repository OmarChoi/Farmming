using System;
using UnityEngine;

[Serializable]
public struct GameTime : IEquatable<GameTime>, IComparable<GameTime>
{
    public const int HoursPerDay = 24;
    public const int MinutesPerHour = 60;
    public const int MinutesPerDay = HoursPerDay * MinutesPerHour;

    private static readonly int[] DaysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
    public const int DaysPerYear = 365;

    [SerializeField, Range(0, 24)] private int _hour;
    [SerializeField, Range(0, 59)] private int _minute;

    public int Hour => _hour;
    public int Minute => _minute;
    public int Hour12 => _hour % 12 == 0 ? 12 : _hour % 12;
    public bool IsAM => _hour < 12;

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

    public string To12HourString()
    {
        string period = IsAM ? "오전" : "오후";
        return $"{period} {Hour12}:{Minute:00}";
    }

    /// <summary>
    /// 누적 일수(1부터 시작)를 "N월 M일" 형식의 문자열로 변환한다.
    /// UI 계층이 게임 달력 규칙을 각자 구현하지 않도록 TimeSystem 측에서 제공한다.
    /// </summary>
    public static string FormatMonthDay(int cumulativeDay)
    {
        if (cumulativeDay < 1) return "-월 -일";
        ToMonthDay(cumulativeDay, out int month, out int day);
        return $"{month}월 {day}일";
    }

    /// <summary>
    /// 누적 일수(1부터 시작)를 실제 달력 기준의 (월, 일) 쌍으로 분해한다.
    /// 365일을 넘기면 연도 주기로 접혀 다음 해 1월 1일부터 다시 센다.
    /// UI 외 도메인 코드가 포맷 문자열 파싱 없이 월/일을 직접 조회할 수 있도록 공개 API로 둔다.
    /// </summary>
    public static void ToMonthDay(int cumulativeDay, out int month, out int day)
    {
        if (cumulativeDay < 1)
        {
            month = 0;
            day = 0;
            return;
        }

        int dayOfYear = (cumulativeDay - 1) % DaysPerYear;
        for (int m = 0; m < DaysInMonth.Length; m++)
        {
            if (dayOfYear < DaysInMonth[m])
            {
                month = m + 1;
                day = dayOfYear + 1;
                return;
            }
            dayOfYear -= DaysInMonth[m];
        }

        month = 12;
        day = DaysInMonth[11];
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