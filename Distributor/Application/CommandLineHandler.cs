using System.CommandLine;
using Distributor.Features.EvaluateScenarios;
using Distributor.Features.PlanDistribution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Distributor.Application;

public interface ICommandLineHandler
{
    Task HandleAsync(IReadOnlyList<string> arguments);
}

public sealed class CommandLineHandler : ICommandLineHandler
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CommandLineHandler> _logger;

    public CommandLineHandler(IServiceProvider services, ILogger<CommandLineHandler> logger)
    {
        _services = services;
        _logger = logger;
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
        var root = new RootCommand("Distribution network analyzer used for planning shipments.");

        AddSubCommand<EvaluateScenariosOptions, EvaluateScenariosQuery, EvaluateScenariosResult>(root);
        AddSubCommand<PlanDistributionOptions, PlanDistributionCommand, PlanDistributionResult>(root);

        return root;
    }

    private void AddSubCommand<TOptions, TRequest, TResponse>(RootCommand root)
        where TOptions : IRequestOptions<TRequest>, new()
        where TRequest : IRequest<TResponse>
    {
        var options = new TOptions();
        var command = new Command(options.Name, options.Description);

        foreach (var option in options.Enumerate())
        {
            command.Options.Add(option);
        }

        command.SetAction(
            async (result, token) =>
                await HandleAsync<TOptions, TRequest, TResponse>(options, result, token).ConfigureAwait(false)
        );

        root.Subcommands.Add(command);
    }

    private async Task HandleAsync<TOptions, TRequest, TResponse>(
        TOptions options,
        ParseResult result,
        CancellationToken token
    )
        where TOptions : IRequestOptions<TRequest>
        where TRequest : IRequest<TResponse>
    {
        var scope = _services.CreateAsyncScope();

        await using (scope.ConfigureAwait(false))
        {
            try
            {
                var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
                var presenter = scope.ServiceProvider.GetService<IPresenter<TResponse>>();

                var request = options.Parse(result);

                _logger.LogInformation("Handling request {Request}.", typeof(TRequest).Name);

                var response = await handler.HandleAsync(request, token).ConfigureAwait(false);

                _logger.LogInformation(
                    "Responded to request {Request} with response {Response}.",
                    typeof(TRequest).Name,
                    typeof(TResponse).Name
                );

                presenter?.Display(response);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogError(
                    e,
                    "An unexpected error occurred while handling request {Request}. {Exception}",
                    typeof(TRequest).Name,
                    e.ToString()
                );

                throw;
            }
        }
    }
}
