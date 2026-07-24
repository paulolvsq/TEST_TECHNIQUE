namespace Distributor.Plans;

public sealed class Shipment
{
    private Shipment() { }

    public Shipment(int periodId, int warehouseId, int storeId, int units)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(periodId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(units);

        PeriodId = periodId;
        WarehouseId = warehouseId;
        StoreId = storeId;
        Units = units;
    }

    public int Id { get; private init; }
    public int DistributionPlanId { get; private init; }
    public int PeriodId { get; private init; }
    public int WarehouseId { get; private init; }
    public int StoreId { get; private init; }
    public int Units { get; private init; }
}
