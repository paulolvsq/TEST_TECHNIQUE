using Distributor.Features.PlanDistribution;
using Distributor.Periods;

namespace Distributor.Tests.Features.PlanDistribution;

public sealed class TransportSolverTests
{
    [Fact]
    public void Solve_GivenPeriod_ProducesShipmentsMeetingDemand()
    {
        var period = new Period(
            2026,
            1,
            [new WarehouseCapacity(1, 200), new WarehouseCapacity(2, 150)],
            [new StoreDemand(1, 50), new StoreDemand(2, 60)],
            [new RouteCost(1, 1, 5.0), new RouteCost(1, 2, 8.0), new RouteCost(2, 1, 7.0), new RouteCost(2, 2, 3.0)]
        );
        var solver = new TransportSolver();

        var shipments = solver.Solve(period);

        var totalShipped = shipments.Sum(shipment => shipment.Units);
        var totalDemand = period.Demands.Sum(demand => demand.Units);
        totalShipped.ShouldBe(totalDemand);
    }

    [Fact]
    public void Solve_GivenSameInput_ProducesDeterministicResult()
    {
        var period = new Period(
            2026,
            1,
            [new WarehouseCapacity(1, 500), new WarehouseCapacity(2, 500)],
            [new StoreDemand(1, 100), new StoreDemand(2, 100)],
            [new RouteCost(1, 1, 5.0), new RouteCost(1, 2, 8.0), new RouteCost(2, 1, 7.0), new RouteCost(2, 2, 3.0)]
        );
        var solver = new TransportSolver();

        var shipments1 = solver.Solve(period);
        var shipments2 = solver.Solve(period);

        var sorted1 = shipments1
            .Select(shipment => (shipment.WarehouseId, shipment.StoreId, shipment.Units))
            .OrderBy(shipment => shipment.WarehouseId)
            .ThenBy(shipment => shipment.StoreId)
            .ToArray();

        var sorted2 = shipments2
            .Select(shipment => (shipment.WarehouseId, shipment.StoreId, shipment.Units))
            .OrderBy(shipment => shipment.WarehouseId)
            .ThenBy(shipment => shipment.StoreId)
            .ToArray();

        sorted1.ShouldBe(sorted2);
    }

    [Fact]
    public void Solve_GivenCapacityConstrainedPeriod_ProducesMinimumCostPlan()
    {
        var period = new Period(
            2026,
            1,
            [new WarehouseCapacity(1, 50), new WarehouseCapacity(2, 120)],
            [new StoreDemand(1, 70), new StoreDemand(2, 90)],
            [new RouteCost(1, 1, 1.0), new RouteCost(1, 2, 10.0), new RouteCost(2, 1, 8.0), new RouteCost(2, 2, 2.0)]
        );
        var solver = new TransportSolver();

        var shipments = solver.Solve(period);

        var costByRoute = period.Costs.ToDictionary(cost => (cost.WarehouseId, cost.StoreId), rc => rc.UnitCost);

        var totalCost = shipments.Sum(shipment =>
            shipment.Units * costByRoute[(shipment.WarehouseId, shipment.StoreId)]
        );

        // Optimal: W1->S1 = 50 * 1.0, W2->S1 = 20 * 8.0, W2->S2 = 90 * 2.0 = 50 + 160 + 180 = 390
        totalCost.ShouldBe(390.0);

        foreach (var demand in period.Demands)
        {
            var shipped = shipments
                .Where(shipment => shipment.StoreId == demand.StoreId)
                .Sum(shipment => shipment.Units);
            shipped.ShouldBe(demand.Units);
        }

        var capacitiesByWarehouse = period.Capacities.ToDictionary(c => c.WarehouseId, c => c.Units);
        var totalsByWarehouse = shipments
            .GroupBy(shipment => shipment.WarehouseId)
            .ToDictionary(group => group.Key, group => group.Sum(shipment => shipment.Units));

        foreach (var (warehouseId, capacity) in capacitiesByWarehouse)
        {
            totalsByWarehouse.GetValueOrDefault(warehouseId).ShouldBeLessThanOrEqualTo(capacity);
        }
    }

    [Fact]
    public void Solve_WhenDemandExceedsCapacity_ThrowsInvalidOperationException()
    {
        var period = new Period(
            2026,
            1,
            [new WarehouseCapacity(1, 10), new WarehouseCapacity(2, 10)],
            [new StoreDemand(1, 100)],
            [new RouteCost(1, 1, 5.0), new RouteCost(2, 1, 7.0)]
        );
        var solver = new TransportSolver();

        Should.Throw<InvalidOperationException>(() => solver.Solve(period));
    }
}
