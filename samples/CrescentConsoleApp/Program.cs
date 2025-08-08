// See https://aka.ms/new-console-template for more information

using System.Collections.Immutable;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.CrescentConsoleApp;
using Mississippi.EventSourcing;
using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Cosmos;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reader;
using Mississippi.EventSourcing.Writer;

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
logger.LogInformation("Run {RunId}: Host started", runId);
// Resolve and log core services
logger.LogInformation("Run {RunId}: Resolving Orleans grain factory", runId);
IBrookGrainFactory aaa = host.Services.GetRequiredService<IBrookGrainFactory>();

// Log Cosmos options
try
{
    IOptions<BrookStorageOptions> brookOptions = host.Services.GetRequiredService<IOptions<BrookStorageOptions>>();
    logger.LogInformation(
        "Run {RunId}: Cosmos options DatabaseId={DatabaseId}, ContainerId={ContainerId}, LockContainer={LockContainer}, MaxEventsPerBatch={MaxEventsPerBatch}, QueryBatchSize={QueryBatchSize}",
        runId,
        brookOptions.Value.DatabaseId,
        brookOptions.Value.ContainerId,
        brookOptions.Value.LockContainerName,
        brookOptions.Value.MaxEventsPerBatch,
        brookOptions.Value.QueryBatchSize);
}
catch (InvalidOperationException ex)
{
    logger.LogWarning(ex, "Run {RunId}: Unable to resolve BrookStorageOptions", runId);
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
    logger.LogInformation(
        "Run {RunId}: Mode=reuse, Using persisted BrookKey={BrookKey} (state file: {Path})",
        runId,
        brookKey,
        RunStateStore.FilePath);
}
else
{
    brookKey = new($"test-brook-{Guid.NewGuid():N}", "sample-brook-001");
    runState.PrimaryType = brookKey.Type;
    runState.PrimaryId = brookKey.Id;
    logger.LogInformation(
        "Run {RunId}: Mode=fresh, Using new BrookKey={BrookKey} (state file: {Path})",
        runId,
        brookKey,
        RunStateStore.FilePath);
}

// Scenario 1: Small batch append (10 small events)
logger.LogInformation("=== Scenario: SmallBatch_10x1KB ===");
await RunAppendScenarioAsync(
    logger,
    runId,
    aaa,
    brookKey,
    "SmallBatch_10x1KB",
    () => SampleEventFactory.CreateFixedSizeEvents(10, 1024, "application/json"));

// Scenario 2: Bulk 100 mixed-size events
logger.LogInformation("=== Scenario: Bulk_100_Mixed ===");
await RunAppendScenarioAsync(
    logger,
    runId,
    aaa,
    brookKey,
    "Bulk_100_Mixed",
    () => SampleEventFactory.CreateRangeSizeEvents(100, 512, 4096));

// Scenario 3: Large single event (~200KB)
logger.LogInformation("=== Scenario: LargeSingle_200KB ===");
await RunAppendScenarioAsync(
    logger,
    runId,
    aaa,
    brookKey,
    "LargeSingle_200KB",
    () => SampleEventFactory.CreateFixedSizeEvents(1, 200 * 1024));

// Scenario 4: Large batch near request limit (sized to push batching logic)
logger.LogInformation("=== Scenario: LargeBatch_200x5KB ===");
await RunAppendScenarioAsync(
    logger,
    runId,
    aaa,
    brookKey,
    "LargeBatch_200x5KB",
    () => SampleEventFactory.CreateFixedSizeEvents(200, 5 * 1024));

// Scenario 5: Max-op constrained (exactly 100 ops across batches)
logger.LogInformation("=== Scenario: OpsLimit_100_Mixed ===");
await RunAppendScenarioAsync(
    logger,
    runId,
    aaa,
    brookKey,
    "OpsLimit_100_Mixed",
    () => SampleEventFactory.CreateRangeSizeEvents(100, 1024, 4096));

// Read back and log
logger.LogInformation("=== Readback after initial appends ===");
await LogStreamReadAsync(logger, runId, aaa, brookKey);

// Scenario 6: Interleaved read/write on same stream
logger.LogInformation("=== Scenario: Interleaved Read/Write (single stream) ===");
await RunInterleavedReadWriteScenarioAsync(logger, runId, aaa, brookKey);

// Scenario 7: Multi-stream interleaved workload
logger.LogInformation("=== Scenario: Multi-stream interleaved workload ===");
List<StreamState> multiStates = await RunMultiStreamScenarioAsync(logger, runId, aaa);
// Persist multi-stream heads
foreach (StreamState s in multiStates)
{
    runState.UpsertStream(s.Type, s.Id, s.Head);
}

// Scenario 8: Explicit cache flush (deactivate grains) then read again
logger.LogInformation("=== Scenario: Explicit cache flush + readback ===");
await FlushCachesAsync(logger, runId, aaa, brookKey);
await LogStreamReadAsync(logger, runId, aaa, brookKey);

// Scenario 9: Cold start resume: stop host, build a new host, start again, then read
logger.LogInformation("Run {RunId}: Performing cold restart of host...", runId);
await host.StopAsync();
using IHost host2 = BuildColdStartHost();
await host2.StartAsync();
logger.LogInformation("=== Scenario: Cold restart readback ===");
await LogStreamReadAsync(logger, runId, host2.Services.GetRequiredService<IBrookGrainFactory>(), brookKey, true);
// Persist final confirmed head for primary stream
BrookPosition confirmed = await host2.Services.GetRequiredService<IBrookGrainFactory>()
    .GetBrookHeadGrain(brookKey)
    .GetLatestPositionConfirmedAsync();
runState.PrimaryHead = confirmed.Value;
runState.UpsertStream(brookKey.Type, brookKey.Id, confirmed.Value);
await RunStateStore.SaveAsync(runState, logger);
await host2.StopAsync();
await host.StopAsync();
return;

// -------- Local helpers (scenarios and readback) --------
static IHost BuildColdStartHost()
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
    return b.Build();
}

// Scenario harness helpers
static async Task RunAppendScenarioAsync(
    ILogger logger,
    string runId,
    IBrookGrainFactory brookGrainFactory,
    BrookKey brookKey,
    string scenarioName,
    Func<ImmutableArray<BrookEvent>> eventFactory,
    BrookPosition? expectedHead = null,
    CancellationToken cancellationToken = default
)
{
    IBrookWriterGrain writer = brookGrainFactory.GetBrookWriterGrain(brookKey);
    ImmutableArray<BrookEvent> events = eventFactory();
    long totalBytes = events.Sum(e => (long)e.Data.Length);
    logger.LogInformation(
        "Run {RunId} [{Scenario}]: Appending count={Count} totalBytes={Bytes}",
        runId,
        scenarioName,
        events.Length,
        totalBytes);
    DateTimeOffset started = DateTimeOffset.UtcNow;
    try
    {
        BrookPosition newHead = await writer.AppendEventsAsync(events, expectedHead, cancellationToken);
        TimeSpan elapsed = DateTimeOffset.UtcNow - started;
        logger.LogInformation(
            "Run {RunId} [{Scenario}]: Append complete -> head={Head} in {Ms} ms (throughput {RateEvN}/s, {RateMB}/s)",
            runId,
            scenarioName,
            newHead.Value,
            (int)elapsed.TotalMilliseconds,
            events.Length / Math.Max(0.001, elapsed.TotalSeconds),
            totalBytes / 1_000_000.0 / Math.Max(0.001, elapsed.TotalSeconds));
    }
    catch (Exception ex)
    {
        TimeSpan elapsed = DateTimeOffset.UtcNow - started;
        logger.LogError(
            ex,
            "Run {RunId} [{Scenario}]: Append failed after {Ms} ms (attempted count={Count}, bytes={Bytes})",
            runId,
            scenarioName,
            (int)elapsed.TotalMilliseconds,
            events.Length,
            totalBytes);
    }
}

static async Task LogStreamReadAsync(
    ILogger logger,
    string runId,
    IBrookGrainFactory brookGrainFactory,
    BrookKey brookKey,
    bool confirmedHead = false,
    CancellationToken cancellationToken = default
)
{
    IBrookReaderGrain reader = brookGrainFactory.GetBrookReaderGrain(brookKey);
    BrookPosition latest = confirmedHead
        ? await brookGrainFactory.GetBrookHeadGrain(brookKey).GetLatestPositionConfirmedAsync()
        : await brookGrainFactory.GetBrookHeadGrain(brookKey).GetLatestPositionAsync();
    logger.LogInformation("Run {RunId}: Readback head={Head}", runId, latest.Value);
    if (latest.Value < 1)
    {
        logger.LogInformation("Run {RunId}: No events to read", runId);
        return;
    }

    int attempt = 0;
    while (true)
    {
        int readCount = 0;
        long totalBytes = 0;
        DateTimeOffset started = DateTimeOffset.UtcNow;
        try
        {
            await foreach (BrookEvent mississippiEvent in reader.ReadEventsAsync(new(1), latest, cancellationToken))
            {
                readCount++;
                totalBytes += mississippiEvent.Data.Length;
                if ((readCount <= 5) || ((readCount % 50) == 0))
                {
                    logger.LogDebug(
                        "Run {RunId}: Read idx={Idx} id={Id} type={Type} bytes={Bytes}",
                        runId,
                        readCount,
                        mississippiEvent.Id,
                        mississippiEvent.Type,
                        mississippiEvent.Data.Length);
                }
            }

            TimeSpan elapsed = DateTimeOffset.UtcNow - started;
            logger.LogInformation(
                "Run {RunId}: Readback complete count={Count} bytes={Bytes} in {Ms} ms (throughput {RateEvN}/s, {RateMB}/s)",
                runId,
                readCount,
                totalBytes,
                (int)elapsed.TotalMilliseconds,
                readCount / Math.Max(0.001, elapsed.TotalSeconds),
                totalBytes / 1_000_000.0 / Math.Max(0.001, elapsed.TotalSeconds));
            break;
        }
        catch (EnumerationAbortedException ex) when (attempt == 0)
        {
            attempt++;
            logger.LogWarning(ex, "Run {RunId}: Read enumeration aborted; retrying once from start", runId);
            // loop and retry full read once
        }
    }
}

static async Task RunInterleavedReadWriteScenarioAsync(
    ILogger logger,
    string runId,
    IBrookGrainFactory brookGrainFactory,
    BrookKey brookKey,
    CancellationToken cancellationToken = default
)
{
    logger.LogInformation("Run {RunId} [Interleave]: Start", runId);
    IBrookWriterGrain writer = brookGrainFactory.GetBrookWriterGrain(brookKey);
    IBrookReaderGrain reader = brookGrainFactory.GetBrookReaderGrain(brookKey);

    // Write a small batch
    BrookPosition head1 = await writer.AppendEventsAsync(
        SampleEventFactory.CreateFixedSizeEvents(5, 1024),
        null,
        cancellationToken);
    logger.LogInformation("Run {RunId} [Interleave]: Head after write1={Head}", runId, head1.Value);
    // Read a tail subset
    int tailCount = 0;
    long tailStart = Math.Max(1, head1.Value - Math.Min(4, head1.Value));
    await foreach (BrookEvent ignoredEvent in reader.ReadEventsAsync(new(tailStart), head1, cancellationToken))
    {
        tailCount++;
    }

    logger.LogInformation("Run {RunId} [Interleave]: Tail read count={Count}", runId, tailCount);

    // Write another mixed batch
    BrookPosition head2 = await writer.AppendEventsAsync(
        SampleEventFactory.CreateRangeSizeEvents(20, 512, 4096),
        head1,
        cancellationToken);
    logger.LogInformation("Run {RunId} [Interleave]: Head after write2={Head}", runId, head2.Value);

    // Verify continuous read from 1..head2
    int attempts = 0;
    while (true)
    {
        try
        {
            int count = 0;
            await foreach (BrookEvent ignoredEvent in reader.ReadEventsAsync(new(1), head2, cancellationToken))
            {
                count++;
            }

            logger.LogInformation("Run {RunId} [Interleave]: Full range read count={Count}", runId, count);
            break;
        }
        catch (EnumerationAbortedException ex) when (attempts == 0)
        {
            attempts++;
            logger.LogWarning(ex, "Run {RunId} [Interleave]: Enumeration aborted; retrying once", runId);
        }
    }
}

static async Task<List<StreamState>> RunMultiStreamScenarioAsync(
    ILogger logger,
    string runId,
    IBrookGrainFactory brookGrainFactory
)
{
    BrookKey keyA = new($"test-brook-{Guid.NewGuid():N}", "A");
    BrookKey keyB = new($"test-brook-{Guid.NewGuid():N}", "B");
    IBrookWriterGrain wA = brookGrainFactory.GetBrookWriterGrain(keyA);
    IBrookWriterGrain wB = brookGrainFactory.GetBrookWriterGrain(keyB);
    await wA.AppendEventsAsync(SampleEventFactory.CreateFixedSizeEvents(50, 1024));
    await wB.AppendEventsAsync(SampleEventFactory.CreateRangeSizeEvents(50, 512, 4096));

    // Use confirmed heads to avoid cached -1 during immediate readback
    BrookPosition hA = await brookGrainFactory.GetBrookHeadGrain(keyA).GetLatestPositionConfirmedAsync();
    BrookPosition hB = await brookGrainFactory.GetBrookHeadGrain(keyB).GetLatestPositionConfirmedAsync();
    logger.LogInformation("Run {RunId} [Multi]: Heads A={HA} B={HB}", runId, hA.Value, hB.Value);

    // Read a portion from each
    IBrookReaderGrain rA = brookGrainFactory.GetBrookReaderGrain(keyA);
    IBrookReaderGrain rB = brookGrainFactory.GetBrookReaderGrain(keyB);
    int ca = 0, cb = 0;
    if (hA.Value >= 1)
    {
        await foreach (BrookEvent ignoredEvent in rA.ReadEventsAsync(new(1), hA))
        {
            ca++;
        }
    }
    else
    {
        logger.LogInformation("Run {RunId} [Multi]: Stream A empty", runId);
    }

    if (hB.Value >= 1)
    {
        await foreach (BrookEvent ignoredEvent in rB.ReadEventsAsync(new(1), hB))
        {
            cb++;
        }
    }
    else
    {
        logger.LogInformation("Run {RunId} [Multi]: Stream B empty", runId);
    }

    logger.LogInformation("Run {RunId} [Multi]: Read counts A={CA} B={CB}", runId, ca, cb);
    return new()
    {
        new()
        {
            Type = keyA.Type,
            Id = keyA.Id,
            Head = hA.Value,
        },
        new()
        {
            Type = keyB.Type,
            Id = keyB.Id,
            Head = hB.Value,
        },
    };
}

static async Task FlushCachesAsync(
    ILogger logger,
    string runId,
    IBrookGrainFactory brookGrainFactory,
    BrookKey brookKey
)
{
    logger.LogInformation("Run {RunId} [Flush]: Requesting grain deactivations for {BrookKey}", runId, brookKey);
    await brookGrainFactory.GetBrookHeadGrain(brookKey).DeactivateAsync();
    await brookGrainFactory.GetBrookReaderGrain(brookKey).DeactivateAsync();
    // Also trigger slice grains to deactivate: touch a few ranges and request deactivation
    BrookPosition head = await brookGrainFactory.GetBrookHeadGrain(brookKey).GetLatestPositionAsync();
    if (head.Value > 0)
    {
        long step = Math.Max(1, head.Value / 3);
        for (long start = 1; start <= head.Value; start += step)
        {
            long end = Math.Min(head.Value, (start + step) - 1);
            IBrookSliceReaderGrain slice = brookGrainFactory.GetBrookSliceReaderGrain(
                BrookRangeKey.FromBrookCompositeKey(brookKey, start, end - start));
            await slice.DeactivateAsync();
        }
    }
}