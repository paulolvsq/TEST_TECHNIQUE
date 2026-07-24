using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Parsing;
using Distributor.Application;
using Distributor.Periods;

namespace Distributor.Features.EvaluateScenarios;

public sealed record EvaluateScenariosOptions : IRequestOptions<EvaluateScenariosQuery>
{
    public string Name => "evaluate";

    public string Description => "Evaluate maximum distribution cost under pricing scenarios.";

    public Option<PeriodDate> Start { get; } =
        new("--start", "-s") { Description = "Start period (YYYY-MM).", CustomParser = ParsePeriodDate };

    public Option<PeriodDate> End { get; } =
        new("--end", "-e") { Description = "End period (YYYY-MM).", CustomParser = ParsePeriodDate };

    public Option<int[]> Scenarios { get; } =
        new("--scenarios")
        {
            Description = "Scenario ids to compare against base scenario.",
            AllowMultipleArgumentsPerToken = true,
        };

    public IEnumerable<Option> Enumerate()
    {
        yield return Start;
        yield return End;
        yield return Scenarios;
    }

    public EvaluateScenariosQuery Parse(ParseResult result)
    {
        var start = result.GetRequiredValue(Start);
        var end = result.GetRequiredValue(End);
        var scenarioIds = result.GetValue(Scenarios) ?? [];

        if (end < start)
        {
            throw new InvalidOperationException("End period must not be before start period.");
        }

        return new EvaluateScenariosQuery
        {
            Start = start,
            End = end,
            ScenarioIds = scenarioIds.ToImmutableArray(),
        };
    }

    private static PeriodDate ParsePeriodDate(ArgumentResult result)
    {
        return PeriodDate.Parse(result.Tokens.Single().Value);
    }
}
