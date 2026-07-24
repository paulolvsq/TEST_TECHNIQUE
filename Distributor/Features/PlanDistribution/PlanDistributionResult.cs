using System.Collections.Immutable;
using Distributor.Periods;

namespace Distributor.Features.PlanDistribution;

public sealed record PlanDistributionResult
{
    public required int DistributionPlanId { get; init; }
    public required PeriodDate Start { get; init; }
    public required PeriodDate End { get; init; }
    public required decimal TotalCost { get; init; }
    public required ImmutableArray<PeriodResult> Periods { get; init; }
}

public sealed record PeriodResult
{
    public required PeriodDate Date { get; init; }
    public required decimal TotalCost { get; init; }
    public required ImmutableArray<ShipmentResult> Shipments { get; init; }
}

public sealed record ShipmentResult
{
    public required string Warehouse { get; init; }
    public required string Store { get; init; }
    public required int Units { get; init; }
    public required decimal UnitCost { get; init; }
    public required decimal Cost { get; init; }
}
