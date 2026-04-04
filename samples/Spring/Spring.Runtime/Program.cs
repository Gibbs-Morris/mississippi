using System.Linq;
using System.Threading;

using Azure.Data.Tables;
using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Runtime;
using Mississippi.Brooks.Runtime;
using Mississippi.Brooks.Runtime.Storage.Cosmos;
using Mississippi.Brooks.Serialization.Json;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Inlet.Runtime;
using Mississippi.Tributary.Runtime;
using Mississippi.Tributary.Runtime.Storage.Cosmos;

using MississippiSamples.Spring.Domain.Projections.BankAccountBalance;
using MississippiSamples.Spring.Domain.Services;
using MississippiSamples.Spring.Runtime.Registrations;
using MississippiSamples.Spring.Runtime.Services;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Orleans.Hosting;
using Orleans.Runtime;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Register HttpClient factory for effects that call external APIs
builder.Services.AddHttpClient();

// Register notification service (stub for demo, replace with real provider in production)
builder.Services.AddSingleton<INotificationService, StubNotificationService>();
builder.Services.AddSingleton<ISagaAccessContextProvider, SpringSagaAccessContextProvider>();
builder.Services.AddSingleton<IValidateOptions<SagaRecoveryOptions>, SpringSagaReminderOptionsValidator>();

// Register Spring domain aggregates
builder.Services.AddAuthProofAggregate();
builder.Services.AddBankAccountAggregate();
builder.Services.AddTransactionInvestigationQueueAggregate();
builder.Services.AddAuthProofProjection();
builder.Services.AddBankAccountBalanceProjection();
builder.Services.AddBankAccountLedgerProjection();
builder.Services.AddFlaggedTransactionsProjection();
builder.Services.AddMoneyTransferStatusProjection();
builder.Services.AddAuthProofSaga();
builder.Services.AddMoneyTransferSaga();
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Microsoft.Orleans.Runtime")
        .AddSource("Microsoft.Orleans.Application"))
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()

        // Mississippi framework meters
        .AddMeter("Mississippi.Brooks.Runtime")
        .AddMeter("Mississippi.DomainModeling.Runtime")
        .AddMeter("Mississippi.Tributary.Runtime")
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
builder.Services.AddSingleton(sp => sp.GetRequiredKeyedService<TableServiceClient>("clustering"));
builder.Services.AddOptions<SagaRecoveryOptions>()
    .Bind(builder.Configuration.GetSection("SagaRecovery"))
    .ValidateOnStart();
builder.Services.AddHealthChecks().AddCheck<SpringSagaReminderHealthCheck>("spring-saga-reminders");

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
    BrookCosmosDefaults.BlobLockingServiceKey,
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

// Add Inlet Silo services for projection subscription management
builder.Services.AddInletSilo();
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
    siloBuilder.UseAzureTableReminderService(optionsBuilder =>
    {
        optionsBuilder.Configure<TableServiceClient>((
            options,
            tableServiceClient
        ) => options.TableServiceClient = tableServiceClient);
    });

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
    async (
        ISiloStatusOracle siloStatus,
        HealthCheckService healthCheckService,
        CancellationToken cancellationToken
    ) =>
    {
        SiloStatus status = siloStatus.CurrentStatus;
        HealthReport report = await healthCheckService.CheckHealthAsync(cancellationToken);
        bool healthy = (status == SiloStatus.Active) && (report.Status != HealthStatus.Unhealthy);
        object payload = new
        {
            Status = healthy ? "Healthy" : "Unhealthy",
            Service = "MississippiSamples.Spring.Runtime",
            Orleans = status.ToString(),
            Checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    Status = entry.Value.Status.ToString(),
                    entry.Value.Description,
                }),
        };
        return healthy
            ? Results.Ok(payload)
            : Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
    });
await app.RunAsync();