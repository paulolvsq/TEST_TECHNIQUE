namespace Distributor.Periods;

public sealed class Period
{
    private readonly List<WarehouseCapacity> _capacities = [];
    private readonly List<StoreDemand> _demands = [];
    private readonly List<RouteCost> _costs = [];

    private Period() { }

    public Period(
        int year,
        int month,
        IReadOnlyList<WarehouseCapacity> capacities,
        IReadOnlyList<StoreDemand> demands,
        IReadOnlyList<RouteCost> costs
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(year);
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);
        ArgumentNullException.ThrowIfNull(capacities);
        ArgumentNullException.ThrowIfNull(demands);
        ArgumentNullException.ThrowIfNull(costs);

        if (capacities.Count is 0)
        {
            throw new ArgumentException("Capacities must not be empty.", nameof(capacities));
        }

        if (demands.Count is 0)
        {
            throw new ArgumentException("Demands must not be empty.", nameof(demands));
        }

        if (costs.Count is 0)
        {
            throw new ArgumentException("Costs must not be empty.", nameof(costs));
        }

        Year = year;
        Month = month;
        _capacities = capacities.ToList();
        _demands = demands.ToList();
        _costs = costs.ToList();
    }

    public int Id { get; private init; }
    public int Year { get; private init; }
    public int Month { get; private init; }
    public IReadOnlyList<WarehouseCapacity> Capacities => _capacities;
    public IReadOnlyList<StoreDemand> Demands => _demands;
    public IReadOnlyList<RouteCost> Costs => _costs;
}
