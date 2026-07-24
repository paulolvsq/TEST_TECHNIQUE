namespace Distributor.Scenarios;

public sealed class ScenarioStoreAdjustment
{
    private ScenarioStoreAdjustment() { }

    public ScenarioStoreAdjustment(int storeId, double multiplier)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(multiplier);

        StoreId = storeId;
        Multiplier = multiplier;
    }

    public int ScenarioId { get; private init; }
    public int StoreId { get; private init; }
    public double Multiplier { get; private init; }
}
