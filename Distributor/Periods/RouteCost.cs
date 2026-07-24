namespace Distributor.Periods;

public sealed class RouteCost
{
    private RouteCost() { }

    public RouteCost(int warehouseId, int storeId, double unitCost)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(unitCost);

        WarehouseId = warehouseId;
        StoreId = storeId;
        UnitCost = unitCost;
    }

    public int PeriodId { get; private init; }
    public int WarehouseId { get; private init; }
    public int StoreId { get; private init; }
    public double UnitCost { get; private init; }
}
