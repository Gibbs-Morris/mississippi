using System;
using System.Collections.Generic;
using System.Net.Http;

using Crescent.ConsoleApp;
using Crescent.ConsoleApp.Counter;
using Crescent.ConsoleApp.CounterSummary;
using Crescent.ConsoleApp.Infrastructure;
using Crescent.ConsoleApp.Scenarios;
using Crescent.ConsoleApp.Shared;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Brooks;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Storage;
using Mississippi.EventSourcing.Brooks.Cosmos;
using Mississippi.EventSourcing.Brooks.Factory;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Serialization.Abstractions;
using Mississippi.EventSourcing.Serialization.Json;
using Mississippi.EventSourcing.Snapshots;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;


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

// Add Cosmos snapshot storage provider with configuration
builder.Services.AddCosmosSnapshotStorageProvider(options =>
{
    options.DatabaseId = "mississippi-dev";
    options.ContainerId = "snapshots";
    options.QueryBatchSize = 100;
});

// Add snapshot caching infrastructure (required for aggregates using snapshots)
builder.Services.AddSnapshotCaching();

// Add snapshot state converter for CounterState (required for snapshot verification)
builder.Services.AddSnapshotStateConverter<CounterState>();

// Add JSON serialization for aggregate events
builder.Services.AddSingleton<ISerializationProvider, JsonSerializationProvider>();

// Add counter aggregate domain services
builder.Services.AddCounterAggregate();
using IHost host = builder.Build();
await host.StartAsync();
ILoggerFactory loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
ILogger logger = loggerFactory.CreateLogger("ConsoleApp");
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

// Test Cosmos DB connectivity before running scenarios
Uri cosmosEmulatorUri = new("https://localhost:8081/");
logger.TestingCosmosConnectivity(runId, cosmosEmulatorUri);
try
{
    using HttpClient httpClient = new();
    httpClient.Timeout = TimeSpan.FromSeconds(5);
    HttpResponseMessage response = await httpClient.GetAsync(cosmosEmulatorUri);
    logger.CosmosConnectivitySuccess(runId, (int)response.StatusCode);
}
catch (HttpRequestException ex)
{
    logger.CosmosConnectivityFailed(runId, ex);
    logger.CosmosEmulatorNotRunning(runId, cosmosEmulatorUri);
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
logger.AboutToWriteToCosmosDb(runId);
ScenarioResult appendResult1 = await AppendScenario.RunAsync(
    logger,
    runId,
    brookFactory,
    brookKey,
    "SmallBatch_10x1KB",
    () => SampleEventFactory.CreateFixedSizeEvents(10, 1024, "application/json"));
LogScenarioResult(logger, appendResult1);

// Scenario 2: Bulk 100 mixed-size events
logger.ScenarioBulk100Mixed();
ScenarioResult appendResult2 = await AppendScenario.RunAsync(
    logger,
    runId,
    brookFactory,
    brookKey,
    "Bulk_100_Mixed",
    () => SampleEventFactory.CreateRangeSizeEvents(100, 512, 4096));
LogScenarioResult(logger, appendResult2);

// Scenario 3: Large single event (~200KB)
logger.ScenarioLargeSingle200KB();
ScenarioResult appendResult3 = await AppendScenario.RunAsync(
    logger,
    runId,
    brookFactory,
    brookKey,
    "LargeSingle_200KB",
    () => SampleEventFactory.CreateFixedSizeEvents(1, 200 * 1024));
LogScenarioResult(logger, appendResult3);

// Scenario 4: Large batch near request limit (sized to push batching logic)
logger.ScenarioLargeBatch200x5KB();
ScenarioResult appendResult4 = await AppendScenario.RunAsync(
    logger,
    runId,
    brookFactory,
    brookKey,
    "LargeBatch_200x5KB",
    () => SampleEventFactory.CreateFixedSizeEvents(200, 5 * 1024));
LogScenarioResult(logger, appendResult4);

// Scenario 5: Max-op constrained (exactly 100 ops across batches)
logger.ScenarioOpsLimit100Mixed();
ScenarioResult appendResult5 = await AppendScenario.RunAsync(
    logger,
    runId,
    brookFactory,
    brookKey,
    "OpsLimit_100_Mixed",
    () => SampleEventFactory.CreateRangeSizeEvents(100, 1024, 4096));
LogScenarioResult(logger, appendResult5);

// Read back and log
logger.ReadbackAfterInitialAppends();
await ReadHelpers.LogStreamReadAsync(logger, runId, brookFactory, brookKey);

// Scenario 6: Interleaved read/write on same stream
logger.ScenarioInterleaved();
ScenarioResult interleavedResult = await InterleavedScenario.RunAsync(logger, runId, brookFactory, brookKey);
LogScenarioResult(logger, interleavedResult);

// Scenario 7: Multi-stream interleaved workload
logger.ScenarioMultiStream();
ScenarioResult multiStreamResult = await MultiStreamScenario.RunAsync(logger, runId, brookFactory);
LogScenarioResult(logger, multiStreamResult);
IReadOnlyList<StreamState> multiStates = MultiStreamScenario.GetStreamStates(multiStreamResult);

// Persist multi-stream cursors
foreach (StreamState s in multiStates)
{
    runState.UpsertStream(s.Type, s.Id, s.Cursor);
}

// Scenario 8: Explicit cache flush (deactivate grains) then read again
logger.ExplicitCacheFlushReadback();
await CacheHelpers.FlushCachesAsync(logger, runId, brookFactory, brookKey);
await ReadHelpers.LogStreamReadAsync(logger, runId, brookFactory, brookKey);

// ============================================================================
// Aggregate Scenarios - Testing command/event aggregate patterns
// ============================================================================
IGrainFactory grainFactory = host.Services.GetRequiredService<IGrainFactory>();

// Scenario 9: Basic aggregate lifecycle (init → increment → decrement → reset)
logger.ScenarioAggregateBasicLifecycle();
ScenarioResult aggLifecycleResult = await AggregateScenario.RunBasicLifecycleAsync(
    logger,
    runId,
    grainFactory,
    $"counter-{Guid.NewGuid():N}");
LogScenarioResult(logger, aggLifecycleResult);

// Scenario 10: Validation error scenarios
logger.ScenarioAggregateValidation();
ScenarioResult aggValidationResult = await AggregateScenario.RunValidationScenarioAsync(
    logger,
    runId,
    grainFactory,
    $"counter-validation-{Guid.NewGuid():N}");
LogScenarioResult(logger, aggValidationResult);

// Scenario 11: Concurrency/version tracking
logger.ScenarioAggregateConcurrency();
ScenarioResult aggConcurrencyResult = await AggregateScenario.RunConcurrencyScenarioAsync(
    logger,
    runId,
    grainFactory,
    $"counter-concurrency-{Guid.NewGuid():N}");
LogScenarioResult(logger, aggConcurrencyResult);

// Scenario 12: Throughput test (100 rapid operations)
logger.ScenarioAggregateThroughput();
ScenarioResult aggThroughputResult = await AggregateScenario.RunThroughputScenarioAsync(
    logger,
    runId,
    grainFactory,
    $"counter-throughput-{Guid.NewGuid():N}");
LogScenarioResult(logger, aggThroughputResult);

// ============================================================================
// End-to-End Verification Scenario - Validates event stream persistence
// ============================================================================

// Scenario 13: End-to-end verification (aggregate → events in Cosmos)
logger.ScenarioVerificationEndToEnd();
IBrookStorageProvider brookStorageProvider = host.Services.GetRequiredService<IBrookStorageProvider>();
ISnapshotStorageProvider snapshotStorageProvider = host.Services.GetRequiredService<ISnapshotStorageProvider>();
ISnapshotStateConverter<CounterState> snapshotStateConverter =
    host.Services.GetRequiredService<ISnapshotStateConverter<CounterState>>();
IRootReducer<CounterState> counterRootReducer = host.Services.GetRequiredService<IRootReducer<CounterState>>();
ScenarioResult verificationResult = await VerificationScenario.RunEndToEndVerificationAsync(
    logger,
    runId,
    grainFactory,
    brookStorageProvider,
    snapshotStorageProvider,
    snapshotStateConverter,
    counterRootReducer,
    $"counter-verify-{Guid.NewGuid():N}");
LogScenarioResult(logger, verificationResult);

// ============================================================================
// Simple UX Projection Scenario - Basic end-to-end flow validation
// ============================================================================

// Scenario 14: Simple UX projection (fresh ID each run: aggregate → events → projection)
logger.ScenarioSimpleUxProjection();
IUxProjectionGrainFactory uxProjectionGrainFactory = host.Services.GetRequiredService<IUxProjectionGrainFactory>();
ScenarioResult simpleUxResult = await SimpleUxProjectionScenario.RunAsync(
    logger,
    runId,
    grainFactory,
    uxProjectionGrainFactory);
LogScenarioResult(logger, simpleUxResult);

// ============================================================================
// UX Projection Scenario - Validates projection snapshot persistence
// ============================================================================

// Scenario 15: UX projection end-to-end (aggregate → projection → snapshot in Cosmos)
logger.ScenarioUxProjectionEndToEnd(runId);
ISnapshotStateConverter<CounterSummaryProjection> summarySnapshotStateConverter =
    host.Services.GetRequiredService<ISnapshotStateConverter<CounterSummaryProjection>>();
IRootReducer<CounterSummaryProjection> summaryRootReducer =
    host.Services.GetRequiredService<IRootReducer<CounterSummaryProjection>>();
ScenarioResult uxProjectionResult = await UxProjectionScenario.RunEndToEndUxProjectionAsync(
    logger,
    runId,
    grainFactory,
    uxProjectionGrainFactory,
    snapshotStorageProvider,
    summarySnapshotStateConverter,
    summaryRootReducer,
    $"counter-ux-proj-{Guid.NewGuid():N}");
LogScenarioResult(logger, uxProjectionResult);

// ============================================================================
// Comprehensive E2E Test Suite - Multiple scenarios validating full pipeline
// ============================================================================

// Scenario 16: Comprehensive E2E test suite
ScenarioResult e2eSuiteResult = await ComprehensiveE2EScenarios.RunAllAsync(
    logger,
    runId,
    grainFactory,
    uxProjectionGrainFactory);
LogScenarioResult(logger, e2eSuiteResult);
(int e2ePassed, int e2eFailed, int e2eTotal) = ComprehensiveE2EScenarios.GetCounts(e2eSuiteResult);
if (e2eFailed > 0)
{
    logger.SimpleUxFailed(runId, "ComprehensiveE2E", $"{e2eFailed}/{e2eTotal} scenarios failed");
}

logger.E2ESuiteComplete(runId, e2ePassed, e2eFailed, e2eTotal, 0);

// ============================================================================
// Cold Restart - Validates persistence across host restart
// ============================================================================

// Scenario 17: Cold start resume: stop host, build a new host, start again, then read
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

// Persist final confirmed cursor for primary stream
BrookPosition confirmed = await host2.Services.GetRequiredService<IBrookGrainFactory>()
    .GetBrookCursorGrain(brookKey)
    .GetLatestPositionConfirmedAsync();
runState.PrimaryCursor = confirmed.Value;
runState.UpsertStream(brookKey.Type, brookKey.Id, confirmed.Value);
await RunStateStore.SaveAsync(runState, logger);
await host2.StopAsync();
await host.StopAsync();
return;

static void LogScenarioResult(
    ILogger scenarioLogger,
    ScenarioResult result
)
{
    if (result.Passed)
    {
        scenarioLogger.ScenarioComplete(result.ScenarioName, result.ElapsedMs, result.Message ?? string.Empty);
    }
    else
    {
        scenarioLogger.ScenarioFailed(result.ScenarioName, result.Message ?? "Unknown error");
    }
}