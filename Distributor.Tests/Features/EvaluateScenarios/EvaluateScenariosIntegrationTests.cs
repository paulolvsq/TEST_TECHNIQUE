using Distributor.Data;
using Distributor.Features.EvaluateScenarios;
using Distributor.Matrices;
using Distributor.Periods;
using Distributor.Scenarios;
using Distributor.Stores;
using Distributor.Tests.Data;
using Distributor.Warehouses;
using Microsoft.Extensions.Logging.Abstractions;

namespace Distributor.Tests.Features.EvaluateScenarios;

public sealed class EvaluateScenariosIntegrationTests
{
    [Fact]
    public async Task HandleAsync_WithNoScenarioIds_ReturnsBaseRatesOnly()
    {
        await using var database = await new TestDatabase().CreateAsync(async context =>
        {
            await TestDataSeeder.SeedWarehousesAsync(context);
            await TestDataSeeder.SeedStoresAsync(context);
            await TestDataSeeder.SeedPeriodsAsync(context);
            await TestDataSeeder.SeedScenariosAsync(context);
        });

        await using var context = database.CreateDatabaseContext();
        var handler = CreateHandler(context);
        var query = new EvaluateScenariosQuery
        {
            Start = new PeriodDate(2026, 1),
            End = new PeriodDate(2026, 4),
            ScenarioIds = [],
        };

        var result = await handler.HandleAsync(query);

        result.Scenarios.Length.ShouldBe(1);
        result.Scenarios[0].Name.ShouldBe("Base scenario");
    }

    [Fact]
    public async Task HandleAsync_WithScenarioIds_ReturnsBaseAndRequestedScenarios()
    {
        await using var database = await new TestDatabase().CreateAsync(async context =>
        {
            await TestDataSeeder.SeedWarehousesAsync(context);
            await TestDataSeeder.SeedStoresAsync(context);
            await TestDataSeeder.SeedPeriodsAsync(context);
            await TestDataSeeder.SeedScenariosAsync(context);
        });

        await using var context = database.CreateDatabaseContext();
        var handler = CreateHandler(context);
        var query = new EvaluateScenariosQuery
        {
            Start = new PeriodDate(2026, 1),
            End = new PeriodDate(2026, 4),
            ScenarioIds = [1, 2],
        };

        var result = await handler.HandleAsync(query);

        result.Scenarios.Length.ShouldBe(3);
        result.Scenarios[0].Name.ShouldBe("Base scenario");
        result.Scenarios[1].Name.ShouldBe("Fuel increase");
        result.Scenarios[2].Name.ShouldBe("Warehouse efficiency");
    }

    [Fact]
    public async Task HandleAsync_TotalCostSumsPeriodsAndWarehouses()
    {
        await using var database = await new TestDatabase().CreateAsync(async context =>
        {
            await TestDataSeeder.SeedWarehousesAsync(context);
            await TestDataSeeder.SeedStoresAsync(context);
            await TestDataSeeder.SeedPeriodsAsync(context);
            await TestDataSeeder.SeedScenariosAsync(context);
        });

        await using var context = database.CreateDatabaseContext();
        var handler = CreateHandler(context);
        var query = new EvaluateScenariosQuery
        {
            Start = new PeriodDate(2026, 1),
            End = new PeriodDate(2026, 2),
            ScenarioIds = [1, 2],
        };

        var result = await handler.HandleAsync(query);

        foreach (var scenario in result.Scenarios)
        {
            scenario.Periods.Length.ShouldBe(2);
            scenario.TotalCost.ShouldBe(scenario.Periods.Sum(period => period.TotalCost));

            foreach (var period in scenario.Periods)
            {
                period.TotalCost.ShouldBe(period.Warehouses.Sum(warehouse => warehouse.Cost));
            }
        }
    }

    [Fact]
    public async Task HandleAsync_FuelIncreaseScalesBaseCostByMultiplier()
    {
        await using var database = await new TestDatabase().CreateAsync(async context =>
        {
            await TestDataSeeder.SeedWarehousesAsync(context);
            await TestDataSeeder.SeedStoresAsync(context);
            await TestDataSeeder.SeedPeriodsAsync(context);
            await TestDataSeeder.SeedScenariosAsync(context);
        });

        await using var context = database.CreateDatabaseContext();
        var handler = CreateHandler(context);
        var query = new EvaluateScenariosQuery
        {
            Start = new PeriodDate(2026, 1),
            End = new PeriodDate(2026, 4),
            ScenarioIds = [1],
        };

        var result = await handler.HandleAsync(query);

        var baseCost = result.Scenarios[0].TotalCost;
        var fuelIncreaseCost = result.Scenarios[1].TotalCost;

        baseCost.ShouldBeGreaterThan(0);
        fuelIncreaseCost.ShouldBeGreaterThan(baseCost);

        var ratio = fuelIncreaseCost / baseCost;
        ratio.ShouldBe(1.15m, 0.001m);
    }

    [Fact]
    public async Task HandleAsync_EachPeriodHasAllWarehouses()
    {
        await using var database = await new TestDatabase().CreateAsync(async context =>
        {
            await TestDataSeeder.SeedWarehousesAsync(context);
            await TestDataSeeder.SeedStoresAsync(context);
            await TestDataSeeder.SeedPeriodsAsync(context);
            await TestDataSeeder.SeedScenariosAsync(context);
        });

        await using var context = database.CreateDatabaseContext();
        var handler = CreateHandler(context);
        var query = new EvaluateScenariosQuery
        {
            Start = new PeriodDate(2026, 1),
            End = new PeriodDate(2026, 1),
            ScenarioIds = [1],
        };

        var result = await handler.HandleAsync(query);

        foreach (var scenario in result.Scenarios)
        {
            foreach (var period in scenario.Periods)
            {
                period.Warehouses.Length.ShouldBe(5);
            }
        }
    }

    private static EvaluateScenariosQueryHandler CreateHandler(DistributorDatabaseContext context)
    {
        return new EvaluateScenariosQueryHandler(
            new PeriodRepository(context),
            new WarehouseRepository(context),
            new StoreRepository(context),
            new ScenarioRepository(context),
            new ScenarioMatrixFactory(),
            new MatrixMultiplier(new MatrixSplitter(), new MatrixSpanMultiplier()),
            NullLogger<EvaluateScenariosQueryHandler>.Instance
        );
    }
}
