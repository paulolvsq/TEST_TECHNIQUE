using Distributor.Data;
using Microsoft.EntityFrameworkCore;

namespace Distributor.Tests.Data;

public sealed class TestDatabase : IAsyncDisposable
{
    private readonly string _path;

    public TestDatabase()
    {
        _path = Path.Combine(Path.GetTempPath(), $"distributor_{Guid.CreateVersion7():N}.db");
    }

    public async Task<TestDatabase> CreateAsync()
    {
        await using var context = CreateDatabaseContext();
        await context.Database.EnsureCreatedAsync();

        return this;
    }

    public async Task<TestDatabase> CreateAsync(Func<DistributorDatabaseContext, Task> function)
    {
        await using var context = CreateDatabaseContext();
        await context.Database.EnsureCreatedAsync();
        await function(context);

        return this;
    }

    public DistributorDatabaseContext CreateDatabaseContext()
    {
        var connectionString = $"Data Source={_path};Pooling=False";
        var options = new DbContextOptionsBuilder<DistributorDatabaseContext>().UseSqlite(connectionString).Options;

        return new DistributorDatabaseContext(options);
    }

    public ValueTask DisposeAsync()
    {
        File.Delete(_path);

        return ValueTask.CompletedTask;
    }
}
