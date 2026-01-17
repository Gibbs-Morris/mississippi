// <copyright file="CrescentFixture.cs" company="Gibbs-Morris LLC">
// Licensed under the Gibbs-Morris commercial license.
// </copyright>

using Crescent.Crescent.L2Tests.Domain.Counter;

using Microsoft.Extensions.Hosting;

using Mississippi.Common.Abstractions;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks;
using Mississippi.EventSourcing.Brooks.Cosmos;
using Mississippi.EventSourcing.Serialization.Json;
using Mississippi.EventSourcing.Snapshots;
using Mississippi.EventSourcing.Snapshots.Cosmos;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

using Projects;


namespace Crescent.Crescent.L2Tests;

/// <summary>
///     xUnit fixture that starts the Crescent AppHost with Cosmos DB and Azure Storage emulators.
///     This fixture is shared across all tests in the collection to minimize container startup overhead.
/// </summary>
/// <remarks>
///     <para>
///         The Cosmos DB emulator can take 2-5 minutes for a cold start depending on partition count.
///         We use a 10-minute timeout to accommodate worst-case scenarios.
///     </para>
///     <para>
///         The Azurite storage emulator starts much faster (typically under 30 seconds).
///     </para>
/// </remarks>
#pragma warning disable CA1515 // Types can be made internal - xUnit fixture must be public
#pragma warning disable IDISP002 // Dispose member - disposed in DisposeAsync
#pragma warning disable IDISP003 // Dispose previous before re-assigning - fields are null initially
public sealed class CrescentFixture
    : IAsyncLifetime,
      IDisposable
#pragma warning restore CA1515
{
    /// <summary>
    ///     Generous timeout to accommodate Cosmos DB emulator cold start.
    /// </summary>
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

    private static IHost BuildOrleansHost(
        string cosmosConnectionString,
        string blobConnectionString
    )
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss.fff ";
        });
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        builder.Logging.AddFilter("Orleans", LogLevel.Warning);
        builder.Logging.AddFilter("Mississippi", LogLevel.Debug);

        // Add Mississippi event sourcing services
        builder.Services.AddEventSourcingByService();

        // Add JSON serialization for event sourcing
        builder.Services.AddJsonSerialization();

        // Add snapshot caching infrastructure (required for aggregate grains)
        builder.Services.AddSnapshotCaching();

        // Configure Orleans silo
        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering()
                .Configure<ClusterOptions>(opt =>
                {
                    opt.ClusterId = "aspire-l2tests";
                    opt.ServiceId = "CrescentTests";
                });

            // Host configures stream infrastructure
            silo.AddMemoryStreams(MississippiDefaults.StreamProviderName);
            silo.AddMemoryGrainStorage("PubSubStore");

            // Tell Brooks which stream provider to use
            silo.AddEventSourcing();
        });

        // Pre-register CosmosClient with Gateway mode for Aspire emulator compatibility
        // See: https://github.com/dotnet/aspire/issues/5364
        builder.Services.AddSingleton<CosmosClient>(_ => new(
            cosmosConnectionString,
            new()
            {
                ConnectionMode = ConnectionMode.Gateway,
                LimitToEndpoint = true,
            }));

        // Pre-register BlobServiceClient as keyed service for distributed locking
        // BlobDistributedLockManager uses [FromKeyedServices(MississippiDefaults.ServiceKeys.BlobLocking)]
        builder.Services.AddKeyedSingleton(
            MississippiDefaults.ServiceKeys.BlobLocking,
            (
                _,
                _
            ) => new BlobServiceClient(blobConnectionString));

        // Configure Cosmos DB storage for brooks (event streams)
        // Use the overload without connection strings since we pre-registered the clients
        builder.Services.AddCosmosBrookStorageProvider(o =>
        {
            o.DatabaseId = "aspire-l2tests";
            o.QueryBatchSize = 50;
            o.MaxEventsPerBatch = 50;
        });

        // Configure Cosmos DB storage for snapshots
        builder.Services.AddCosmosSnapshotStorageProvider(options =>
        {
            options.DatabaseId = "aspire-l2tests";
            options.ContainerId = "snapshots";
            options.QueryBatchSize = 100;
        });

        // Register Counter aggregate domain (events, handlers, reducers, projections)
        builder.Services.AddCounterAggregate();
        IHost host = builder.Build();

        // Pre-warm: ensure the database and containers exist
        // This is done by the storage providers on first access, but we can trigger it early
        Console.WriteLine("[Fixture] Orleans host built, database will be created on first access.");
        return host;
    }

    private static string MaskConnectionString(
        string connectionString
    )
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return "(empty)";
        }

        // Show first 50 chars and mask the rest
        return connectionString.Length > 50
            ? $"{connectionString[..50]}...(length={connectionString.Length})"
            : connectionString;
    }

    /// <summary>
    ///     Creates a new BlobServiceClient configured to connect to the Azurite emulator.
    /// </summary>
    /// <returns>A configured BlobServiceClient instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the fixture is not initialized.</exception>
    public BlobServiceClient CreateBlobServiceClient()
    {
        EnsureInitialized();
        return new(BlobConnectionString);
    }

    /// <summary>
    ///     Creates a new CosmosClient configured to connect to the emulator.
    /// </summary>
    /// <returns>A configured CosmosClient instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the fixture is not initialized.</exception>
    /// <remarks>
    ///     The preview emulator (Linux-based) uses HTTP, so no certificate bypass is needed.
    ///     We still use Gateway mode for compatibility.
    /// </remarks>
    public CosmosClient CreateCosmosClient()
    {
        EnsureInitialized();
        Console.WriteLine("=== CREATE COSMOS CLIENT DEBUG ===");
        Console.WriteLine($"[CreateCosmosClient] Creating client with connection string: {CosmosConnectionString}");

        // Detect if we're using the preview emulator (HTTP) or regular emulator (HTTPS)
        bool isHttpEndpoint = CosmosConnectionString.Contains("http://", StringComparison.OrdinalIgnoreCase);
        Console.WriteLine($"[CreateCosmosClient] Is HTTP endpoint: {isHttpEndpoint}");
        CosmosClientOptions options = new()
        {
            ConnectionMode = ConnectionMode.Gateway, // Emulator works better with Gateway mode
            LimitToEndpoint = true, // Required for emulator - prevents SDK from trying to discover replicas
        };

        // Only add certificate bypass for HTTPS endpoints (non-preview emulator)
        if (!isHttpEndpoint)
        {
            Console.WriteLine("[CreateCosmosClient] Using HTTPS - adding certificate bypass for self-signed cert");
#pragma warning disable CA5400 // HttpClient certificate check - emulator uses self-signed cert
#pragma warning disable IDISP014, IDISP001 // Use a single instance of HttpClient - CosmosClient manages its own lifecycle
            options.HttpClientFactory = () =>
            {
                Console.WriteLine("[CreateCosmosClient] HttpClientFactory invoked - creating handler with cert bypass");
                HttpClientHandler handler = new()
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                };
                return new(handler);
            };
#pragma warning restore IDISP014, IDISP001
#pragma warning restore CA5400
        }
        else
        {
            Console.WriteLine("[CreateCosmosClient] Using HTTP (preview emulator) - no certificate bypass needed");
        }

        CosmosClient client = new(CosmosConnectionString, options);
        Console.WriteLine($"[CreateCosmosClient] CosmosClient created successfully, endpoint: {client.Endpoint}");
        Console.WriteLine("=== END CREATE COSMOS CLIENT DEBUG ===");
        return client;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
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
#pragma warning disable IDISP001 // Dispose created - appHost implements builder pattern; BuildAsync returns app that we dispose
    public async Task InitializeAsync()
    {
        try
        {
            // Start Crescent AppHost
            IDistributedApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilder
                .CreateAsync<Crescent_AppHost>();
#pragma warning restore IDISP001

            // Configure logging - use Debug level to see resource health status
            appHost.Services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
                logging.AddFilter("Aspire.", LogLevel.Debug);
            });

            // Add HTTP resilience for any HTTP-based operations
            appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            {
                clientBuilder.AddStandardResilienceHandler();
            });

            // Build and start the application (following official docs pattern)
            DistributedApplication builtApp = await appHost.BuildAsync().WaitAsync(DefaultTimeout);
            app = builtApp;
            await app.StartAsync().WaitAsync(DefaultTimeout);

            // Wait for container resources to be healthy (following official docs pattern)
            using CancellationTokenSource cts = new(DefaultTimeout);

            // Wait for Cosmos DB emulator (this is the slow one)
            await app.ResourceNotifications.WaitForResourceHealthyAsync("cosmos", cts.Token)
                .WaitAsync(DefaultTimeout, cts.Token);

            // Wait for Azure Storage emulator (Azurite)
            await app.ResourceNotifications.WaitForResourceHealthyAsync("storage", cts.Token)
                .WaitAsync(DefaultTimeout, cts.Token);

            // Get connection strings for tests
            CosmosConnectionString = await app.GetConnectionStringAsync("cosmos", cts.Token) ??
                                     throw new InvalidOperationException("Failed to get Cosmos DB connection string.");
            BlobConnectionString = await app.GetConnectionStringAsync("blobs", cts.Token) ??
                                   throw new InvalidOperationException("Failed to get Blob storage connection string.");

            // Debug: Log connection strings (masked for security)
            Console.WriteLine("=== ASPIRE FIXTURE DEBUG ===");
            Console.WriteLine(
                $"[Fixture] Cosmos connection string obtained: {MaskConnectionString(CosmosConnectionString)}");
            Console.WriteLine(
                $"[Fixture] Blob connection string obtained: {MaskConnectionString(BlobConnectionString)}");
            Console.WriteLine($"[Fixture] Full Cosmos connection string: {CosmosConnectionString}");
            Console.WriteLine("=== END FIXTURE DEBUG ===");

            // Start Orleans silo with Mississippi event sourcing configured to use the Crescent emulators
            Console.WriteLine("[Fixture] Starting Orleans silo with Mississippi...");
            orleansHost = BuildOrleansHost(CosmosConnectionString, BlobConnectionString);
            await orleansHost.StartAsync(cts.Token);
            Console.WriteLine("[Fixture] Orleans silo started successfully.");
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            InitializationError = ex;
            IsInitialized = false;

            // Re-throw to fail the test fixture, but keep the error captured for diagnostics
            throw;
        }
    }

    private void EnsureInitialized()
    {
        Console.WriteLine(
            $"[EnsureInitialized] IsInitialized={IsInitialized}, HasError={InitializationError is not null}");
        if (!IsInitialized)
        {
            throw new InvalidOperationException(
                "Aspire fixture is not initialized. " +
                (InitializationError is not null
                    ? $"Initialization failed: {InitializationError.Message}"
                    : "Call InitializeAsync() first."));
        }
    }
}