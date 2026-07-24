using BenchmarkDotNet.Attributes;
using Distributor.Application;
using Distributor.Data;
using Distributor.Features.PlanDistribution;
using Distributor.Periods;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Distributor.Benchmarks;

[MemoryDiagnoser]
public class PlanDistributionBenchmarks
{
    private IHost _host = null!;

    [Params(1, 3, 6, 12)]
    public int PeriodCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddDistributorServices();
        builder.Logging.ClearProviders();

        _host = builder.Build();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _host.Dispose();
    }

    [Benchmark]
    public async Task PlanDistribution()
    {
        var scope = _host.Services.CreateAsyncScope();

        await using (scope.ConfigureAwait(false))
        {
            var handler = scope.ServiceProvider.GetRequiredService<
                IRequestHandler<PlanDistributionCommand, PlanDistributionResult>
            >();

            await handler
                .HandleAsync(
                    new PlanDistributionCommand
                    {
                        Start = new PeriodDate(2025, 1),
                        End = new PeriodDate(2025, PeriodCount),
                    }
                )
                .ConfigureAwait(false);
        }
    }

    [IterationCleanup]
    public void CleanupIteration()
    {
        using var scope = _host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DistributorDatabaseContext>();

        context.Database.ExecuteSqlRaw(@"DELETE FROM ""shipments""");
        context.Database.ExecuteSqlRaw(@"DELETE FROM ""distribution_plans""");
    }
}
