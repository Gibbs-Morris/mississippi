using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Grains;


namespace Aqueduct.L2Tests;

/// <summary>
///     xUnit fixture that starts an Orleans silo with MemoryStreams for Aqueduct integration testing.
///     This fixture provides real Orleans grain infrastructure without external dependencies.
/// </summary>
/// <remarks>
///     <para>
///         Aqueduct grains are ephemeral (no persistent state) so no Cosmos DB or storage emulator is needed.
///         Orleans MemoryStreams provider is configured for SignalR message routing tests.
///     </para>
/// </remarks>
#pragma warning disable CA1515 // Types can be made internal - xUnit fixture must be public
#pragma warning disable IDISP002 // Dispose member - disposed in DisposeAsync
#pragma warning disable IDISP003 // Dispose previous before re-assigning - fields are null initially
public sealed class AqueductFixture
    : IAsyncLifetime,
      IDisposable
#pragma warning restore CA1515
{
    private bool disposed;

    private IHost? orleansHost;

    /// <summary>
    ///     Gets the Orleans cluster client for resolving grains.
    /// </summary>
    public IClusterClient ClusterClient =>
        orleansHost?.Services.GetRequiredService<IClusterClient>() ??
        throw new InvalidOperationException("Orleans host not initialized.");

    /// <summary>
    ///     Gets the initialization error if the fixture failed to start.
    /// </summary>
    public Exception? InitializationError { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the fixture initialized successfully.
    /// </summary>
    public bool IsInitialized { get; private set; }

    private static IHost BuildOrleansHost()
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
        builder.Logging.AddFilter("Mississippi.Aqueduct", LogLevel.Debug);

        // Configure Orleans silo with Aqueduct
        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering()
                .Configure<ClusterOptions>(opt =>
                {
                    opt.ClusterId = "aqueduct-l2tests";
                    opt.ServiceId = "AqueductTests";
                })

                // Use Aqueduct with MemoryStreams for testing
                .UseAqueduct(options => { options.UseMemoryStreams(); });
        });
        return builder.Build();
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
    }

    /// <summary>
    ///     Gets a SignalR client grain reference.
    /// </summary>
    /// <param name="hubName">The hub name.</param>
    /// <param name="connectionId">The connection identifier.</param>
    /// <returns>The client grain reference.</returns>
    public ISignalRClientGrain GetClientGrain(
        string hubName,
        string connectionId
    )
    {
        EnsureInitialized();
        return ClusterClient.GetGrain<ISignalRClientGrain>($"{hubName}:{connectionId}");
    }

    /// <summary>
    ///     Gets a SignalR group grain reference.
    /// </summary>
    /// <param name="hubName">The hub name.</param>
    /// <param name="groupName">The group name.</param>
    /// <returns>The group grain reference.</returns>
    public ISignalRGroupGrain GetGroupGrain(
        string hubName,
        string groupName
    )
    {
        EnsureInitialized();
        return ClusterClient.GetGrain<ISignalRGroupGrain>($"{hubName}:{groupName}");
    }

    /// <summary>
    ///     Gets the SignalR server directory grain reference.
    /// </summary>
    /// <returns>The server directory grain reference.</returns>
    public ISignalRServerDirectoryGrain GetServerDirectoryGrain()
    {
        EnsureInitialized();
        return ClusterClient.GetGrain<ISignalRServerDirectoryGrain>("default");
    }

    /// <summary>
    ///     Gets the stream provider for SignalR messaging.
    /// </summary>
    /// <returns>The stream provider.</returns>
    public IStreamProvider GetStreamProvider()
    {
        EnsureInitialized();
        return ClusterClient.GetStreamProvider("SignalRStreams");
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            Console.WriteLine("[AqueductFixture] Starting Orleans silo with MemoryStreams...");
            orleansHost = BuildOrleansHost();
            await orleansHost.StartAsync();
            Console.WriteLine("[AqueductFixture] Orleans silo started successfully.");
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            InitializationError = ex;
            IsInitialized = false;
            throw;
        }
    }

    private void EnsureInitialized()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException(
                "Aqueduct fixture is not initialized. " +
                (InitializationError is not null
                    ? $"Initialization failed: {InitializationError.Message}"
                    : "Call InitializeAsync() first."));
        }
    }
}