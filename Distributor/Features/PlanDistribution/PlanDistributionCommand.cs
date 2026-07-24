using Distributor.Application;
using Distributor.Periods;

namespace Distributor.Features.PlanDistribution;

public sealed record PlanDistributionCommand : IRequest<PlanDistributionResult>
{
    public required PeriodDate Start { get; init; }
    public required PeriodDate End { get; init; }
}
