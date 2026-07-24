using System.Collections.Immutable;
using Distributor.Periods;

namespace Distributor.Features.EvaluateScenarios;

public sealed record EvaluateScenariosResult
{
    public required PeriodDate Start { get; init; }
    public required PeriodDate End { get; init; }
    public required ImmutableArray<ScenarioResult> Scenarios { get; init; }
}

public sealed record ScenarioResult
{
    public required string Name { get; init; }
    public required decimal TotalCost { get; init; }
    public required ImmutableArray<ScenarioPeriodResult> Periods { get; init; }
}

public sealed record ScenarioPeriodResult
{
    public required PeriodDate Date { get; init; }
    public required decimal TotalCost { get; init; }
    public required ImmutableArray<ScenarioWarehouseCostResult> Warehouses { get; init; }
}

public sealed record ScenarioWarehouseCostResult
{
    public required string Warehouse { get; init; }
    public required decimal Cost { get; init; }
}
