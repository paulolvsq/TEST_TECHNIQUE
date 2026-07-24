namespace Distributor.Warehouses;

public sealed class Warehouse
{
    public Warehouse(int id, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Name = name;
    }

    public int Id { get; }
    public string Name { get; }
}
