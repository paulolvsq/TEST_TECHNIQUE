using Distributor.Periods;
using Distributor.Scenarios;
using Distributor.Stores;
using Distributor.Warehouses;
using MathNet.Numerics.LinearAlgebra;

namespace Distributor.Features.EvaluateScenarios;

public interface IScenarioMatrixFactory
{
    Matrix<double> BuildCostMatrix(
        IReadOnlyList<Warehouse> warehouses,
        IReadOnlyList<Store> stores,
        IReadOnlyList<RouteCost> routeCosts
    );

    Matrix<double> BuildAdjustedDemandMatrix(
        IReadOnlyList<Store> stores,
        IReadOnlyList<StoreDemand> demands,
        IReadOnlyList<Scenario> scenarios
    );

    Matrix<double> BuildWarehouseMultiplierMatrix(
        IReadOnlyList<Warehouse> warehouses,
        IReadOnlyList<Scenario> scenarios
    );
}

public sealed class ScenarioMatrixFactory : IScenarioMatrixFactory
{
    public Matrix<double> BuildCostMatrix(
        IReadOnlyList<Warehouse> warehouses,
        IReadOnlyList<Store> stores,
        IReadOnlyList<RouteCost> routeCosts
    )
    {
        var warehouseIndices = BuildIndicesDictionary(warehouses, warehouse => warehouse.Id);
        var storeIndices = BuildIndicesDictionary(stores, store => store.Id);
        var matrix = Matrix<double>.Build.Dense(warehouses.Count, stores.Count);

        foreach (var routeCost in routeCosts)
        {
            var warehouseIndex = warehouseIndices[routeCost.WarehouseId];
            var storeIndex = storeIndices[routeCost.StoreId];
            matrix[storeIndex, warehouseIndex] = routeCost.UnitCost;
        }

        return matrix;
    }

    public Matrix<double> BuildAdjustedDemandMatrix(
        IReadOnlyList<Store> stores,
        IReadOnlyList<StoreDemand> demands,
        IReadOnlyList<Scenario> scenarios
    )
    {
        var storeIndices = BuildIndicesDictionary(stores, store => store.Id);
        var demandByStore = new double[stores.Count];

        foreach (var demand in demands)
        {
            var storeIndex = storeIndices[demand.StoreId];
            demandByStore[storeIndex] = demand.Units;
        }

        var matrix = Matrix<double>.Build.Dense(stores.Count, scenarios.Count);

        for (var scenarioIndex = 0; scenarioIndex < scenarios.Count; scenarioIndex++)
        {
            var storeMultipliers = scenarios[scenarioIndex]
                .StoreAdjustments.ToDictionary(adjustment => adjustment.StoreId, adjustment => adjustment.Multiplier);

            for (var storeIndex = 0; storeIndex < stores.Count; storeIndex++)
            {
                var storeId = stores[storeIndex].Id;
                var multiplier = storeMultipliers.GetValueOrDefault(storeId, 1.0);
                matrix[storeIndex, scenarioIndex] = demandByStore[storeIndex] * multiplier;
            }
        }

        return matrix;
    }

    public Matrix<double> BuildWarehouseMultiplierMatrix(
        IReadOnlyList<Warehouse> warehouses,
        IReadOnlyList<Scenario> scenarios
    )
    {
        var warehouseIndices = BuildIndicesDictionary(warehouses, warehouse => warehouse.Id);
        var matrix = Matrix<double>.Build.Dense(warehouses.Count, scenarios.Count);

        for (var scenarioIndex = 0; scenarioIndex < scenarios.Count; scenarioIndex++)
        {
            var scenario = scenarios[scenarioIndex];
            var warehouseMultipliers = scenario.WarehouseAdjustments.ToDictionary(
                adjustment => adjustment.WarehouseId,
                adjustment => adjustment.Multiplier
            );

            for (var warehouseIndex = 0; warehouseIndex < warehouses.Count; warehouseIndex++)
            {
                var warehouseId = warehouses[warehouseIndex].Id;
                matrix[warehouseIndex, scenarioIndex] = warehouseMultipliers.TryGetValue(
                    warehouseId,
                    out var multiplier
                )
                    ? multiplier
                    : 0.0;
            }
        }

        return matrix;
    }

    private static Dictionary<int, int> BuildIndicesDictionary<T>(IReadOnlyList<T> items, Func<T, int> keySelector)
    {
        var indices = new Dictionary<int, int>(items.Count);

        for (var index = 0; index < items.Count; index++)
        {
            var key = keySelector(items[index]);
            indices[key] = index;
        }

        return indices;
    }
}
