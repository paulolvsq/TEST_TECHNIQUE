using Distributor.Data;
using Microsoft.EntityFrameworkCore;

namespace Distributor.Scenarios;

public interface IScenarioRepository
{
    Task<List<Scenario>> GetScenariosAsync(CancellationToken token = default);
    Task<List<Scenario>> GetScenariosAsync(IEnumerable<int> ids, CancellationToken token = default);
}

public sealed class ScenarioRepository : IScenarioRepository
{
    private readonly DistributorDatabaseContext _context;

    public ScenarioRepository(DistributorDatabaseContext context)
    {
        _context = context;
    }

    public async Task<List<Scenario>> GetScenariosAsync(CancellationToken token = default)
    {
        return await _context
            .Scenarios.Include(scenario => scenario.WarehouseAdjustments)
            .Include(scenario => scenario.StoreAdjustments)
            .AsSplitQuery()
            .OrderBy(scenario => scenario.Id)
            .ToListAsync(token)
            .ConfigureAwait(false);
    }

    public async Task<List<Scenario>> GetScenariosAsync(IEnumerable<int> ids, CancellationToken token = default)
    {
        var idSet = ids.ToHashSet();

        return await _context
            .Scenarios.Include(scenario => scenario.WarehouseAdjustments)
            .Include(scenario => scenario.StoreAdjustments)
            .AsSplitQuery()
            .Where(scenario => idSet.Contains(scenario.Id))
            .OrderBy(scenario => scenario.Id)
            .ToListAsync(token)
            .ConfigureAwait(false);
    }
}
