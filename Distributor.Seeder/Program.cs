using System.IO.Abstractions;
using Distributor.Data;
using Distributor.Seeder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDistributorDatabase();
builder.Services.AddSingleton<IFileSystem>(new FileSystem());
builder.Services.AddTransient<IDistributorDatabaseFileProvider, DistributorDatabaseFileProvider>();
builder.Services.AddTransient<IDistributorDatabaseSeeder, DistributorDatabaseSeeder>();
builder.Services.AddSingleton<ICommandLineHandler, CommandLineHandler>();

var app = builder.Build();

var handler = app.Services.GetRequiredService<ICommandLineHandler>();

await handler.HandleAsync(args).ConfigureAwait(false);
