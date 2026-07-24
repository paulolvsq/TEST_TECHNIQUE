using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Distributor.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDistributorDatabase(this IServiceCollection services)
    {
        services.AddDbContext<DistributorDatabaseContext>(ConfigureDistributorDatabase);

        return services;
    }

    private static void ConfigureDistributorDatabase(IServiceProvider services, DbContextOptionsBuilder options)
    {
        var provider = services.GetRequiredService<IDistributorDatabaseFileProvider>();
        var file = provider.GetDatabaseFile();

        var directory =
            file.Directory
            ?? throw new InvalidOperationException("Distributor database file must have a parent directory.");

        directory.Create();

        var builder = new SqliteConnectionStringBuilder() { DataSource = file.FullName };

        options.UseSqlite(builder.ConnectionString);
    }
}
