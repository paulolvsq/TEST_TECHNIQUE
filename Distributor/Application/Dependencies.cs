using System.IO.Abstractions;
using Distributor.Data;
using Distributor.Features.EvaluateScenarios;
using Distributor.Features.PlanDistribution;
using Distributor.Matrices;
using Distributor.Periods;
using Distributor.Scenarios;
using Distributor.Stores;
using Distributor.Warehouses;
using Microsoft.Extensions.DependencyInjection;

namespace Distributor.Application;

public static class Dependencies
{
    public static void AddDistributorServices(this IServiceCollection services)
    {
        services.AddDistributorDatabase();
        services.AddSingleton<IFileSystem>(new FileSystem());
        services.AddSingleton<ICommandLineHandler, CommandLineHandler>();

        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IPeriodRepository, PeriodRepository>();
        services.AddScoped<IScenarioRepository, ScenarioRepository>();

        services.AddTransient<IDistributorDatabaseFileProvider, DistributorDatabaseFileProvider>();
        services.AddTransient<IScenarioMatrixFactory, ScenarioMatrixFactory>();
        services.AddTransient<IMatrixMultiplier, MatrixMultiplier>();
        services.AddTransient<IMatrixSplitter, MatrixSplitter>();
        services.AddTransient<IMatrixSpanMultiplier, MatrixSpanMultiplier>();
        services.AddTransient<ITransportSolver, TransportSolver>();

        services.AddScoped<
            IRequestHandler<EvaluateScenariosQuery, EvaluateScenariosResult>,
            EvaluateScenariosQueryHandler
        >();
        services.AddTransient<IPresenter<EvaluateScenariosResult>, EvaluateScenariosResultPresenter>();

        services.AddScoped<
            IRequestHandler<PlanDistributionCommand, PlanDistributionResult>,
            PlanDistributionCommandHandler
        >();

        services.AddTransient<IPresenter<PlanDistributionResult>, PlanDistributionResultPresenter>();
    }
}
