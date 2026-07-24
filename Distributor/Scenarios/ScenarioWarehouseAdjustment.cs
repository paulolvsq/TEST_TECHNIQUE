namespace Distributor.Scenarios;

public sealed class ScenarioWarehouseAdjustment
{
    private ScenarioWarehouseAdjustment() { }

    public ScenarioWarehouseAdjustment(int warehouseId, double multiplier)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(multiplier);

        WarehouseId = warehouseId;
        Multiplier = multiplier;
    }

    public int ScenarioId { get; private init; }
    public int WarehouseId { get; private init; }
    public double Multiplier { get; private init; }
}
