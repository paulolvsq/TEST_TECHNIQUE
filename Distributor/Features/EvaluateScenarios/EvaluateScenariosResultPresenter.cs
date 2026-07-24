using System.Collections.Immutable;
using Distributor.Application;

namespace Distributor.Features.EvaluateScenarios;

public sealed class EvaluateScenariosResultPresenter : IPresenter<EvaluateScenariosResult>
{
    private readonly TextWriter _writer;

    public EvaluateScenariosResultPresenter()
    {
        _writer = Console.Out;
    }

    public void Display(EvaluateScenariosResult result)
    {
        _writer.WriteLine();
        _writer.WriteLine($"=== Scenario evaluation ({result.Start} to {result.End}) ===");

        var periodCount = result.Scenarios[0].Periods.Length;

        for (var periodIndex = 0; periodIndex < periodCount; periodIndex++)
        {
            var periodDate = result.Scenarios[0].Periods[periodIndex].Date;
            var rankings = BuildRankings(result, scenario => scenario.Periods[periodIndex].TotalCost);

            DisplayRankings(periodDate.ToString(), rankings);
        }

        var totalRankings = BuildRankings(result, scenario => scenario.TotalCost);

        DisplayRankings("Total", totalRankings);

        _writer.WriteLine();
    }

    private static ImmutableArray<ScenarioRanking> BuildRankings(
        EvaluateScenariosResult result,
        Func<ScenarioResult, decimal> costSelector
    )
    {
        var baseCost = costSelector(result.Scenarios[0]);
        var scenarios = result.Scenarios.OrderBy(costSelector).ToImmutableArray();
        var rankings = scenarios.Select(scenario => BuildRanking(costSelector, scenario, baseCost));

        return rankings.ToImmutableArray();
    }

    private static ScenarioRanking BuildRanking(
        Func<ScenarioResult, decimal> costSelector,
        ScenarioResult scenario,
        decimal baseCost
    )
    {
        var cost = costSelector(scenario);
        var percentage = baseCost > 0 ? (cost - baseCost) / baseCost : 0m;

        return new ScenarioRanking
        {
            Name = scenario.Name,
            Cost = cost,
            Percentage = percentage,
        };
    }

    private void DisplayRankings(string name, ImmutableArray<ScenarioRanking> rankings)
    {
        _writer.WriteLine();
        _writer.WriteLine($"--- {name} ---");

        foreach (var ranking in rankings)
        {
            var cost = $"$ {ranking.Cost:N2}";
            var percentage = ranking.Percentage is 0m ? "" : $"  ({ranking.Percentage:+0.0%;-0.0%})";

            _writer.WriteLine($"  {ranking.Name, -40} {cost, 16}{percentage}");
        }
    }

    private sealed record ScenarioRanking
    {
        public required string Name { get; init; }
        public required decimal Cost { get; init; }
        public required decimal Percentage { get; init; }
    }
}
