using Crescent.ConsoleApp.Counter;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing;
using Mississippi.EventSourcing.Cosmos;
using Mississippi.EventSourcing.Snapshots.Cosmos;

using Orleans.Configuration;
using Orleans.Hosting;


namespace Crescent.ConsoleApp.Infrastructure;

/// <summary>
///     Factory for creating hosts used by cold-start scenarios.
/// </summary>
internal static class HostFactory
{
    /// <summary>
    ///     Builds a new host for cold-start testing with consistent configuration.
    /// </summary>
    /// <returns>A configured <see cref="IHost" /> instance ready to start.</returns>
    public static IHost BuildColdStartHost()
    {
        HostApplicationBuilder b = Host.CreateApplicationBuilder([]);
        b.Logging.ClearProviders();
        b.Logging.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss.fff ";
        });
        b.Logging.SetMinimumLevel(LogLevel.Trace);
        b.AddEventSourcing();
        b.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering()
                .Configure<ClusterOptions>(opt =>
                {
                    opt.ClusterId = "dev";
                    opt.ServiceId = "SampleApp";
                })
                .AddMemoryGrainStorage("PubSubStore");
            silo.ConfigureLogging(lb =>
            {
                lb.AddFilter("Orleans", LogLevel.Debug);
                lb.AddFilter("Orleans.Runtime", LogLevel.Debug);
                lb.AddFilter("Orleans.Streams", LogLevel.Debug);
                lb.AddFilter("Orleans.Hosting", LogLevel.Debug);
                lb.AddFilter("Mississippi", LogLevel.Trace);
            });
        });
        b.Services.AddCosmosBrookStorageProvider(
            "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            "UseDevelopmentStorage=true",
            o =>
            {
                o.DatabaseId = "mississippi-dev";
                o.QueryBatchSize = 50;
                o.MaxEventsPerBatch = 50;
            });
        b.Services.AddCosmosSnapshotStorageProvider(options =>
        {
            options.DatabaseId = "mississippi-dev";
            options.ContainerId = "snapshots";
            options.QueryBatchSize = 100;
        });
        return b.Build();
    }
}
