namespace Distributor.Plans;

public sealed class DistributionPlan
{
    private readonly List<Shipment> _shipments = [];

    private DistributionPlan() { }

    public DistributionPlan(
        string name,
        DateTime createdAt,
        int startPeriodId,
        int endPeriodId,
        IReadOnlyList<Shipment> shipments
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(startPeriodId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(endPeriodId);
        ArgumentNullException.ThrowIfNull(shipments);

        if (shipments.Count is 0)
        {
            throw new ArgumentException("Shipments must not be empty.", nameof(shipments));
        }

        Name = name;
        CreatedAt = createdAt;
        StartPeriodId = startPeriodId;
        EndPeriodId = endPeriodId;
        _shipments = shipments.ToList();
    }

    public int Id { get; private init; }
    public string Name { get; private init; } = null!;
    public DateTime CreatedAt { get; private init; }
    public int StartPeriodId { get; private init; }
    public int EndPeriodId { get; private init; }
    public IReadOnlyList<Shipment> Shipments => _shipments;
}
