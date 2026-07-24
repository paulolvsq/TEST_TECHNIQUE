namespace Distributor.Periods;

public sealed class WarehouseCapacity
{
    private WarehouseCapacity() { }

    public WarehouseCapacity(int warehouseId, int units)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(units);

        WarehouseId = warehouseId;
        Units = units;
    }

    public int PeriodId { get; private init; }
    public int WarehouseId { get; private init; }
    public int Units { get; private init; }
}
