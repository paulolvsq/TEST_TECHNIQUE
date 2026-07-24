using Distributor.Data;
using Distributor.Features.PlanDistribution;
using Distributor.Periods;
using Distributor.Stores;
using Distributor.Tests.Data;
using Distributor.Warehouses;
using Microsoft.Extensions.Logging.Abstractions;

namespace Distributor.Tests.Features.PlanDistribution;

public sealed class PlanDistributionIntegrationTests
{
    [Fact]
    public async Task HandleAsync_CreatesDistributionPlan()
    {
        await using var database = await new TestDatabase().CreateAsync(async context =>
        {
            await TestDataSeeder.SeedWarehousesAsync(context);
            await TestDataSeeder.SeedStoresAsync(context);
            await TestDataSeeder.SeedPeriodsAsync(context);
        });

        await using var context = database.CreateDatabaseContext();
        var handler = CreateHandler(context);
        var command = new PlanDistributionCommand { Start = new PeriodDate(2026, 1), End = new PeriodDate(2026, 2) };

        var result = await handler.HandleAsync(command);

        result.DistributionPlanId.ShouldBeGreaterThan(0);
        result.TotalCost.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task HandleAsync_EachPeriodHasShipments()
    {
        await using var database = await new TestDatabase().CreateAsync(async context =>
        {
            await TestDataSeeder.SeedWarehousesAsync(context);
            await TestDataSeeder.SeedStoresAsync(context);
            await TestDataSeeder.SeedPeriodsAsync(context);
        });

        await using var context = database.CreateDatabaseContext();
        var handler = CreateHandler(context);
        var command = new PlanDistributionCommand { Start = new PeriodDate(2026, 1), End = new PeriodDate(2026, 4) };

        var result = await handler.HandleAsync(command);

        result.Periods.Length.ShouldBe(4);

        foreach (var period in result.Periods)
        {
            period.Shipments.Length.ShouldBeGreaterThan(0);
            period.TotalCost.ShouldBeGreaterThan(0);
        }
    }

    [Fact]
    public async Task HandleAsync_TotalCostSumsPeriodCosts()
    {
        await using var database = await new TestDatabase().CreateAsync(async context =>
        {
            await TestDataSeeder.SeedWarehousesAsync(context);
            await TestDataSeeder.SeedStoresAsync(context);
            await TestDataSeeder.SeedPeriodsAsync(context);
        });

        await using var context = database.CreateDatabaseContext();
        var handler = CreateHandler(context);
        var command = new PlanDistributionCommand { Start = new PeriodDate(2026, 1), End = new PeriodDate(2026, 4) };

        var result = await handler.HandleAsync(command);

        var expectedTotal = result.Periods.Sum(period => period.TotalCost);
        result.TotalCost.ShouldBe(expectedTotal);
    }

    [Fact]
    public async Task HandleAsync_ShipmentCostEqualsUnitsTimesUnitCost()
    {
        await using var database = await new TestDatabase().CreateAsync(async context =>
        {
            await TestDataSeeder.SeedWarehousesAsync(context);
            await TestDataSeeder.SeedStoresAsync(context);
            await TestDataSeeder.SeedPeriodsAsync(context);
        });

        await using var context = database.CreateDatabaseContext();
        var handler = CreateHandler(context);
        var command = new PlanDistributionCommand { Start = new PeriodDate(2026, 1), End = new PeriodDate(2026, 1) };

        var result = await handler.HandleAsync(command);

        foreach (var period in result.Periods)
        {
            foreach (var shipment in period.Shipments)
            {
                shipment.Cost.ShouldBe(shipment.Units * shipment.UnitCost);
            }
        }
    }

    private static PlanDistributionCommandHandler CreateHandler(DistributorDatabaseContext context)
    {
        return new PlanDistributionCommandHandler(
            context,
            new PeriodRepository(context),
            new WarehouseRepository(context),
            new StoreRepository(context),
            new TransportSolver(),
            NullLogger<PlanDistributionCommandHandler>.Instance
        );
    }
}
