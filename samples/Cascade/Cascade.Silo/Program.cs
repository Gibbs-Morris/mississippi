using Azure.Storage.Blobs;

using Cascade.Domain;
using Cascade.Domain.Projections.ChannelMessages;

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
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos;
using Mississippi.Inlet.Orleans;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Orleans.Hosting;
using Orleans.Runtime;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
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
        .AddMeter("Mississippi.EventSourcing.UxProjections")
        .AddMeter("Mississippi.Aqueduct")
        .AddMeter("Mississippi.Inlet")
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
// See: https://github.com/dotnet/aspire/issues/5364
builder.AddAzureCosmosClient(
    "cosmos",
    configureClientOptions: options =>
    {
        options.ConnectionMode = ConnectionMode.Gateway;
        options.LimitToEndpoint = true;
    });

// Add Blob client for distributed locking (Brooks)
// Uses the "blobs" connection string from AppHost's WithReference(blobs)
// Register with the Brooks key so BlobDistributedLockManager can resolve it via [FromKeyedServices]
builder.AddKeyedAzureBlobServiceClient("blobs");

// Forward the Aspire-registered blob client to the Brooks key used by BlobDistributedLockManager
builder.Services.AddKeyedSingleton(
    MississippiDefaults.ServiceKeys.BlobLocking,
    (
        sp,
        _
    ) => sp.GetRequiredKeyedService<BlobServiceClient>("blobs"));

// Add Cascade domain services (aggregates, handlers, reducers, projections)
builder.Services.AddCascadeDomain();

// Add Inlet Orleans services for projection subscription management
// The InletSubscriptionGrain needs IProjectionBrookRegistry to map projection types to brooks
builder.Services.AddInletOrleans();
builder.Services.ScanProjectionAssemblies(typeof(ChannelMessagesProjection).Assembly);

// Add event sourcing infrastructure
builder.Services.AddJsonSerialization();
builder.Services.AddEventSourcingByService();
builder.Services.AddSnapshotCaching();

// Configure snapshot retention to persist more frequently (every 25 events)
// This reduces activation time by limiting the maximum number of events to replay
builder.Services.Configure<SnapshotRetentionOptions>(options => options.DefaultRetainModulus = 100);

// Configure Cosmos storage for Brooks (event streams)
builder.Services.AddCosmosBrookStorageProvider(options =>
{
    options.DatabaseId = "cascade-web-db";
    options.QueryBatchSize = 50;
    options.MaxEventsPerBatch = 50;
});

// Configure Cosmos storage for Snapshots
builder.Services.AddCosmosSnapshotStorageProvider(options =>
{
    options.DatabaseId = "cascade-web-db";
    options.ContainerId = "snapshots";
    options.QueryBatchSize = 100;
});

// Configure Orleans silo with Aspire integration
// UseOrleans() automatically configures clustering, grain storage, and streaming
// based on the AppHost configuration (WithClustering, WithGrainStorage, WithMemoryStreaming)
builder.UseOrleans(siloBuilder =>
{
    // Enable distributed tracing context propagation across grain calls
    siloBuilder.AddActivityPropagation();

    // Configure Aqueduct to use the Aspire-configured stream provider
    siloBuilder.UseAqueduct(options => options.StreamProviderName = "StreamProvider");

    // Configure event sourcing to use the same stream provider
    // Must match the stream provider name configured in AppHost via WithMemoryStreaming
    siloBuilder.AddEventSourcing(options => options.OrleansStreamProviderName = "StreamProvider");
});
WebApplication app = builder.Build();

// Health check endpoint for Aspire orchestration
// Only returns healthy when Orleans silo is fully started and accepting requests
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
                    Service = "Cascade.Silo",
                    Orleans = status.ToString(),
                })
            : Results.StatusCode(503);
    });
await app.RunAsync();