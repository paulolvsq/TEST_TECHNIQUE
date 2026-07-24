using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace Distributor.Seeder;

public interface ICommandLineHandler
{
    Task HandleAsync(IReadOnlyList<string> arguments);
}

public sealed class CommandLineHandler : ICommandLineHandler
{
    private readonly IServiceProvider _services;

    public CommandLineHandler(IServiceProvider services)
    {
        _services = services;
    }

    public async Task HandleAsync(IReadOnlyList<string> arguments)
    {
        using var source = new CancellationTokenSource();
        var token = source.Token;
        var exitCode = 0;

        Console.CancelKeyPress += OnCancelled;

        try
        {
            var command = BuildCommand();
            var result = command.Parse(arguments);

            await result.InvokeAsync(configuration: null, token).ConfigureAwait(false);
        }
        catch
        {
            exitCode = 1;
        }
        finally
        {
            Console.CancelKeyPress -= OnCancelled;

            Environment.Exit(exitCode);
        }

        void OnCancelled(object? _, ConsoleCancelEventArgs arguments)
        {
            arguments.Cancel = true;
            source.Cancel();
        }
    }

    private RootCommand BuildCommand()
    {
        var root = new RootCommand("Distributor database seeder.");

        var smallOption = new Option<bool>("--small", "-s") { Description = "Seed a small network." };
        var largeOption = new Option<bool>("--large", "-l") { Description = "Seed a large network." };

        var seed = new Command("seed", "Create and seed the database if not already seeded.");
        seed.Options.Add(smallOption);
        seed.Options.Add(largeOption);
        seed.SetAction(
            async (result, token) =>
            {
                var size = ParseNetworkSize(result, smallOption, largeOption);
                await SeedAsync(size, token).ConfigureAwait(false);
            }
        );
        root.Subcommands.Add(seed);

        var reset = new Command("reset", "Delete the database and recreate it with seed data.");
        reset.Options.Add(smallOption);
        reset.Options.Add(largeOption);
        reset.SetAction(
            async (result, token) =>
            {
                var size = ParseNetworkSize(result, smallOption, largeOption);
                await ResetAsync(size, token).ConfigureAwait(false);
            }
        );
        root.Subcommands.Add(reset);

        return root;
    }

    private static NetworkSize ParseNetworkSize(ParseResult result, Option<bool> smallOption, Option<bool> largeOption)
    {
        var isSmall = result.GetValue(smallOption);
        var isLarge = result.GetValue(largeOption);

        if (isSmall && isLarge)
        {
            throw new InvalidOperationException("Cannot specify both --small and --large.");
        }

        if (isLarge)
        {
            return NetworkSize.Large;
        }

        return NetworkSize.Small;
    }

    private async Task SeedAsync(NetworkSize size, CancellationToken token)
    {
        var scope = _services.CreateAsyncScope();

        await using (scope.ConfigureAwait(false))
        {
            var seeder = scope.ServiceProvider.GetRequiredService<IDistributorDatabaseSeeder>();

            await seeder.SeedAsync(size, token).ConfigureAwait(false);
        }
    }

    private async Task ResetAsync(NetworkSize size, CancellationToken token)
    {
        var scope = _services.CreateAsyncScope();

        await using (scope.ConfigureAwait(false))
        {
            var seeder = scope.ServiceProvider.GetRequiredService<IDistributorDatabaseSeeder>();

            await seeder.ResetAsync(size, token).ConfigureAwait(false);
        }
    }
}
