using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.Extensions.Hosting;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Brooks.Runtime;
using Mississippi.Brooks.Runtime.Storage.Cosmos;
using Mississippi.Brooks.Serialization.Abstractions;
using Mississippi.Brooks.Serialization.Json;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime;
using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob;

using Orleans.Configuration;
using Orleans.Hosting;

using Projects;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Dedicated scenario host for the Blob snapshot trust slice.
/// </summary>
internal sealed class BlobSnapshotTrustSliceScenario : IAsyncDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);

    private readonly DistributedApplication app;

    private readonly IDistributedApplicationTestingBuilder appHost;

    private readonly List<IHost> orleansHosts = [];

    private BlobSnapshotTrustSliceScenario(
        string snapshotContainerName,
        string snapshotBlobPrefix,
        IDistributedApplicationTestingBuilder appHost,
        DistributedApplication app,
        string cosmosConnectionString,
        string blobConnectionString,
        IHost initialOrleansHost
    )
    {
        SnapshotContainerName = snapshotContainerName;
        SnapshotBlobPrefix = snapshotBlobPrefix;
        this.appHost = appHost;
        this.app = app;
        CosmosConnectionString = cosmosConnectionString;
        BlobConnectionString = blobConnectionString;
        orleansHosts.Add(initialOrleansHost);
    }

    /// <summary>
    ///     Gets the aggregate grain factory for the running Orleans host.
    /// </summary>
    public IAggregateGrainFactory AggregateGrainFactory =>
        CurrentOrleansHost.Services.GetRequiredService<IAggregateGrainFactory>();

    /// <summary>
    ///     Gets the Azurite Blob connection string.
    /// </summary>
    public string BlobConnectionString { get; }

    /// <summary>
    ///     Gets the Cosmos emulator connection string.
    /// </summary>
    public string CosmosConnectionString { get; }

    /// <summary>
    ///     Gets the snapshot blob prefix configured for the scenario.
    /// </summary>
    public string SnapshotBlobPrefix { get; }

    /// <summary>
    ///     Gets the snapshot container name configured for the scenario.
    /// </summary>
    public string SnapshotContainerName { get; }

    private IHost CurrentOrleansHost =>
        orleansHosts.Count > 0
            ? orleansHosts[^1]
            : throw new InvalidOperationException("The Orleans host is not started.");

    /// <summary>
    ///     Starts the dedicated Blob snapshot trust-slice scenario.
    /// </summary>
    /// <returns>A started scenario host.</returns>
#pragma warning disable IDISP001 // Dispose created - appHost/app ownership transfers to the returned scenario instance
    public static async Task<BlobSnapshotTrustSliceScenario> StartAsync()
    {
        string uniqueId = Guid.NewGuid().ToString("N");
        string snapshotContainerName = $"blob-snapshot-{uniqueId[..18]}";
        string snapshotBlobPrefix = $"trust-slice/{uniqueId}/";
        IDistributedApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Crescent_AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder => clientBuilder.AddStandardResilienceHandler());
        DistributedApplication startedApp = await BuildStartedApplicationAsync(appHost);
        await startedApp.StartAsync().WaitAsync(DefaultTimeout);
        using CancellationTokenSource cancellationTokenSource = new(DefaultTimeout);
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        await startedApp.ResourceNotifications.WaitForResourceHealthyAsync("cosmos", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        await startedApp.ResourceNotifications.WaitForResourceHealthyAsync("storage", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        string cosmosConnectionString = await startedApp.GetConnectionStringAsync("cosmos", cancellationToken) ??
                                        throw new InvalidOperationException(
                                            "Failed to get the Cosmos connection string.");
        string blobConnectionString = await startedApp.GetConnectionStringAsync("blobs", cancellationToken) ??
                                      throw new InvalidOperationException("Failed to get the Blob connection string.");
        IHost initialOrleansHost = await StartOrleansHostAsync(
            cosmosConnectionString,
            blobConnectionString,
            snapshotContainerName,
            snapshotBlobPrefix,
            cancellationToken);
        return new(
            snapshotContainerName,
            snapshotBlobPrefix,
            appHost,
            startedApp,
            cosmosConnectionString,
            blobConnectionString,
            initialOrleansHost);
    }
#pragma warning restore IDISP001

    private static IHost BuildOrleansHost(
        string cosmosConnectionString,
        string blobConnectionString,
        string snapshotContainerName,
        string snapshotBlobPrefix
    )
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss.fff ";
        });
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddFilter("Orleans", LogLevel.Warning);
        builder.Logging.AddFilter("Mississippi", LogLevel.Information);
        builder.Services.AddEventSourcingByService();
        builder.Services.AddJsonSerialization();
        builder.Services.AddSingleton<ISerializationProvider, CrescentBlobCustomJsonSerializationProvider>();
        builder.Services.AddSnapshotCaching();
        builder.Services.Configure<SnapshotRetentionOptions>(options => options.DefaultRetainModulus = 1);
        builder.Services.AddKeyedSingleton(
            BrookCosmosDefaults.CosmosClientServiceKey,
            (
                _,
                _
            ) => new CosmosClient(
                cosmosConnectionString,
                new()
                {
                    ConnectionMode = ConnectionMode.Gateway,
                    LimitToEndpoint = true,
                }));
        builder.Services.AddKeyedSingleton(
            BrookCosmosDefaults.BlobLockingServiceKey,
            (
                _,
                _
            ) => new BlobServiceClient(blobConnectionString));
        builder.Services.AddCosmosBrookStorageProvider(options =>
        {
            options.CosmosClientServiceKey = BrookCosmosDefaults.CosmosClientServiceKey;
            options.DatabaseId = "aspire-l2tests";
            options.QueryBatchSize = 50;
            options.MaxEventsPerBatch = 50;
        });
        builder.Services.AddBlobSnapshotStorageProvider(
            blobConnectionString,
            options =>
            {
                options.ContainerName = snapshotContainerName;
                options.BlobPrefix = snapshotBlobPrefix;
                options.Compression = SnapshotBlobCompression.Gzip;
                options.PayloadSerializerFormat = CrescentBlobCustomJsonSerializationProvider.SerializerFormat;
                options.ContainerInitializationMode = SnapshotBlobContainerInitializationMode.CreateIfMissing;
            });
        builder.Services.AddLargeSnapshotScenarioAggregate();
        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "aspire-l2tests";
                    options.ServiceId = "CrescentBlobSnapshotTrustSlice";
                });
            silo.AddMemoryStreams(BrookStreamingDefaults.OrleansStreamProviderName);
            silo.AddMemoryGrainStorage("PubSubStore");
            silo.AddEventSourcing();
        });
        return builder.Build();
    }

    private static Task<DistributedApplication> BuildStartedApplicationAsync(
        IDistributedApplicationTestingBuilder appHost
    ) =>
        appHost.BuildAsync().WaitAsync(DefaultTimeout);

    private static string NormalizeBlobPrefix(
        string? blobPrefix
    )
    {
        if (string.IsNullOrWhiteSpace(blobPrefix))
        {
            return string.Empty;
        }

        string normalizedPrefix = blobPrefix.Trim().Replace('\\', '/').Trim('/');
        return string.IsNullOrEmpty(normalizedPrefix) ? string.Empty : string.Concat(normalizedPrefix, "/");
    }

    private static async Task<IHost> StartOrleansHostAsync(
        string cosmosConnectionString,
        string blobConnectionString,
        string snapshotContainerName,
        string snapshotBlobPrefix,
        CancellationToken cancellationToken
    )
    {
        IHost host = BuildOrleansHost(
            cosmosConnectionString,
            blobConnectionString,
            snapshotContainerName,
            snapshotBlobPrefix);
        await host.StartAsync(cancellationToken);
        return host;
    }

    /// <summary>
    ///     Creates a Blob service client connected to the scenario Azurite instance.
    /// </summary>
    /// <returns>The Blob service client.</returns>
    public BlobServiceClient CreateBlobServiceClient() => new(BlobConnectionString);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopOrleansAsync();
        await app.StopAsync();
        await app.DisposeAsync();
        if (appHost is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
            return;
        }

        if (appHost is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    ///     Persists the supplied large-snapshot state through the registered snapshot storage writer and returns the exact
    ///     snapshot Blob for that version.
    /// </summary>
    /// <param name="entityId">The aggregate entity identifier.</param>
    /// <param name="snapshotState">The snapshot state to persist.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The exact Blob client for the persisted snapshot version.</returns>
    public async Task<BlobClient> PersistSnapshotAsync(
        string entityId,
        LargeSnapshotAggregate snapshotState,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentNullException.ThrowIfNull(snapshotState);
        using IServiceScope scope = CurrentOrleansHost.Services.CreateScope();
        IServiceProvider services = scope.ServiceProvider;
        IBrookGrainFactory brookGrainFactory = services.GetRequiredService<IBrookGrainFactory>();
        IRootReducer<LargeSnapshotAggregate> rootReducer =
            services.GetRequiredService<IRootReducer<LargeSnapshotAggregate>>();
        ISnapshotStateConverter<LargeSnapshotAggregate> snapshotStateConverter =
            services.GetRequiredService<ISnapshotStateConverter<LargeSnapshotAggregate>>();
        ISnapshotStorageWriter snapshotStorageWriter = services.GetRequiredService<ISnapshotStorageWriter>();
        BrookPosition snapshotVersion = await brookGrainFactory
            .GetBrookCursorGrain(BrookKey.ForType<LargeSnapshotAggregate>(entityId))
            .GetLatestPositionAsync()
            .ConfigureAwait(false);
        if (snapshotVersion.NotSet)
        {
            throw new InvalidOperationException($"No events have been committed for aggregate '{entityId}'.");
        }

        string reducerHash = rootReducer.GetReducerHash();
        SnapshotStreamKey snapshotStreamKey = new(
            BrookNameHelper.GetBrookName<LargeSnapshotAggregate>(),
            SnapshotStorageNameHelper.GetStorageName<LargeSnapshotAggregate>(),
            entityId,
            reducerHash);
        SnapshotKey snapshotKey = new(snapshotStreamKey, snapshotVersion.Value);
        BlobClient blobClient = CreateBlobServiceClient()
            .GetBlobContainerClient(SnapshotContainerName)
            .GetBlobClient(GetBlobName(snapshotKey));
        if (await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return blobClient;
        }

        SnapshotEnvelope snapshotEnvelope = snapshotStateConverter.ToEnvelope(snapshotState, reducerHash);
        try
        {
            await snapshotStorageWriter.WriteAsync(snapshotKey, snapshotEnvelope, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            if (!await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                throw;
            }

            // The background persister may have already committed the exact blob version; treat that as ready.
        }

        return blobClient;
    }

    /// <summary>
    ///     Restarts only the Orleans host while keeping the emulators running.
    /// </summary>
    /// <returns>A task representing the restart operation.</returns>
    public async Task RestartOrleansAsync()
    {
        await StopOrleansAsync();
        IHost restartedHost = await StartOrleansHostAsync(
            CosmosConnectionString,
            BlobConnectionString,
            SnapshotContainerName,
            SnapshotBlobPrefix,
            CancellationToken.None);
        orleansHosts.Add(restartedHost);
    }

    private string GetBlobName(
        SnapshotKey snapshotKey
    ) =>
        $"{GetStreamPrefix(snapshotKey.Stream)}v{snapshotKey.Version:D20}.snapshot";

    private string GetStreamPrefix(
        SnapshotStreamKey snapshotStreamKey
    )
    {
        ArrayBufferWriter<byte> buffer = new();
        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("brookName", snapshotStreamKey.BrookName);
            writer.WriteString("snapshotStorageName", snapshotStreamKey.SnapshotStorageName);
            writer.WriteString("entityId", snapshotStreamKey.EntityId);
            writer.WriteString("reducersHash", snapshotStreamKey.ReducersHash);
            writer.WriteEndObject();
        }

        string streamHash = Convert.ToHexString(SHA256.HashData(buffer.WrittenSpan));
        return string.Concat(NormalizeBlobPrefix(SnapshotBlobPrefix), streamHash, "/");
    }

    private async Task StopOrleansAsync()
    {
        if (orleansHosts.Count == 0)
        {
            return;
        }

        IHost currentHost = orleansHosts[^1];
        orleansHosts.RemoveAt(orleansHosts.Count - 1);
        await currentHost.StopAsync();
        currentHost.Dispose();
    }
}