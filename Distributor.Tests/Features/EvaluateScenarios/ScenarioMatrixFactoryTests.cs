using Distributor.Features.EvaluateScenarios;
using Distributor.Periods;
using Distributor.Scenarios;
using Distributor.Stores;
using Distributor.Warehouses;

namespace Distributor.Tests.Features.EvaluateScenarios;

public sealed class ScenarioMatrixFactoryTests
{
    private readonly ScenarioMatrixFactory _factory = new();

    [Fact]
    public void BuildCostMatrix_GivenRouteCosts_ProducesCorrectMatrix()
    {
        var warehouses = new List<Warehouse> { new(1, "W1"), new(2, "W2") };
        var stores = new List<Store> { new(1, "S1"), new(2, "S2") };
        var routeCosts = new List<RouteCost> { new(1, 1, 1.50), new(1, 2, 2.50), new(2, 1, 3.00), new(2, 2, 4.00) };

        var matrix = _factory.BuildCostMatrix(warehouses, stores, routeCosts);

        Assert.Equal(2, matrix.RowCount);
        Assert.Equal(2, matrix.ColumnCount);
        Assert.Equal(1.50, matrix[0, 0]);
        Assert.Equal(2.50, matrix[0, 1]);
        Assert.Equal(3.00, matrix[1, 0]);
        Assert.Equal(4.00, matrix[1, 1]);
    }

    [Fact]
    public void BuildAdjustedDemandMatrix_GivenNoAdjustments_ReturnsRawDemand()
    {
        var stores = new List<Store> { new(1, "S1"), new(2, "S2") };
        var demands = new List<StoreDemand> { new(1, 100), new(2, 200) };
        var scenarios = new List<Scenario> { new("Base", [], []) };

        var matrix = _factory.BuildAdjustedDemandMatrix(stores, demands, scenarios);

        Assert.Equal(2, matrix.RowCount);
        Assert.Equal(1, matrix.ColumnCount);
        Assert.Equal(100.0, matrix[0, 0]);
        Assert.Equal(200.0, matrix[1, 0]);
    }

    [Fact]
    public void BuildAdjustedDemandMatrix_GivenStoreMultiplier_MultipliesDemand()
    {
        var stores = new List<Store> { new(1, "S1"), new(2, "S2") };
        var demands = new List<StoreDemand> { new(1, 100), new(2, 200) };
        var scenarios = new List<Scenario> { new("Surcharge", [], [new(1, 1.50)]) };

        var matrix = _factory.BuildAdjustedDemandMatrix(stores, demands, scenarios);

        Assert.Equal(150.0, matrix[0, 0]);
        Assert.Equal(200.0, matrix[1, 0]);
    }

    [Fact]
    public void BuildAdjustedDemandMatrix_GivenMultipleScenarios_ProducesCorrectColumns()
    {
        var stores = new List<Store> { new(1, "S1"), new(2, "S2") };
        var demands = new List<StoreDemand> { new(1, 100), new(2, 200) };
        var scenarios = new List<Scenario> { new("Base", [], []), new("Adjusted", [], [new(1, 2.0), new(2, 0.5)]) };

        var matrix = _factory.BuildAdjustedDemandMatrix(stores, demands, scenarios);

        Assert.Equal(2, matrix.ColumnCount);
        Assert.Equal(100.0, matrix[0, 0]);
        Assert.Equal(200.0, matrix[1, 0]);
        Assert.Equal(200.0, matrix[0, 1]);
        Assert.Equal(100.0, matrix[1, 1]);
    }

    [Fact]
    public void BuildWarehouseMultiplierMatrix_GivenNoAdjustments_ReturnsAllOnes()
    {
        var warehouses = new List<Warehouse> { new(1, "W1"), new(2, "W2") };
        var scenarios = new List<Scenario> { new("Base", [], []) };

        var matrix = _factory.BuildWarehouseMultiplierMatrix(warehouses, scenarios);

        Assert.Equal(2, matrix.RowCount);
        Assert.Equal(1, matrix.ColumnCount);
        Assert.Equal(1.0, matrix[0, 0]);
        Assert.Equal(1.0, matrix[1, 0]);
    }

    [Fact]
    public void BuildWarehouseMultiplierMatrix_GivenAdjustments_AppliesMultipliers()
    {
        var warehouses = new List<Warehouse> { new(1, "W1"), new(2, "W2") };
        var scenarios = new List<Scenario> { new("Efficiency", [new(1, 0.75)], []) };

        var matrix = _factory.BuildWarehouseMultiplierMatrix(warehouses, scenarios);

        Assert.Equal(0.75, matrix[0, 0]);
        Assert.Equal(1.0, matrix[1, 0]);
    }
}
