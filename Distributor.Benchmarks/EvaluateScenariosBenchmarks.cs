using BenchmarkDotNet.Attributes;
using Distributor.Application;
using Distributor.Features.EvaluateScenarios;
using Distributor.Periods;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Distributor.Benchmarks;

[MemoryDiagnoser]
public class EvaluateScenariosBenchmarks
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
    public async Task EvaluateScenarios()
    {
        var scope = _host.Services.CreateAsyncScope();

        await using (scope.ConfigureAwait(false))
        {
            var handler = scope.ServiceProvider.GetRequiredService<
                IRequestHandler<EvaluateScenariosQuery, EvaluateScenariosResult>
            >();

            await handler
                .HandleAsync(
                    new EvaluateScenariosQuery
                    {
                        Start = new PeriodDate(2026, 1),
                        End = new PeriodDate(2026, PeriodCount),
                        ScenarioIds = [1, 2, 3, 4, 5, 6, 7, 8, 9],
                    }
                )
                .ConfigureAwait(false);
        }
    }
}
