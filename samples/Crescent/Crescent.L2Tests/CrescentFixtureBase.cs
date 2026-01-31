using Azure.Storage.Blobs;

using Crescent.Crescent.L2Tests.Domain.Counter;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Mississippi.Common.Abstractions;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks;
using Mississippi.EventSourcing.Brooks.Cosmos;
using Mississippi.EventSourcing.Serialization.Json;
using Mississippi.EventSourcing.Snapshots;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

using Projects;


namespace Crescent.Crescent.L2Tests;

/// <summary>
///     Base fixture for Crescent integration tests using Aspire and Orleans.
/// </summary>
public abstract class CrescentFixtureBase : IAsyncLifetime, IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);

    private DistributedApplication? app;

    private bool disposed;

    private IHost? orleansHost;

    /// <summary>
    ///     Gets the aggregate grain factory for resolving aggregate grains.
    /// </summary>
    public IAggregateGrainFactory AggregateGrainFactory =>
        orleansHost?.Services.GetRequiredService<IAggregateGrainFactory>() ??
        throw new InvalidOperationException("Orleans host not initialized.");

    /// <summary>
    ///     Gets the Blob storage connection string for tests.
    /// </summary>
    public string BlobConnectionString { get; private set; } = string.Empty;

    /// <summary>
    ///     Gets the Orleans cluster client for resolving grains.
    /// </summary>
    public IClusterClient ClusterClient =>
        orleansHost?.Services.GetRequiredService<IClusterClient>() ??
        throw new InvalidOperationException("Orleans host not initialized.");

    /// <summary>
    ///     Gets the Cosmos DB connection string for tests.
    /// </summary>
    public string CosmosConnectionString { get; private set; } = string.Empty;

    /// <summary>
    ///     Gets the initialization error if the fixture failed to start.
    /// </summary>
    public Exception? InitializationError { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the fixture initialized successfully.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    ///     Gets the UX projection grain factory for resolving projection grains.
    /// </summary>
    public IUxProjectionGrainFactory UxProjectionGrainFactory =>
        orleansHost?.Services.GetRequiredService<IUxProjectionGrainFactory>() ??
        throw new InvalidOperationException("Orleans host not initialized.");

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (orleansHost is not null)
        {
            await orleansHost.StopAsync();
            orleansHost.Dispose();
        }

        if (app is not null)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            IDistributedApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilder
                .CreateAsync<Crescent_AppHost>();

            appHost.Services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
                logging.AddFilter("Aspire.", LogLevel.Debug);
            });

            appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            {
                clientBuilder.AddStandardResilienceHandler();
            });

            DistributedApplication builtApp = await appHost.BuildAsync().WaitAsync(DefaultTimeout);
            app = builtApp;
            await app.StartAsync().WaitAsync(DefaultTimeout);

            using CancellationTokenSource cts = new(DefaultTimeout);

            await app.ResourceNotifications.WaitForResourceHealthyAsync("cosmos", cts.Token)
                .WaitAsync(DefaultTimeout, cts.Token);

            await app.ResourceNotifications.WaitForResourceHealthyAsync("storage", cts.Token)
                .WaitAsync(DefaultTimeout, cts.Token);

            CosmosConnectionString = await app.GetConnectionStringAsync("cosmos", cts.Token) ??
                                     throw new InvalidOperationException("Failed to get Cosmos DB connection string.");
            BlobConnectionString = await app.GetConnectionStringAsync("blobs", cts.Token) ??
                                   throw new InvalidOperationException("Failed to get Blob storage connection string.");

            Console.WriteLine("=== ASPIRE FIXTURE DEBUG ===");
            Console.WriteLine($"[Fixture] Cosmos connection string: {MaskConnectionString(CosmosConnectionString)}");
            Console.WriteLine($"[Fixture] Blob connection string: {MaskConnectionString(BlobConnectionString)}");
            Console.WriteLine("=== END FIXTURE DEBUG ===");

            Console.WriteLine("[Fixture] Starting Orleans silo...");
            orleansHost = BuildOrleansHost(CosmosConnectionString, BlobConnectionString);
            await orleansHost.StartAsync(cts.Token);
            Console.WriteLine("[Fixture] Orleans silo started successfully.");
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            InitializationError = ex;
            IsInitialized = false;
            throw;
        }
    }

    /// <summary>
    ///     Creates a new BlobServiceClient configured to connect to the Azurite emulator.
    /// </summary>
    public BlobServiceClient CreateBlobServiceClient()
    {
        EnsureInitialized();
        return new(BlobConnectionString);
    }

    /// <summary>
    ///     Creates a new CosmosClient configured to connect to the emulator.
    /// </summary>
    public CosmosClient CreateCosmosClient()
    {
        EnsureInitialized();
        CosmosClientOptions options = new()
        {
            ConnectionMode = ConnectionMode.Gateway,
            LimitToEndpoint = true,
        };

        if (!CosmosConnectionString.Contains("http://", StringComparison.OrdinalIgnoreCase))
        {
            options.HttpClientFactory = () =>
            {
                HttpClientHandler handler = new()
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                };
                return new(handler);
            };
        }

        return new(CosmosConnectionString, options);
    }

    /// <summary>
    ///     Waits for a UX projection to reach a desired state.
    /// </summary>
    /// <typeparam name="TProjection">The projection type to read.</typeparam>
    /// <param name="entityId">The entity identifier used by the projection grain.</param>
    /// <param name="predicate">The predicate that determines when the projection is ready.</param>
    /// <param name="timeout">The maximum time to wait before failing.</param>
    /// <param name="pollInterval">The polling interval between reads.</param>
    /// <param name="cancellationToken">A cancellation token to abort the wait early.</param>
    /// <returns>The projection state that satisfies the predicate.</returns>
    /// <exception cref="TimeoutException">Thrown when the projection does not reach the expected state in time.</exception>
    public async Task<TProjection> WaitForProjectionAsync<TProjection>(
        string entityId,
        Func<TProjection, bool> predicate,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default
    )
        where TProjection : class
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        ArgumentNullException.ThrowIfNull(predicate);

        TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);
        TimeSpan effectivePollInterval = pollInterval ?? TimeSpan.FromMilliseconds(200);

        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(effectiveTimeout);

        IUxProjectionGrain<TProjection> projectionGrain = UxProjectionGrainFactory
            .GetUxProjectionGrain<TProjection>(entityId);

        try
        {
            using PeriodicTimer timer = new(effectivePollInterval);
            while (true)
            {
                TProjection? projection = await projectionGrain.GetAsync(cts.Token);
                if (projection is not null && predicate(projection))
                {
                    return projection;
                }

                await timer.WaitForNextTickAsync(cts.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Projection for '{entityId}' did not reach the expected state within {effectiveTimeout}.");
        }
    }

    /// <summary>
    ///     Adds the specific snapshot storage provider services.
    /// </summary>
    protected abstract void AddSnapshotStorage(IHostApplicationBuilder builder);

    private IHost BuildOrleansHost(string cosmosConnectionString, string blobConnectionString)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss.fff ";
        });
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        builder.Logging.AddFilter("Orleans", LogLevel.Warning);
        builder.Logging.AddFilter("Mississippi", LogLevel.Debug);

        builder.Services.AddEventSourcingByService();
        builder.Services.AddJsonSerialization();
        builder.Services.AddSnapshotCaching();

        // Register keyed clients
        builder.Services.AddKeyedSingleton(
            MississippiDefaults.ServiceKeys.CosmosBrooksClient,
            (_, _) => new CosmosClient(cosmosConnectionString, new() { ConnectionMode = ConnectionMode.Gateway, LimitToEndpoint = true }));

        builder.Services.AddKeyedSingleton(
            MississippiDefaults.ServiceKeys.CosmosSnapshotsClient,
            (_, _) => new CosmosClient(cosmosConnectionString, new() { ConnectionMode = ConnectionMode.Gateway, LimitToEndpoint = true }));

        builder.Services.AddKeyedSingleton(
            MississippiDefaults.ServiceKeys.BlobLocking,
            (_, _) => new BlobServiceClient(blobConnectionString));

        builder.Services.AddKeyedSingleton(
            MississippiDefaults.ServiceKeys.BlobSnapshotsClient,
            (_, _) => new BlobServiceClient(blobConnectionString));

        // Configure Brooks (Cosmos) - common for both fixtures currently unless we want to vary brooks too
        builder.Services.AddCosmosBrookStorageProvider(o =>
        {
            o.CosmosClientServiceKey = MississippiDefaults.ServiceKeys.CosmosBrooksClient;
            o.DatabaseId = "aspire-l2tests";
            o.QueryBatchSize = 50;
            o.MaxEventsPerBatch = 50;
        });

        // Add Snapshot Storage (Abstract)
        AddSnapshotStorage(builder);

        // Register Domain
        builder.Services.AddCounterAggregate();

        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering()
                .Configure<ClusterOptions>(opt =>
                {
                    opt.ClusterId = "aspire-l2tests";
                    opt.ServiceId = "CrescentTests";
                });

            silo.AddMemoryStreams(MississippiDefaults.StreamProviderName);
            silo.AddMemoryGrainStorage("PubSubStore");
            silo.AddEventSourcing();
        });

        return builder.Build();
    }

    private void EnsureInitialized()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("Aspire fixture is not initialized.");
        }
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return "(empty)";
        }

        return connectionString.Length > 50 ? $"{connectionString[..50]}..." : connectionString;
    }
}
