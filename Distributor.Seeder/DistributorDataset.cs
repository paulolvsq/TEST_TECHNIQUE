using System.Collections.Immutable;

namespace Distributor.Seeder;

public sealed record DistributorDataset
{
    public required ImmutableArray<Warehouse> Warehouses { get; init; }
    public required ImmutableArray<Store> Stores { get; init; }
    public required ImmutableArray<Period> Periods { get; init; }
    public required ImmutableArray<WarehouseCapacity> Capacities { get; init; }
    public required ImmutableArray<StoreDemand> Demands { get; init; }
    public required ImmutableArray<RouteCost> Costs { get; init; }
    public required ImmutableArray<Scenario> Scenarios { get; init; }
    public required ImmutableArray<ScenarioWarehouseAdjustment> ScenarioWarehouseAdjustments { get; init; }
    public required ImmutableArray<ScenarioStoreAdjustment> ScenarioStoreAdjustments { get; init; }
}

public sealed record Warehouse(int Id, string Name);

public sealed record Store(int Id, string Name);

public sealed record Period(int Id, int Year, int Month);

public sealed record WarehouseCapacity(int PeriodId, int WarehouseId, int Units);

public sealed record StoreDemand(int PeriodId, int StoreId, int Units);

public sealed record RouteCost(int PeriodId, int WarehouseId, int StoreId, double UnitCost);

public sealed record Scenario(int Id, string Name);

public sealed record ScenarioWarehouseAdjustment(int ScenarioId, int WarehouseId, double Multiplier);

public sealed record ScenarioStoreAdjustment(int ScenarioId, int StoreId, double Multiplier);
