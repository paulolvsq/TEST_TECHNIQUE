using System.Data.Common;
using Distributor.Data;
using Distributor.Periods;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Distributor.Seeder;

public interface IDistributorDatabaseSeeder
{
    Task SeedAsync(NetworkSize size, CancellationToken token = default);
    Task ResetAsync(NetworkSize size, CancellationToken token = default);
}

public sealed class DistributorDatabaseSeeder : IDistributorDatabaseSeeder
{
    private readonly DistributorDatabaseContext _context;
    private readonly IDistributorDatabaseFileProvider _provider;
    private readonly ILogger<DistributorDatabaseSeeder> _logger;

    public DistributorDatabaseSeeder(
        DistributorDatabaseContext context,
        IDistributorDatabaseFileProvider provider,
        ILogger<DistributorDatabaseSeeder> logger
    )
    {
        _context = context;
        _provider = provider;
        _logger = logger;
    }

    public async Task SeedAsync(NetworkSize size, CancellationToken token = default)
    {
        var database = _provider.GetDatabaseFile();

        _logger.LogInformation("Creating database {Database} if necessary.", database.FullName);

        await _context.Database.EnsureCreatedAsync(token).ConfigureAwait(false);

        if (await _context.Warehouses.AnyAsync(token).ConfigureAwait(false))
        {
            _logger.LogInformation("Database {Database} is already seeded.", database.FullName);

            return;
        }

        try
        {
            _logger.LogInformation("Seeding database {Database}.", database.FullName);

            await SeedDatabaseAsync(size, token).ConfigureAwait(false);

            _logger.LogInformation("Seeded database {Database}.", database.FullName);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to seed database {Database}.", database.FullName);

            throw;
        }
    }

    public async Task ResetAsync(NetworkSize size, CancellationToken token = default)
    {
        var database = _provider.GetDatabaseFile();

        _logger.LogInformation("Deleting database {Database} if necessary.", database.FullName);

        database.Delete();

        await SeedAsync(size, token).ConfigureAwait(false);
    }

    private async Task SeedDatabaseAsync(NetworkSize size, CancellationToken token)
    {
        var dataset = new DistributorDatasetBuilder()
            .WithSize(size)
            .WithStartPeriod(new PeriodDate(year: 2026, month: 1))
            .WithPeriodCount(12)
            .WithSeed(0)
            .Build();

        var connection = _context.Database.GetDbConnection();

        await connection.OpenAsync(token).ConfigureAwait(false);

        var transaction = await connection.BeginTransactionAsync(token).ConfigureAwait(false);

        await using (transaction.ConfigureAwait(false))
        {
            try
            {
                await AddWarehousesAsync(dataset, connection, token).ConfigureAwait(false);
                await AddStoresAsync(dataset, connection, token).ConfigureAwait(false);
                await AddPeriodsAsync(dataset, connection, token).ConfigureAwait(false);
                await AddWarehouseCapacitiesAsync(dataset, connection, token).ConfigureAwait(false);
                await AddStoreDemandsAsync(dataset, connection, token).ConfigureAwait(false);
                await AddRouteCostsAsync(dataset, connection, token).ConfigureAwait(false);
                await AddScenariosAsync(dataset, connection, token).ConfigureAwait(false);
                await AddScenarioWarehouseAdjustmentsAsync(dataset, connection, token).ConfigureAwait(false);
                await AddScenarioStoreAdjustmentsAsync(dataset, connection, token).ConfigureAwait(false);

                await transaction.CommitAsync(token).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);

                throw;
            }
        }
    }

    private async Task AddWarehousesAsync(DistributorDataset dataset, DbConnection connection, CancellationToken token)
    {
        var command = connection.CreateCommand();

        await using (command.ConfigureAwait(false))
        {
            command.CommandText = """
                INSERT INTO "warehouses" ("Id", "Name")
                VALUES ($Id, $Name);
                """;

            var id = AddParameter(command, "$Id");
            var name = AddParameter(command, "$Name");

            foreach (var warehouse in dataset.Warehouses)
            {
                id.Value = warehouse.Id;
                name.Value = warehouse.Name;

                await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Added warehouses to database.");
    }

    private async Task AddStoresAsync(DistributorDataset dataset, DbConnection connection, CancellationToken token)
    {
        var command = connection.CreateCommand();

        await using (command.ConfigureAwait(false))
        {
            command.CommandText = """
                INSERT INTO "stores" ("Id", "Name")
                VALUES ($Id, $Name);
                """;

            var id = AddParameter(command, "$Id");
            var name = AddParameter(command, "$Name");

            foreach (var store in dataset.Stores)
            {
                id.Value = store.Id;
                name.Value = store.Name;

                await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Added stores to database.");
    }

    private async Task AddPeriodsAsync(DistributorDataset dataset, DbConnection connection, CancellationToken token)
    {
        var command = connection.CreateCommand();

        await using (command.ConfigureAwait(false))
        {
            command.CommandText = """
                INSERT INTO "periods" ("Id", "Year", "Month")
                VALUES ($Id, $Year, $Month);
                """;

            var id = AddParameter(command, "$Id");
            var year = AddParameter(command, "$Year");
            var month = AddParameter(command, "$Month");

            foreach (var period in dataset.Periods)
            {
                id.Value = period.Id;
                year.Value = period.Year;
                month.Value = period.Month;

                await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Added periods to database.");
    }

    private async Task AddWarehouseCapacitiesAsync(
        DistributorDataset dataset,
        DbConnection connection,
        CancellationToken token
    )
    {
        var command = connection.CreateCommand();

        await using (command.ConfigureAwait(false))
        {
            command.CommandText = """
                INSERT INTO "warehouse_capacities" ("PeriodId", "WarehouseId", "Units")
                VALUES ($PeriodId, $WarehouseId, $Units);
                """;

            var periodId = AddParameter(command, "$PeriodId");
            var warehouseId = AddParameter(command, "$WarehouseId");
            var units = AddParameter(command, "$Units");

            foreach (var capacity in dataset.Capacities)
            {
                periodId.Value = capacity.PeriodId;
                warehouseId.Value = capacity.WarehouseId;
                units.Value = capacity.Units;

                await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Added warehouse capacities to database.");
    }

    private async Task AddStoreDemandsAsync(
        DistributorDataset dataset,
        DbConnection connection,
        CancellationToken token
    )
    {
        var command = connection.CreateCommand();

        await using (command.ConfigureAwait(false))
        {
            command.CommandText = """
                INSERT INTO "store_demands" ("PeriodId", "StoreId", "Units")
                VALUES ($PeriodId, $StoreId, $Units);
                """;

            var periodId = AddParameter(command, "$PeriodId");
            var storeId = AddParameter(command, "$StoreId");
            var units = AddParameter(command, "$Units");

            foreach (var demand in dataset.Demands)
            {
                periodId.Value = demand.PeriodId;
                storeId.Value = demand.StoreId;
                units.Value = demand.Units;

                await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Added store demands to database.");
    }

    private async Task AddRouteCostsAsync(DistributorDataset dataset, DbConnection connection, CancellationToken token)
    {
        var command = connection.CreateCommand();

        await using (command.ConfigureAwait(false))
        {
            command.CommandText = """
                INSERT INTO "route_costs" ("PeriodId", "WarehouseId", "StoreId", "UnitCost")
                VALUES ($PeriodId, $WarehouseId, $StoreId, $UnitCost);
                """;

            var periodId = AddParameter(command, "$PeriodId");
            var warehouseId = AddParameter(command, "$WarehouseId");
            var storeId = AddParameter(command, "$StoreId");
            var unitCost = AddParameter(command, "$UnitCost");

            foreach (var cost in dataset.Costs)
            {
                periodId.Value = cost.PeriodId;
                warehouseId.Value = cost.WarehouseId;
                storeId.Value = cost.StoreId;
                unitCost.Value = cost.UnitCost;

                await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Added route costs to database.");
    }

    private async Task AddScenariosAsync(DistributorDataset dataset, DbConnection connection, CancellationToken token)
    {
        var command = connection.CreateCommand();

        await using (command.ConfigureAwait(false))
        {
            command.CommandText = """
                INSERT INTO "scenarios" ("Id", "Name")
                VALUES ($Id, $Name);
                """;

            var id = AddParameter(command, "$Id");
            var name = AddParameter(command, "$Name");

            foreach (var scenario in dataset.Scenarios)
            {
                id.Value = scenario.Id;
                name.Value = scenario.Name;

                await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Added scenarios to database.");
    }

    private async Task AddScenarioWarehouseAdjustmentsAsync(
        DistributorDataset dataset,
        DbConnection connection,
        CancellationToken token
    )
    {
        var command = connection.CreateCommand();

        await using (command.ConfigureAwait(false))
        {
            command.CommandText = """
                INSERT INTO "scenario_warehouse_adjustments" ("ScenarioId", "WarehouseId", "Multiplier")
                VALUES ($ScenarioId, $WarehouseId, $Multiplier);
                """;

            var scenarioId = AddParameter(command, "$ScenarioId");
            var warehouseId = AddParameter(command, "$WarehouseId");
            var multiplier = AddParameter(command, "$Multiplier");

            foreach (var adjustment in dataset.ScenarioWarehouseAdjustments)
            {
                scenarioId.Value = adjustment.ScenarioId;
                warehouseId.Value = adjustment.WarehouseId;
                multiplier.Value = adjustment.Multiplier;

                await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Added scenario warehouse adjustments to database.");
    }

    private async Task AddScenarioStoreAdjustmentsAsync(
        DistributorDataset dataset,
        DbConnection connection,
        CancellationToken token
    )
    {
        var command = connection.CreateCommand();

        await using (command.ConfigureAwait(false))
        {
            command.CommandText = """
                INSERT INTO "scenario_store_adjustments" ("ScenarioId", "StoreId", "Multiplier")
                VALUES ($ScenarioId, $StoreId, $Multiplier);
                """;

            var scenarioId = AddParameter(command, "$ScenarioId");
            var storeId = AddParameter(command, "$StoreId");
            var multiplier = AddParameter(command, "$Multiplier");

            foreach (var adjustment in dataset.ScenarioStoreAdjustments)
            {
                scenarioId.Value = adjustment.ScenarioId;
                storeId.Value = adjustment.StoreId;
                multiplier.Value = adjustment.Multiplier;

                await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Added scenario store adjustments to database.");
    }

    private static DbParameter AddParameter(DbCommand command, string name)
    {
        var parameter = command.CreateParameter();

        parameter.ParameterName = name;
        command.Parameters.Add(parameter);

        return parameter;
    }
}
