using Cascade.Domain;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;

using Mississippi.Aqueduct.Grains;
using Mississippi.EventSourcing.Brooks;
using Mississippi.EventSourcing.Brooks.Cosmos;
using Mississippi.EventSourcing.Serialization.Json;
using Mississippi.EventSourcing.Snapshots;
using Mississippi.EventSourcing.Snapshots.Cosmos;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
builder.AddAzureBlobServiceClient("blobs");

// Add Cascade domain services (aggregates, handlers, reducers, projections)
builder.Services.AddCascadeDomain();

// Add event sourcing infrastructure
builder.Services.AddJsonSerialization();
builder.Services.AddEventSourcingByService();
builder.Services.AddSnapshotCaching();

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
    // Configure Aqueduct to use the Aspire-configured stream provider
    siloBuilder.UseAqueduct(options => options.StreamProviderName = "StreamProvider");

    // Configure event sourcing to use the same stream provider
    siloBuilder.AddEventSourcing();
});
WebApplication app = builder.Build();

// Health check endpoint for Aspire orchestration
app.MapGet(
    "/health",
    () => Results.Ok(
        new
        {
            Status = "Healthy",
            Service = "Cascade.Web.Silo",
        }));
await app.RunAsync();