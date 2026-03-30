namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L2Tests;

/// <summary>
///     Starts the minimal Cosmos-emulator AppHost and exposes a real Cosmos client configured for emulator use.
/// </summary>
#pragma warning disable CA1515 // xUnit fixture types must be public for the existing repo test pattern
public sealed class CosmosReplicaSinkCalibrationAppHostFixture
        : IAsyncLifetime,
            IDisposable
#pragma warning restore CA1515
{
    private const string CosmosResourceName = "cosmos";

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(4);

    private DistributedApplication? app;

    private string? cosmosConnectionString;

    private bool disposed;

    /// <summary>
    ///     Gets the initialization error captured during AppHost startup, when one occurs.
    /// </summary>
    public Exception? InitializationError { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the fixture finished startup successfully.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    ///     Creates a deterministic workload runner bound to the emulator-backed Cosmos client.
    /// </summary>
    /// <returns>The deterministic workload runner.</returns>
    internal CosmosReplicaSinkCalibrationWorkloadRunner CreateWorkloadRunner()
    {
        EnsureInitialized();
        return new(cosmosConnectionString ?? throw new InvalidOperationException("Cosmos connection string not initialized."));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        app?.Dispose();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (app is not null)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    /// <inheritdoc />
#pragma warning disable IDISP001 // Dispose created - AppHost fixture follows the repository Aspire testing pattern
    public async Task InitializeAsync()
    {
        try
        {
            if (app is not null)
            {
                await app.DisposeAsync();
            }

            IDistributedApplicationTestingBuilder builder =
                await DistributedApplicationTestingBuilder.CreateAsync<ReplicaSinkCosmosCalibrationAppHost>();
            builder.Services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddFilter("Aspire", LogLevel.Warning);
                logging.AddFilter("Microsoft.Hosting", LogLevel.Warning);
            });

            DistributedApplication builtApp = await builder.BuildAsync().WaitAsync(DefaultTimeout);
            app = builtApp;
            await app.StartAsync().WaitAsync(DefaultTimeout);

            using CancellationTokenSource cts = new(DefaultTimeout);
            await app.ResourceNotifications.WaitForResourceHealthyAsync(CosmosResourceName, cts.Token)
                .WaitAsync(DefaultTimeout, cts.Token);

            string connectionString = await app.GetConnectionStringAsync(CosmosResourceName, cts.Token) ??
                                      throw new InvalidOperationException(
                                          "Failed to resolve the Cosmos emulator connection string.");

            cosmosConnectionString = connectionString;

            IsInitialized = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            InitializationError = ex;
            IsInitialized = false;
            throw;
        }
    }
#pragma warning restore IDISP001

    private void EnsureInitialized()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException(
                InitializationError is null
                    ? "The Cosmos calibration fixture is not initialized."
                    : $"The Cosmos calibration fixture failed to initialize: {InitializationError.Message}");
        }
    }
}
