using Distributor.Data;
using Distributor.Periods;
using Distributor.Scenarios;
using Distributor.Stores;
using Distributor.Warehouses;

namespace Distributor.Tests.Data;

internal static class TestDataSeeder
{
    public static async Task SeedWarehousesAsync(DistributorDatabaseContext context)
    {
        context.Warehouses.AddRange(
            new Warehouse(1, "Montréal"),
            new Warehouse(2, "Toronto"),
            new Warehouse(3, "Calgary"),
            new Warehouse(4, "Vancouver"),
            new Warehouse(5, "Winnipeg")
        );

        await context.SaveChangesAsync();
    }

    public static async Task SeedStoresAsync(DistributorDatabaseContext context)
    {
        context.Stores.AddRange(
            new Store(1, "Montréal"),
            new Store(2, "Toronto"),
            new Store(3, "Ottawa"),
            new Store(4, "Halifax"),
            new Store(5, "Regina"),
            new Store(6, "Edmonton"),
            new Store(7, "Vancouver"),
            new Store(8, "Victoria")
        );

        await context.SaveChangesAsync();
    }

    public static async Task SeedPeriodsAsync(DistributorDatabaseContext context)
    {
        double[][] costs =
        [
            [0.50, 2.80, 1.20, 4.80, 12.00, 16.50, 18.00, 19.50],
            [2.80, 0.50, 2.50, 6.50, 10.50, 14.50, 16.00, 17.50],
            [16.50, 15.00, 15.00, 18.00, 3.80, 1.80, 4.00, 5.20],
            [18.00, 16.50, 16.50, 19.50, 7.00, 5.00, 0.50, 1.10],
            [10.50, 9.50, 9.00, 12.00, 2.80, 7.50, 10.50, 12.50],
        ];

        int[] warehouseIds = [1, 2, 3, 4, 5];
        int[] storeIds = [1, 2, 3, 4, 5, 6, 7, 8];
        int[] capacities = [500, 750, 400, 350, 300];

        int[][] demandsByPeriod =
        [
            [90, 130, 80, 50, 35, 70, 85, 40],
            [110, 150, 95, 65, 45, 85, 100, 50],
            [130, 140, 90, 80, 50, 90, 110, 65],
            [160, 210, 120, 100, 60, 115, 130, 75],
        ];

        for (var periodIndex = 0; periodIndex < 4; periodIndex++)
        {
            var warehouseCapacities = warehouseIds
                .Select((warehouseId, index) => new WarehouseCapacity(warehouseId, capacities[index]))
                .ToList();

            var routeCosts = new List<RouteCost>();

            for (var warehouseIndex = 0; warehouseIndex < warehouseIds.Length; warehouseIndex++)
            {
                for (var storeIndex = 0; storeIndex < storeIds.Length; storeIndex++)
                {
                    routeCosts.Add(
                        new RouteCost(
                            warehouseIds[warehouseIndex],
                            storeIds[storeIndex],
                            costs[warehouseIndex][storeIndex]
                        )
                    );
                }
            }

            var demands = storeIds
                .Select((storeId, index) => new StoreDemand(storeId, demandsByPeriod[periodIndex][index]))
                .ToList();

            context.Periods.Add(new Period(2026, periodIndex + 1, warehouseCapacities, demands, routeCosts));
        }

        await context.SaveChangesAsync();
    }

    public static async Task SeedScenariosAsync(DistributorDatabaseContext context)
    {
        context.Scenarios.AddRange(
            new Scenario(
                "Fuel increase",
                [],
                [
                    new ScenarioStoreAdjustment(1, 1.15),
                    new ScenarioStoreAdjustment(2, 1.15),
                    new ScenarioStoreAdjustment(3, 1.15),
                    new ScenarioStoreAdjustment(4, 1.15),
                    new ScenarioStoreAdjustment(5, 1.15),
                    new ScenarioStoreAdjustment(6, 1.15),
                    new ScenarioStoreAdjustment(7, 1.15),
                    new ScenarioStoreAdjustment(8, 1.15),
                ]
            ),
            new Scenario(
                "Warehouse efficiency",
                [new ScenarioWarehouseAdjustment(1, 0.80), new ScenarioWarehouseAdjustment(2, 0.75)],
                [new ScenarioStoreAdjustment(1, 1.10), new ScenarioStoreAdjustment(2, 1.10)]
            )
        );

        await context.SaveChangesAsync();
    }

    public static async Task SeedMinimalPeriodAsync(DistributorDatabaseContext context)
    {
        context.Periods.Add(
            new Period(2026, 1, [new WarehouseCapacity(1, 100)], [new StoreDemand(1, 10)], [new RouteCost(1, 1, 1.0)])
        );

        await context.SaveChangesAsync();
    }
}
