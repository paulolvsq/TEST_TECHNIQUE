using Distributor.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDistributorServices();

var app = builder.Build();

var handler = app.Services.GetRequiredService<ICommandLineHandler>();

await handler.HandleAsync(args).ConfigureAwait(false);
