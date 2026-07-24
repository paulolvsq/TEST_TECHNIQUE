using Distributor.Data;
using Microsoft.EntityFrameworkCore;

namespace Distributor.Warehouses;

public interface IWarehouseRepository
{
    Task<List<Warehouse>> GetWarehousesAsync(IEnumerable<int> ids, CancellationToken token = default);
}

public sealed class WarehouseRepository : IWarehouseRepository
{
    private readonly DistributorDatabaseContext _context;

    public WarehouseRepository(DistributorDatabaseContext context)
    {
        _context = context;
    }

    public async Task<List<Warehouse>> GetWarehousesAsync(IEnumerable<int> ids, CancellationToken token = default)
    {
        var idSet = ids.ToHashSet();

        return await _context
            .Warehouses.Where(warehouse => idSet.Contains(warehouse.Id))
            .ToListAsync(token)
            .ConfigureAwait(false);
    }
}
