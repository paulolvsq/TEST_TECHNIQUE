using System.Collections.Immutable;
using Distributor.Periods;

namespace Distributor.Seeder;

public sealed class DistributorDatasetBuilder
{
    private static readonly ImmutableArray<string> Cities =
    [
        "Montréal",
        "Toronto",
        "Calgary",
        "Vancouver",
        "Winnipeg",
        "Edmonton",
        "Ottawa",
        "Halifax",
        "Québec City",
        "Hamilton",
        "Kitchener",
        "London",
        "Victoria",
        "Saskatoon",
        "Regina",
        "St. John's",
        "Moncton",
        "Thunder Bay",
        "Sudbury",
        "Kelowna",
        "Red Deer",
        "Lethbridge",
        "Prince George",
        "Kamloops",
        "Nanaimo",
        "Barrie",
        "Kingston",
        "Peterborough",
        "Brantford",
        "Guelph",
        "Saint John",
        "Fredericton",
        "Charlottetown",
        "Whitehorse",
        "Yellowknife",
        "Sherbrooke",
        "Trois-Rivières",
        "Saguenay",
        "Drummondville",
        "Granby",
        "Rimouski",
        "Shawinigan",
        "Val-d'Or",
        "Sept-Îles",
        "Timmins",
        "North Bay",
        "Sault Ste. Marie",
        "Cornwall",
        "Medicine Hat",
        "Brandon",
    ];

    private static readonly ImmutableArray<string> Directions = ["North", "South", "East", "West"];

    private NetworkSize _size = NetworkSize.Small;
    private PeriodDate _start = new(year: 2000, month: 1);
    private int _periodCount = 1;
    private int _seed;

    public DistributorDatasetBuilder WithSize(NetworkSize size)
    {
        if (!Enum.IsDefined(size))
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        _size = size;

        return this;
    }

    public DistributorDatasetBuilder WithStartPeriod(PeriodDate start)
    {
        _start = start;

        return this;
    }

    public DistributorDatasetBuilder WithPeriodCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        _periodCount = count;

        return this;
    }

    public DistributorDatasetBuilder WithSeed(int seed)
    {
        _seed = seed;

        return this;
    }

    public DistributorDataset Build()
    {
        var random = new Random(_seed);
        var warehouseCount = GetWarehouseCount(_size);
        var storeCount = GetStoreCount(_size);
        var capacityCount = _periodCount * warehouseCount;
        var demandCount = _periodCount * storeCount;
        var costCount = _periodCount * warehouseCount * storeCount;
        var scenarioDefinitions = BuildScenarioDefinitions(warehouseCount, storeCount);
        var warehouseAdjustmentCount = scenarioDefinitions.Sum(definition => definition.WarehouseMultipliers.Count);
        var storeAdjustmentCount = scenarioDefinitions.Sum(definition => definition.StoreMultipliers.Count);

        var warehouses = BuildWarehouses(warehouseCount);
        var stores = BuildStores(storeCount);
        var periods = ImmutableArray.CreateBuilder<Period>(_periodCount);
        var capacities = ImmutableArray.CreateBuilder<WarehouseCapacity>(capacityCount);
        var demands = ImmutableArray.CreateBuilder<StoreDemand>(demandCount);
        var costs = ImmutableArray.CreateBuilder<RouteCost>(costCount);
        var scenarios = ImmutableArray.CreateBuilder<Scenario>(scenarioDefinitions.Length);
        var warehouseAdjustments = ImmutableArray.CreateBuilder<ScenarioWarehouseAdjustment>(warehouseAdjustmentCount);
        var storeAdjustments = ImmutableArray.CreateBuilder<ScenarioStoreAdjustment>(storeAdjustmentCount);

        var periodDate = _start;

        for (var index = 0; index < _periodCount; index++)
        {
            var period = new Period(Id: index + 1, periodDate.Year, periodDate.Month);
            var periodCapacities = BuildPeriodCapacities(period, warehouses, random).ToImmutableArray();
            var totalCapacity = periodCapacities.Sum(capacity => capacity.Units);

            periods.Add(period);
            capacities.AddRange(periodCapacities);
            costs.AddRange(BuildPeriodCosts(period, warehouses, stores, random));
            demands.AddRange(BuildPeriodDemands(period, stores, random, totalCapacity));

            periodDate = periodDate.Next();
        }

        for (var index = 0; index < scenarioDefinitions.Length; index++)
        {
            var definition = scenarioDefinitions[index];
            var scenario = new Scenario(Id: index + 1, definition.Name);

            scenarios.Add(scenario);
            warehouseAdjustments.AddRange(BuildWarehouseAdjustments(definition, scenario));
            storeAdjustments.AddRange(BuildStoreAdjustments(definition, scenario));
        }

        return new DistributorDataset
        {
            Warehouses = warehouses,
            Stores = stores,
            Periods = periods.MoveToImmutable(),
            Capacities = capacities.MoveToImmutable(),
            Demands = demands.MoveToImmutable(),
            Costs = costs.MoveToImmutable(),
            Scenarios = scenarios.MoveToImmutable(),
            ScenarioWarehouseAdjustments = warehouseAdjustments.MoveToImmutable(),
            ScenarioStoreAdjustments = storeAdjustments.MoveToImmutable(),
        };
    }

    private static IEnumerable<ScenarioWarehouseAdjustment> BuildWarehouseAdjustments(
        ScenarioDefinition definition,
        Scenario scenario
    )
    {
        return definition.WarehouseMultipliers.Select(entry => new ScenarioWarehouseAdjustment(
            scenario.Id,
            entry.Key,
            entry.Value
        ));
    }

    private static IEnumerable<ScenarioStoreAdjustment> BuildStoreAdjustments(
        ScenarioDefinition definition,
        Scenario scenario
    )
    {
        return definition.StoreMultipliers.Select(entry => new ScenarioStoreAdjustment(
            scenario.Id,
            entry.Key,
            entry.Value
        ));
    }

    private static int GetWarehouseCount(NetworkSize size)
    {
        return size switch
        {
            NetworkSize.Small => 10,
            NetworkSize.Large => 150,

            _ => throw new ArgumentOutOfRangeException(nameof(size)),
        };
    }

    private static int GetStoreCount(NetworkSize size)
    {
        return size switch
        {
            NetworkSize.Small => 20,
            NetworkSize.Large => 300,

            _ => throw new ArgumentOutOfRangeException(nameof(size)),
        };
    }

    private static ImmutableArray<Warehouse> BuildWarehouses(int count)
    {
        var warehouses = ImmutableArray.CreateBuilder<Warehouse>(count);

        for (var i = 0; i < count; i++)
        {
            var id = i + 1;
            var name = i < Cities.Length ? Cities[i] : $"Warehouse {i + 1}";
            var warehouse = new Warehouse(id, name);
            warehouses.Add(warehouse);
        }

        return warehouses.MoveToImmutable();
    }

    private static ImmutableArray<Store> BuildStores(int count)
    {
        var stores = ImmutableArray.CreateBuilder<Store>(count);
        var namedStoreCount = Cities.Length * Directions.Length;

        for (var i = 0; i < count; i++)
        {
            var id = i + 1;

            if (i < namedStoreCount)
            {
                var cityIndex = i / Directions.Length;
                var directionIndex = i % Directions.Length;
                var store = new Store(id, $"{Cities[cityIndex]} {Directions[directionIndex]}");
                stores.Add(store);
            }
            else
            {
                var store = new Store(id, $"Store {id}");
                stores.Add(store);
            }
        }

        return stores.MoveToImmutable();
    }

    private static ImmutableArray<ScenarioDefinition> BuildScenarioDefinitions(int warehouseCount, int storeCount)
    {
        return
        [
            new()
            {
                Name = "Federal fuel tax increase",
                WarehouseMultipliers = [],
                StoreMultipliers = ConstantMultipliers(storeCount, 1.15),
            },
            new()
            {
                Name = "Toronto and Calgary automation",
                WarehouseMultipliers = new() { [2] = 0.75, [3] = 0.75 },
                StoreMultipliers = [],
            },
            new()
            {
                Name = "British Columbia port disruption",
                WarehouseMultipliers = new() { [4] = 1.50 },
                StoreMultipliers = new()
                {
                    [13] = 1.20,
                    [14] = 1.20,
                    [15] = 1.20,
                    [16] = 1.20,
                },
            },
            new()
            {
                Name = "Central Canada hub strategy",
                WarehouseMultipliers = new()
                {
                    [1] = 0.80,
                    [2] = 0.80,
                    [7] = 0.80,
                    [10] = 0.80,
                    [3] = 1.15,
                    [4] = 1.15,
                    [5] = 1.15,
                    [8] = 1.15,
                },
                StoreMultipliers = [],
            },
            new()
            {
                Name = "Warehouse scale efficiency",
                WarehouseMultipliers = MultipliersGradient(warehouseCount, 0.85, 1.15),
                StoreMultipliers = [],
            },
            new()
            {
                Name = "Montréal and Toronto delivery premium",
                WarehouseMultipliers = [],
                StoreMultipliers = Enumerable
                    .Range(1, storeCount)
                    .ToDictionary(
                        id => id,
                        id =>
                            id switch
                            {
                                >= 1 and <= 4 => 1.20,
                                >= 5 and <= 8 => 1.25,
                                _ => 0.95,
                            }
                    ),
            },
            new()
            {
                Name = "Prairie market competition",
                WarehouseMultipliers = new() { [3] = 1.10, [5] = 1.10 },
                StoreMultipliers = new()
                {
                    [9] = 0.80,
                    [10] = 0.80,
                    [11] = 0.80,
                    [12] = 0.80,
                    [17] = 0.85,
                    [18] = 0.85,
                    [19] = 0.85,
                    [20] = 0.85,
                },
            },
            new()
            {
                Name = "New Edmonton distribution center",
                WarehouseMultipliers = new() { [6] = 0.60 },
                StoreMultipliers = new()
                {
                    [9] = 0.95,
                    [10] = 0.95,
                    [11] = 0.95,
                    [12] = 0.95,
                },
            },
            new()
            {
                Name = "Provincial carbon pricing",
                WarehouseMultipliers = MultipliersGradient(warehouseCount, 0.95, 1.25),
                StoreMultipliers = [],
            },
        ];
    }

    private static Dictionary<int, double> ConstantMultipliers(int count, double multiplier)
    {
        return Enumerable.Range(1, count).ToDictionary(id => id, _ => multiplier);
    }

    private static Dictionary<int, double> MultipliersGradient(int count, double from, double to)
    {
        return Enumerable
            .Range(1, count)
            .ToDictionary(
                id => id,
                id =>
                {
                    var fraction = (double)(id - 1) / Math.Max(1, count - 1);
                    var value = from + (fraction * (to - from));

                    return Math.Round(value, 2, MidpointRounding.AwayFromZero);
                }
            );
    }

    private static IEnumerable<StoreDemand> BuildPeriodDemands(
        Period period,
        ImmutableArray<Store> stores,
        Random random,
        int totalCapacity
    )
    {
        return stores.Select(store => new StoreDemand(
            period.Id,
            store.Id,
            Units: GetDemandUnits(stores, totalCapacity, random)
        ));
    }

    private static IEnumerable<RouteCost> BuildPeriodCosts(
        Period period,
        ImmutableArray<Warehouse> warehouses,
        ImmutableArray<Store> stores,
        Random random
    )
    {
        return warehouses.SelectMany(warehouse =>
            stores.Select(store => new RouteCost(
                period.Id,
                warehouse.Id,
                store.Id,
                GetUnitCost(warehouse.Id, store.Id, random)
            ))
        );
    }

    private static IEnumerable<WarehouseCapacity> BuildPeriodCapacities(
        Period period,
        ImmutableArray<Warehouse> warehouses,
        Random random
    )
    {
        return warehouses.Select(warehouse => new WarehouseCapacity(
            period.Id,
            warehouse.Id,
            Units: random.Next(200, 1001)
        ));
    }

    private static double GetUnitCost(int warehouseId, int storeId, Random random)
    {
        var warehousePositionX = (warehouseId - 1) % 10;
        var warehousePositionY = (warehouseId - 1) / 10;
        var storePositionX = ((storeId - 1) % 20) * 0.5;
        var storePositionY = ((storeId - 1) / 20) * 0.5;
        var distanceX = warehousePositionX - storePositionX;
        var distanceY = warehousePositionY - storePositionY;
        var distance = Math.Sqrt((distanceX * distanceX) + (distanceY * distanceY));

        var baseUnitCost = distance * 2;
        var noise = (random.NextDouble() * 2) + 0.5;
        var unitCost = baseUnitCost + noise;

        return Math.Round(unitCost, digits: 2, MidpointRounding.AwayFromZero);
    }

    private static int GetDemandUnits(ImmutableArray<Store> stores, int totalCapacity, Random random)
    {
        var maximum = Math.Max(1, (int)(totalCapacity * 0.9) / stores.Length);

        return random.Next(1, maximum + 1);
    }

    private sealed record ScenarioDefinition
    {
        public required string Name { get; init; }
        public required Dictionary<int, double> WarehouseMultipliers { get; init; }
        public required Dictionary<int, double> StoreMultipliers { get; init; }
    }
}
