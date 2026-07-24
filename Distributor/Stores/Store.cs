namespace Distributor.Stores;

public sealed class Store
{
    public Store(int id, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Name = name;
    }

    public int Id { get; }
    public string Name { get; }
}
