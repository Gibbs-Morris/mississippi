using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.Aqueduct.Grains;
using Mississippi.Common.Abstractions;
using Mississippi.EventSourcing.Brooks;
using Mississippi.EventSourcing.Brooks.Cosmos;
using Mississippi.EventSourcing.Serialization.Json;
using Mississippi.EventSourcing.Snapshots;
using Mississippi.EventSourcing.Snapshots.Cosmos;
using Mississippi.Inlet.Silo;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Orleans.Hosting;
using Orleans.Runtime;

using Spring.Domain.Projections.BankAccountBalance;
using Spring.Silo.Registrations;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Register HttpClient factory for effects that call external APIs
builder.Services.AddHttpClient();

// Register Spring domain aggregates
builder.Services.AddBankAccountAggregate();
builder.Services.AddTransactionInvestigationQueueAggregate();

// Register Spring domain projections
builder.Services.AddBankAccountBalanceProjection();
builder.Services.AddBankAccountLedgerProjection();
builder.Services.AddFlaggedTransactionsProjection();
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Microsoft.Orleans.Runtime")
        .AddSource("Microsoft.Orleans.Application"))
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()

        // Mississippi framework meters
        .AddMeter("Mississippi.EventSourcing.Brooks")
        .AddMeter("Mississippi.EventSourcing.Aggregates")
        .AddMeter("Mississippi.EventSourcing.Snapshots")
        .AddMeter("Mississippi.Storage.Cosmos")
        .AddMeter("Mississippi.Storage.Snapshots")
        .AddMeter("Mississippi.Storage.Locking")

        // Orleans meters
        .AddMeter("Microsoft.Orleans"))
    .WithLogging()
    .UseOtlpExporter();

// Add Aspire-managed Azure Storage clients for Orleans clustering and grain state
// These are configured by the AppHost via WithReference
// Must use Keyed variants so Orleans can resolve via GetRequiredKeyedService<T>(serviceKey)
builder.AddKeyedAzureTableServiceClient("clustering");
builder.AddKeyedAzureBlobServiceClient("grainstate");

// Add Aspire-managed Cosmos client for event sourcing storage (Brooks + Snapshots)
// Gateway mode required for Aspire Cosmos emulator compatibility
builder.AddAzureCosmosClient(
    "cosmos",
    configureClientOptions: options =>
    {
        options.ConnectionMode = ConnectionMode.Gateway;
        options.LimitToEndpoint = true;
    });

// Add Blob client for distributed locking (Brooks)
builder.AddKeyedAzureBlobServiceClient("blobs");

// Forward the Aspire-registered blob client to the Brooks key used by BlobDistributedLockManager
builder.Services.AddKeyedSingleton(
    MississippiDefaults.ServiceKeys.BlobLocking,
    (
        sp,
        _
    ) => sp.GetRequiredKeyedService<BlobServiceClient>("blobs"));

// Forward the Aspire-registered Cosmos client to a shared Mississippi keyed service key
// Both Brooks and Snapshots use the same Cosmos account but different containers
const string sharedCosmosKey = "spring-cosmos";
builder.Services.AddKeyedSingleton(
    sharedCosmosKey,
    (
        sp,
        _
    ) => sp.GetRequiredService<CosmosClient>());

// Add Inlet Orleans services for projection subscription management
builder.Services.AddInletOrleans();
builder.Services.ScanProjectionAssemblies(typeof(BankAccountBalanceProjection).Assembly);

// Add event sourcing infrastructure
builder.Services.AddJsonSerialization();
builder.Services.AddEventSourcingByService();
builder.Services.AddSnapshotCaching();

// Configure Cosmos storage for Brooks (event streams)
builder.Services.AddCosmosBrookStorageProvider(options =>
{
    options.CosmosClientServiceKey = sharedCosmosKey;
    options.DatabaseId = "spring-db";
    options.ContainerId = "events";
    options.QueryBatchSize = 50;
    options.MaxEventsPerBatch = 50;
});

// Configure Cosmos storage for Snapshots
builder.Services.AddCosmosSnapshotStorageProvider(options =>
{
    options.CosmosClientServiceKey = sharedCosmosKey;
    options.DatabaseId = "spring-db";
    options.ContainerId = "snapshots";
    options.QueryBatchSize = 100;
});

// Configure Orleans silo - Aspire injects clustering config via environment variables
builder.UseOrleans(siloBuilder =>
{
    siloBuilder.AddActivityPropagation();

    // Configure Aqueduct to use the Aspire-configured stream provider for SignalR backplane
    siloBuilder.UseAqueduct(options => options.StreamProviderName = "StreamProvider");

    // Configure event sourcing to use the Aspire-configured stream provider
    // Must match the stream provider name configured in AppHost via WithMemoryStreaming
    siloBuilder.AddEventSourcing(options => options.OrleansStreamProviderName = "StreamProvider");
});
WebApplication app = builder.Build();

// Health check endpoint for Aspire orchestration
app.MapGet(
    "/health",
    (
        ISiloStatusOracle siloStatus
    ) =>
    {
        SiloStatus status = siloStatus.CurrentStatus;
        return status == SiloStatus.Active
            ? Results.Ok(
                new
                {
                    Status = "Healthy",
                    Service = "Spring.Silo",
                    Orleans = status.ToString(),
                })
            : Results.StatusCode(503);
    });
await app.RunAsync();