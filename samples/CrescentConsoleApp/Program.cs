//

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.CrescentConsoleApp;
using Mississippi.EventSourcing;
using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Cosmos;
using Mississippi.EventSourcing.Factory;

using Orleans.Configuration;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Verbose logging for diagnostics
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss.fff ";
});
builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Logging.AddFilter("Microsoft", LogLevel.Information);
builder.Logging.AddFilter("Orleans", LogLevel.Debug);
builder.Logging.AddFilter("Mississippi", LogLevel.Trace);

// Add event sourcing with Orleans configuration
builder.AddEventSourcing();

// Configure Orleans clustering
builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering()
        .Configure<ClusterOptions>(opt =>
        {
            opt.ClusterId = "dev";
            opt.ServiceId = "SampleApp";
        })
        .AddMemoryGrainStorage("PubSubStore");

    // Optional: raise Orleans internal logging verbosity
    silo.ConfigureLogging(lb =>
    {
        lb.AddFilter("Orleans", LogLevel.Debug);
        lb.AddFilter("Orleans.Runtime", LogLevel.Debug);
        lb.AddFilter("Orleans.Streams", LogLevel.Debug);
        lb.AddFilter("Orleans.Hosting", LogLevel.Debug);
        lb.AddFilter("Mississippi", LogLevel.Trace);
    });
});

// Add Cosmos storage provider with configuration
builder.Services.AddCosmosBrookStorageProvider(
    "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "UseDevelopmentStorage=true", // Use Azurite for local development
    options =>
    {
        options.DatabaseId = "mississippi-dev";
        options.QueryBatchSize = 50;
        options.MaxEventsPerBatch = 50;
    });
using IHost host = builder.Build();
await host.StartAsync();
ILoggerFactory loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
ILogger logger = loggerFactory.CreateLogger("CrescentConsoleApp");
string runId = Guid.NewGuid().ToString("N");
logger.HostStarted(runId);

// Resolve and log core services
logger.ResolvingGrainFactory(runId);
IBrookGrainFactory brookFactory = host.Services.GetRequiredService<IBrookGrainFactory>();

// Log Cosmos options
try
{
    IOptions<BrookStorageOptions> brookOptions = host.Services.GetRequiredService<IOptions<BrookStorageOptions>>();
    logger.CosmosOptions(
        runId,
        brookOptions.Value.DatabaseId,
        brookOptions.Value.ContainerId,
        brookOptions.Value.LockContainerName,
        brookOptions.Value.MaxEventsPerBatch,
        brookOptions.Value.QueryBatchSize);
}
catch (InvalidOperationException ex)
{
    logger.UnableToResolveBrookStorageOptions(runId, ex);
}

// Load persisted run state and decide mode (fresh or reuse)
string mode = Environment.GetEnvironmentVariable("CRESCENT_MODE") ?? builder.Configuration["mode"] ?? "fresh";
RunState runState = await RunStateStore.LoadAsync(logger);
BrookKey brookKey;
if (string.Equals(mode, "reuse", StringComparison.OrdinalIgnoreCase) &&
    !string.IsNullOrWhiteSpace(runState.PrimaryType) &&
    !string.IsNullOrWhiteSpace(runState.PrimaryId))
{
    brookKey = new(runState.PrimaryType!, runState.PrimaryId!);
    logger.ModeReuseUsingPersistedBrookKey(runId, brookKey, RunStateStore.FilePath);
}
else
{
    brookKey = new($"test-brook-{Guid.NewGuid():N}", "sample-brook-001");
    runState.PrimaryType = brookKey.Type;
    runState.PrimaryId = brookKey.Id;
    logger.ModeFreshUsingNewBrookKey(runId, brookKey, RunStateStore.FilePath);
}

// Scenario 1: Small batch append (10 small events)
logger.ScenarioSmallBatch10x1KB();
await AppendScenarioRunner.RunAsync(
    logger,
    runId,
    brookFactory,
    brookKey,
    "SmallBatch_10x1KB",
    () => SampleEventFactory.CreateFixedSizeEvents(10, 1024, "application/json"));

// Scenario 2: Bulk 100 mixed-size events
logger.ScenarioBulk100Mixed();
await AppendScenarioRunner.RunAsync(
    logger,
    runId,
    brookFactory,
    brookKey,
    "Bulk_100_Mixed",
    () => SampleEventFactory.CreateRangeSizeEvents(100, 512, 4096));

// Scenario 3: Large single event (~200KB)
logger.ScenarioLargeSingle200KB();
await AppendScenarioRunner.RunAsync(
    logger,
    runId,
    brookFactory,
    brookKey,
    "LargeSingle_200KB",
    () => SampleEventFactory.CreateFixedSizeEvents(1, 200 * 1024));

// Scenario 4: Large batch near request limit (sized to push batching logic)
logger.ScenarioLargeBatch200x5KB();
await AppendScenarioRunner.RunAsync(
    logger,
    runId,
    brookFactory,
    brookKey,
    "LargeBatch_200x5KB",
    () => SampleEventFactory.CreateFixedSizeEvents(200, 5 * 1024));

// Scenario 5: Max-op constrained (exactly 100 ops across batches)
logger.ScenarioOpsLimit100Mixed();
await AppendScenarioRunner.RunAsync(
    logger,
    runId,
    brookFactory,
    brookKey,
    "OpsLimit_100_Mixed",
    () => SampleEventFactory.CreateRangeSizeEvents(100, 1024, 4096));

// Read back and log
logger.ReadbackAfterInitialAppends();
await ReadHelpers.LogStreamReadAsync(logger, runId, brookFactory, brookKey);

// Scenario 6: Interleaved read/write on same stream
logger.ScenarioInterleaved();
await InterleavedScenario.RunAsync(logger, runId, brookFactory, brookKey);

// Scenario 7: Multi-stream interleaved workload
logger.ScenarioMultiStream();
List<StreamState> multiStates = await MultiStreamScenario.RunAsync(logger, runId, brookFactory);

// Persist multi-stream heads
foreach (StreamState s in multiStates)
{
    runState.UpsertStream(s.Type, s.Id, s.Head);
}

// Scenario 8: Explicit cache flush (deactivate grains) then read again
logger.ExplicitCacheFlushReadback();
await CacheHelpers.FlushCachesAsync(logger, runId, brookFactory, brookKey);
await ReadHelpers.LogStreamReadAsync(logger, runId, brookFactory, brookKey);

// Scenario 9: Cold start resume: stop host, build a new host, start again, then read
logger.PerformingColdRestartOfHost(runId);
await host.StopAsync();
using IHost host2 = HostFactory.BuildColdStartHost();
await host2.StartAsync();
logger.ScenarioColdRestartReadback();
await ReadHelpers.LogStreamReadAsync(
    logger,
    runId,
    host2.Services.GetRequiredService<IBrookGrainFactory>(),
    brookKey,
    true);

// Persist final confirmed head for primary stream
BrookPosition confirmed = await host2.Services.GetRequiredService<IBrookGrainFactory>()
    .GetBrookHeadGrain(brookKey)
    .GetLatestPositionConfirmedAsync();
runState.PrimaryHead = confirmed.Value;
runState.UpsertStream(brookKey.Type, brookKey.Id, confirmed.Value);
await RunStateStore.SaveAsync(runState, logger);
await host2.StopAsync();
await host.StopAsync();
