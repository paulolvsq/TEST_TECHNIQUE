using System.CommandLine;
using System.CommandLine.Parsing;
using Distributor.Application;
using Distributor.Periods;

namespace Distributor.Features.PlanDistribution;

public sealed record PlanDistributionOptions : IRequestOptions<PlanDistributionCommand>
{
    public string Name => "plan";

    public string Description => "Plan optimal distribution for a period range.";

    public Option<PeriodDate> Start { get; } =
        new("--start", "-s") { Description = "Start period (YYYY-MM).", CustomParser = ParsePeriodDate };

    public Option<PeriodDate> End { get; } =
        new("--end", "-e") { Description = "End period (YYYY-MM).", CustomParser = ParsePeriodDate };

    public IEnumerable<Option> Enumerate()
    {
        yield return Start;
        yield return End;
    }

    public PlanDistributionCommand Parse(ParseResult result)
    {
        var start = result.GetRequiredValue(Start);
        var end = result.GetRequiredValue(End);

        if (end < start)
        {
            throw new InvalidOperationException("End period must not be before start period.");
        }

        return new PlanDistributionCommand { Start = start, End = end };
    }

    private static PeriodDate ParsePeriodDate(ArgumentResult result)
    {
        return PeriodDate.Parse(result.Tokens.Single().Value);
    }
}
