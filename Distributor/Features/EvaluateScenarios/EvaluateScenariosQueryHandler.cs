using System.Collections.Immutable;
using Distributor.Application;
using Distributor.Matrices;
using Distributor.Periods;
using Distributor.Scenarios;
using Distributor.Stores;
using Distributor.Warehouses;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;

namespace Distributor.Features.EvaluateScenarios;

public sealed class EvaluateScenariosQueryHandler : IRequestHandler<EvaluateScenariosQuery, EvaluateScenariosResult>
{
    private readonly IPeriodRepository _periodRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IScenarioRepository _scenarioRepository;
    private readonly IScenarioMatrixFactory _matrixFactory;
    private readonly IMatrixMultiplier _matrixMultiplier;
    private readonly ILogger<EvaluateScenariosQueryHandler> _logger;

    public EvaluateScenariosQueryHandler(
        IPeriodRepository periodRepository,
        IWarehouseRepository warehouseRepository,
        IStoreRepository storeRepository,
        IScenarioRepository scenarioRepository,
        IScenarioMatrixFactory matrixFactory,
        IMatrixMultiplier matrixMultiplier,
        ILogger<EvaluateScenariosQueryHandler> logger
    )
    {
        _periodRepository = periodRepository;
        _warehouseRepository = warehouseRepository;
        _storeRepository = storeRepository;
        _scenarioRepository = scenarioRepository;
        _matrixFactory = matrixFactory;
        _matrixMultiplier = matrixMultiplier;
        _logger = logger;
    }

    public async Task<EvaluateScenariosResult> HandleAsync(
        EvaluateScenariosQuery query,
        CancellationToken token = default
    )
    {
        _logger.LogInformation("Evaluating scenarios from {Start} to {End}.", query.Start, query.End);

        var periods = await GetPeriodsAsync(query, token).ConfigureAwait(false);
        var warehouses = await GetWarehousesAsync(periods, token).ConfigureAwait(false);
        var stores = await GetStoresAsync(periods, token).ConfigureAwait(false);
        var scenarios = await GetScenariosAsync(query, token).ConfigureAwait(false);

        var periodCostsByScenario = await EvaluatePeriodsAsync(periods, warehouses, stores, scenarios, token)
            .ConfigureAwait(false);

        var scenarioResults = BuildScenarioResults(scenarios, periodCostsByScenario);

        return new EvaluateScenariosResult
        {
            Start = query.Start,
            End = query.End,
            Scenarios = scenarioResults,
        };
    }

    private async Task<List<Period>> GetPeriodsAsync(EvaluateScenariosQuery query, CancellationToken token)
    {
        var periods = await _periodRepository.GetPeriodsAsync(query.Start, query.End, token).ConfigureAwait(false);

        if (periods.Count is 0)
        {
            throw new InvalidOperationException("No periods found in the specified range.");
        }

        return periods;
    }

    private async Task<List<Warehouse>> GetWarehousesAsync(List<Period> periods, CancellationToken token)
    {
	// correction : ajout de Distinct
        var ids = periods.SelectMany(period => period.Costs).Select(cost => cost.WarehouseId).Distinct();

        return await _warehouseRepository.GetWarehousesAsync(ids, token).ConfigureAwait(false);
    }

    private async Task<List<Store>> GetStoresAsync(List<Period> periods, CancellationToken token)
    {
	// correction : ajout de Distinct
	var ids = periods.SelectMany(period => period.Costs).Select(cost => cost.StoreId).Distinct();

        return await _storeRepository.GetStoresAsync(ids, token).ConfigureAwait(false);
    }

    private async Task<List<Scenario>> GetScenariosAsync(EvaluateScenariosQuery query, CancellationToken token)
    {
        var baseScenario = new Scenario("Base scenario", [], []);

        if (query.ScenarioIds.IsEmpty)
        {
            return [baseScenario];
        }

        var scenarios = await _scenarioRepository.GetScenariosAsync(query.ScenarioIds, token).ConfigureAwait(false);

        if (scenarios.Count is 0)
        {
            throw new InvalidOperationException("No scenarios found.");
        }

        scenarios.Insert(0, baseScenario);

        return scenarios;
    }

    private async Task<Dictionary<int, List<ScenarioPeriodResult>>> EvaluatePeriodsAsync(
        List<Period> periods,
        List<Warehouse> warehouses,
        List<Store> stores,
        List<Scenario> scenarios,
        CancellationToken token
    )
    {
        var warehouseMultipliers = _matrixFactory.BuildWarehouseMultiplierMatrix(warehouses, scenarios);
        var spanSize = ComputeSpanSize(warehouses.Count, scenarios.Count);

        var periodResultsByScenarioId = scenarios.ToDictionary(
            scenario => scenario.Id,
            _ => new List<ScenarioPeriodResult>(periods.Count)
        );

	// correction : suppression de la déclaration de costMatrix ici
        //var costMatrix = _matrixFactory.BuildCostMatrix(warehouses, stores, periods[0].Costs);

        foreach (var period in periods)
        {

	    // correction : déclaration dans la boucle de la matrice de coûts pour prendre en compte les variations
	    // de coûts spécifiques à la période en cours
	    var costMatrix = _matrixFactory.BuildCostMatrix(warehouses, stores, period.Costs);
            var adjustedDemand = _matrixFactory.BuildAdjustedDemandMatrix(stores, period.Demands, scenarios);

            var product = await _matrixMultiplier
                .MultiplyAsync(costMatrix, adjustedDemand, spanSize, token)
                .ConfigureAwait(false);

            var periodDate = new PeriodDate(period.Year, period.Month);

            for (var scenarioIndex = 0; scenarioIndex < scenarios.Count; scenarioIndex++)
            {
                periodResultsByScenarioId[scenarios[scenarioIndex].Id]
                    .Add(
                        BuildScenarioPeriodResult(periodDate, scenarioIndex, warehouses, product, warehouseMultipliers)
                    );
            }

            _logger.LogInformation(
                "Evaluated period {Period} across {ScenarioCount} scenarios.",
                periodDate,
                scenarios.Count
            );
        }

        return periodResultsByScenarioId;
    }

    private static int ComputeSpanSize(int rows, int columns)
    {
        var elements = rows * columns;
        var elementsPerProcessor = (double)elements / Environment.ProcessorCount;
        var spanSize = Math.Sqrt(elementsPerProcessor);

        return (int)Math.Max(1, spanSize);
    }

    private static ScenarioPeriodResult BuildScenarioPeriodResult(
        PeriodDate periodDate,
        int scenarioIndex,
        List<Warehouse> warehouses,
        Matrix<double> product,
        Matrix<double> warehouseMultipliers
    )
    {
        var warehouseCostResults = new List<ScenarioWarehouseCostResult>(warehouses.Count);
        var periodCost = 0m;

        for (var index = 0; index < warehouses.Count; index++)
        {
            var warehouse = warehouses[index];
            var baseCost = product[index, scenarioIndex];
            var multiplier = warehouseMultipliers[index, scenarioIndex];
            var cost = (decimal)(baseCost * multiplier);
            var result = new ScenarioWarehouseCostResult { Warehouse = warehouse.Name, Cost = cost };

            warehouseCostResults.Add(result);
            periodCost += cost;
        }

        return new ScenarioPeriodResult
        {
            Date = periodDate,
            TotalCost = periodCost,
            Warehouses = warehouseCostResults.ToImmutableArray(),
        };
    }

    private static ImmutableArray<ScenarioResult> BuildScenarioResults(
        List<Scenario> scenarios,
        Dictionary<int, List<ScenarioPeriodResult>> periodResultsByScenarioId
    )
    {
        var scenarioResults = new List<ScenarioResult>(scenarios.Count);

        foreach (var scenario in scenarios)
        {
            var periodResults = periodResultsByScenarioId[scenario.Id];
            var totalCost = periodResults.Sum(period => period.TotalCost);

            scenarioResults.Add(
                new ScenarioResult
                {
                    Name = scenario.Name,
                    TotalCost = totalCost,
                    Periods = periodResults.ToImmutableArray(),
                }
            );
        }

        return scenarioResults.ToImmutableArray();
    }
}
