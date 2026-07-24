using System.Globalization;
using System.Runtime.InteropServices;

namespace Distributor.Periods;

[StructLayout(LayoutKind.Auto)]
public readonly record struct PeriodDate : IComparable<PeriodDate>
{
    private readonly DateOnly _date;

    public PeriodDate(int year, int month)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(year);
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);

        _date = new DateOnly(year, month, day: 1);
    }

    public int Year => _date.Year;
    public int Month => _date.Month;

    public override string ToString()
    {
        return $"{Year}-{Month:D2}";
    }

    public static PeriodDate Parse(string value)
    {
        var date = DateOnly.ParseExact(value, "yyyy-MM", CultureInfo.InvariantCulture);

        return new PeriodDate(date.Year, date.Month);
    }

    public PeriodDate Next()
    {
        var date = _date.AddMonths(1);

        return new PeriodDate(date.Year, date.Month);
    }

    public int CompareTo(PeriodDate other)
    {
        return _date.CompareTo(other._date);
    }

    public static bool operator <(PeriodDate left, PeriodDate right)
    {
        return left._date < right._date;
    }

    public static bool operator >(PeriodDate left, PeriodDate right)
    {
        return left._date > right._date;
    }

    public static bool operator <=(PeriodDate left, PeriodDate right)
    {
        return left._date <= right._date;
    }

    public static bool operator >=(PeriodDate left, PeriodDate right)
    {
        return left._date >= right._date;
    }
}
