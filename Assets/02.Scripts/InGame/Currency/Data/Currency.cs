using System;

public readonly struct Currency
{
    public readonly double Value;

    public Currency(double value)
    {
        Value = Math.Max(0, value);
    }

    public override string ToString() => Value.ToString("N0");
    public static Currency operator +(Currency a, Currency b) => new Currency(a.Value + b.Value);
    public static Currency operator -(Currency a, Currency b) => new Currency(a.Value - b.Value);
    public static bool operator >=(Currency a, Currency b) => a.Value >= b.Value;
    public static bool operator <=(Currency a, Currency b) => a.Value <= b.Value;
    public static bool operator >(Currency a, Currency b) => a.Value > b.Value;
    public static bool operator <(Currency a, Currency b) => a.Value < b.Value;
    public static explicit operator Currency(double value) => new Currency(value);
    public static explicit operator double(Currency currency) => currency.Value;
}
