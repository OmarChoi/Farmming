using System;

public readonly struct Currency
{
    public readonly int Value;
    private const int MaxValue = 999999999;

    public Currency(int value)
    {
        Value = Math.Clamp(value, 0, MaxValue);
    }

    public override string ToString() => Value.ToString("N0");
    public static Currency operator +(Currency a, Currency b) => new Currency(a.Value + b.Value);
    public static Currency operator -(Currency a, Currency b) => new Currency(a.Value - b.Value);
    public static bool operator >=(Currency a, Currency b) => a.Value >= b.Value;
    public static bool operator <=(Currency a, Currency b) => a.Value <= b.Value;
    public static bool operator >(Currency a, Currency b) => a.Value > b.Value;
    public static bool operator <(Currency a, Currency b) => a.Value < b.Value;
    public static explicit operator Currency(int value) => new Currency(value);
    public static explicit operator int(Currency currency) => currency.Value;
}
