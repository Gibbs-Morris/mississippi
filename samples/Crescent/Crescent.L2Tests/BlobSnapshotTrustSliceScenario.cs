using Microsoft.Extensions.Hosting;

using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Brooks.Runtime;
using Mississippi.Brooks.Runtime.Storage.Cosmos;
using Mississippi.Brooks.Serialization.Abstractions;
using Mississippi.Brooks.Serialization.Json;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime;
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
    private readonly IDistributedApplicationTestingBuilder appHost;

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);

    private readonly DistributedApplication app;

    private readonly List<IHost> orleansHosts = [];

    private readonly string snapshotBlobPrefix;

    private readonly string snapshotContainerName;

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
        this.snapshotContainerName = snapshotContainerName;
        this.snapshotBlobPrefix = snapshotBlobPrefix;
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
    public string BlobConnectionString { get; private set; }

    /// <summary>
    ///     Gets the Cosmos emulator connection string.
    /// </summary>
    public string CosmosConnectionString { get; private set; }

    /// <summary>
    ///     Gets the snapshot blob prefix configured for the scenario.
    /// </summary>
    public string SnapshotBlobPrefix => snapshotBlobPrefix;

    /// <summary>
    ///     Gets the snapshot container name configured for the scenario.
    /// </summary>
    public string SnapshotContainerName => snapshotContainerName;

    /// <summary>
    ///     Starts the dedicated Blob snapshot trust-slice scenario.
    /// </summary>
    /// <returns>A started scenario host.</returns>
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
                                        throw new InvalidOperationException("Failed to get the Cosmos connection string.");
        string blobConnectionString = await startedApp.GetConnectionStringAsync("blobs", cancellationToken) ??
                                      throw new InvalidOperationException("Failed to get the Blob connection string.");

        return new BlobSnapshotTrustSliceScenario(
            snapshotContainerName,
            snapshotBlobPrefix,
            appHost,
            startedApp,
            cosmosConnectionString,
            blobConnectionString,
            await StartOrleansHostAsync(cosmosConnectionString, blobConnectionString, snapshotContainerName, snapshotBlobPrefix, cancellationToken));
    }

    /// <summary>
    ///     Creates a Blob service client connected to the scenario Azurite instance.
    /// </summary>
    /// <returns>The Blob service client.</returns>
    public BlobServiceClient CreateBlobServiceClient() => new(BlobConnectionString);

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
            snapshotContainerName,
            snapshotBlobPrefix,
            CancellationToken.None);
        orleansHosts.Add(restartedHost);
    }

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

    private IHost CurrentOrleansHost =>
        orleansHosts.Count > 0
            ? orleansHosts[^1]
            : throw new InvalidOperationException("The Orleans host is not started.");

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
                new CosmosClientOptions
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