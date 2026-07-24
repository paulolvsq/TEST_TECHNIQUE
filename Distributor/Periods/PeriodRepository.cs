using System.Linq.Expressions;
using Distributor.Data;
using Microsoft.EntityFrameworkCore;

namespace Distributor.Periods;

public interface IPeriodRepository
{
    Task<List<Period>> GetPeriodsAsync(PeriodDate start, PeriodDate end, CancellationToken token = default);
}

public sealed class PeriodRepository : IPeriodRepository
{
    private readonly DistributorDatabaseContext _context;

    public PeriodRepository(DistributorDatabaseContext context)
    {
        _context = context;
    }

    public async Task<List<Period>> GetPeriodsAsync(PeriodDate start, PeriodDate end, CancellationToken token = default)
    {
        return await _context
            .Periods.Include(period => period.Capacities)
            .Include(period => period.Demands)
            .Include(period => period.Costs)
            .AsSplitQuery()
            .Where(PeriodIsBetween(start, end))
            .OrderBy(period => period.Year)
            .ThenBy(period => period.Month)
            .ToListAsync(token)
            .ConfigureAwait(false);
    }

    private static Expression<Func<Period, bool>> PeriodIsBetween(PeriodDate start, PeriodDate end)
    {
        var startMonth = (start.Year * 12) + start.Month;
        var endMonth = (end.Year * 12) + end.Month;

        return period =>
            (period.Year * 12) + period.Month >= startMonth && (period.Year * 12) + period.Month <= endMonth;
    }
}
