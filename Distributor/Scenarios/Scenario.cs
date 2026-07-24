namespace Distributor.Scenarios;

public sealed class Scenario
{
    private readonly List<ScenarioWarehouseAdjustment> _warehouseAdjustments = [];
    private readonly List<ScenarioStoreAdjustment> _storeAdjustments = [];

    private Scenario() { }

    public Scenario(
        string name,
        IReadOnlyList<ScenarioWarehouseAdjustment> warehouseAdjustments,
        IReadOnlyList<ScenarioStoreAdjustment> storeAdjustments
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(warehouseAdjustments);
        ArgumentNullException.ThrowIfNull(storeAdjustments);

        Name = name;
        _warehouseAdjustments = warehouseAdjustments.ToList();
        _storeAdjustments = storeAdjustments.ToList();
    }

    public int Id { get; private init; }
    public string Name { get; private init; } = null!;
    public IReadOnlyList<ScenarioWarehouseAdjustment> WarehouseAdjustments => _warehouseAdjustments;
    public IReadOnlyList<ScenarioStoreAdjustment> StoreAdjustments => _storeAdjustments;
}
