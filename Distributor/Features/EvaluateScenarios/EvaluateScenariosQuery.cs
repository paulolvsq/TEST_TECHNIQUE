using System.Collections.Immutable;
using Distributor.Application;
using Distributor.Periods;

namespace Distributor.Features.EvaluateScenarios;

public sealed record EvaluateScenariosQuery : IRequest<EvaluateScenariosResult>
{
    public required PeriodDate Start { get; init; }
    public required PeriodDate End { get; init; }
    public required ImmutableArray<int> ScenarioIds { get; init; }
}
