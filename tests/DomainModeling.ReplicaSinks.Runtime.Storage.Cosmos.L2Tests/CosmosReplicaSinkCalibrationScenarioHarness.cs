namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L2Tests;

/// <summary>
///     Owns the per-scenario DI container and Cosmos database created for one calibration run.
/// </summary>
internal sealed class CosmosReplicaSinkCalibrationScenarioHarness : IAsyncDisposable
{
    private readonly string databaseId;

    private readonly IHostedService[] hostedServices;

    private readonly CosmosClient sharedCosmosClient;

    private readonly ServiceProvider serviceProvider;

    private CosmosReplicaSinkCalibrationScenarioHarness(
        string databaseId,
        CosmosClient sharedCosmosClient,
        ServiceProvider serviceProvider,
        IHostedService[] hostedServices,
        IReadOnlyList<CosmosReplicaSinkCalibrationSinkRegistration> registrations,
        IReadOnlyDictionary<string, IReplicaSinkProvider> providers,
        IReplicaSinkDeliveryStateStore stateStore
    )
    {
        this.databaseId = databaseId;
        this.sharedCosmosClient = sharedCosmosClient;
        this.serviceProvider = serviceProvider;
        this.hostedServices = hostedServices;
        Registrations = registrations;
        Providers = providers;
        StateStore = stateStore;
    }

    /// <summary>
    ///     Gets the keyed provider instances participating in the run.
    /// </summary>
    public IReadOnlyDictionary<string, IReplicaSinkProvider> Providers { get; }

    /// <summary>
    ///     Gets the sink registrations participating in the run.
    /// </summary>
    public IReadOnlyList<CosmosReplicaSinkCalibrationSinkRegistration> Registrations { get; }

    /// <summary>
    ///     Gets the aggregate durable state store used by the calibration run.
    /// </summary>
    public IReplicaSinkDeliveryStateStore StateStore { get; }

    /// <summary>
    ///     Creates and starts the per-scenario DI container and storage resources.
    /// </summary>
    /// <param name="sharedCosmosClient">The shared emulator-backed Cosmos client.</param>
    /// <param name="scenario">The deterministic scenario to configure.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The started scenario harness.</returns>
    public static async Task<CosmosReplicaSinkCalibrationScenarioHarness> CreateAsync(
        CosmosClient sharedCosmosClient,
        CosmosReplicaSinkCalibrationScenario scenario,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(sharedCosmosClient);
        ArgumentNullException.ThrowIfNull(scenario);

        string databaseId = $"replica-sink-calibration-{Guid.NewGuid():N}";
        List<CosmosReplicaSinkCalibrationSinkRegistration> registrations = [];
        ServiceCollection services = [];
        services.AddLogging(static logging => logging.ClearProviders());
        services.AddKeyedSingleton<CosmosClient>(
            CosmosReplicaSinkCalibrationDefaults.ClientKey,
            (
                _,
                _
            ) => sharedCosmosClient);

        for (int sinkIndex = 0; sinkIndex < scenario.SinkCount; sinkIndex++)
        {
            string sinkKey = $"sink-{sinkIndex + 1:D2}";
            string containerId = $"{scenario.Name}-{sinkIndex + 1:D2}";
            ReplicaTargetDescriptor target = new(
                new(
                    CosmosReplicaSinkCalibrationDefaults.ClientKey,
                    CosmosReplicaSinkCalibrationDefaults.TargetName),
                ReplicaProvisioningMode.CreateIfMissing);
            services.AddCosmosReplicaSink(
                sinkKey,
                CosmosReplicaSinkCalibrationDefaults.ClientKey,
                options =>
                {
                    options.DatabaseId = databaseId;
                    options.ContainerId = containerId;
                    options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing;
                    options.QueryBatchSize = 50;
                });
            registrations.Add(new(sinkKey, containerId, target));
        }

#pragma warning disable IDISP001 // Ownership of the built provider transfers to the returned harness and failure path disposes it
        ServiceProvider builtProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
#pragma warning restore IDISP001

        try
        {
            IHostedService[] hostedServices = builtProvider.GetServices<IHostedService>().ToArray();
            foreach (IHostedService hostedService in hostedServices)
            {
                await hostedService.StartAsync(cancellationToken);
            }

            IReplicaSinkDeliveryStateStore stateStore = builtProvider.GetRequiredService<IReplicaSinkDeliveryStateStore>();
            IReadOnlyDictionary<string, IReplicaSinkProvider> providers = registrations.ToDictionary(
                static registration => registration.SinkKey,
                registration => builtProvider.GetRequiredKeyedService<IReplicaSinkProvider>(registration.SinkKey),
                StringComparer.Ordinal);

            return new(
                databaseId,
                sharedCosmosClient,
                builtProvider,
                hostedServices,
                registrations,
                providers,
                stateStore);
        }
        catch
        {
            await builtProvider.DisposeAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (IHostedService hostedService in hostedServices.Reverse())
        {
            await hostedService.StopAsync(CancellationToken.None);
        }

        await DeleteDatabaseIfPresentAsync();
        await serviceProvider.DisposeAsync();
    }

    private async Task DeleteDatabaseIfPresentAsync()
    {
        try
        {
            await sharedCosmosClient.GetDatabase(databaseId).DeleteAsync();
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Nothing to clean up.
        }
    }
}
