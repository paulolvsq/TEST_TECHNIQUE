using Distributor.Data;
using Microsoft.EntityFrameworkCore;

namespace Distributor.Stores;

public interface IStoreRepository
{
    Task<List<Store>> GetStoresAsync(IEnumerable<int> ids, CancellationToken token = default);
}

public sealed class StoreRepository : IStoreRepository
{
    private readonly DistributorDatabaseContext _context;

    public StoreRepository(DistributorDatabaseContext context)
    {
        _context = context;
    }

    public async Task<List<Store>> GetStoresAsync(IEnumerable<int> ids, CancellationToken token = default)
    {
        var idSet = ids.ToHashSet();

        return await _context.Stores.Where(store => idSet.Contains(store.Id)).ToListAsync(token).ConfigureAwait(false);
    }
}
