namespace Distributor.Periods;

public sealed class StoreDemand
{
    private StoreDemand() { }

    public StoreDemand(int storeId, int units)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(units);

        StoreId = storeId;
        Units = units;
    }

    public int PeriodId { get; private init; }
    public int StoreId { get; private init; }
    public int Units { get; private init; }
}
