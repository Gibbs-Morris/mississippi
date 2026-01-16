using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;

using Orleans.Runtime;


namespace Mississippi.Aqueduct;

/// <summary>
///     Manages periodic heartbeat operations for server liveness tracking.
/// </summary>
/// <remarks>
///     <para>
///         This manager registers the server with the server directory grain on start
///         and sends periodic heartbeats containing the current connection count.
///         On stop, it unregisters the server from the directory.
///     </para>
///     <para>
///         Heartbeat failures are logged but do not interrupt operation, as the
///         server directory grain has timeout-based dead server detection.
///     </para>
/// </remarks>
internal sealed class HeartbeatManager : IHeartbeatManager
{
    private readonly SemaphoreSlim startLock = new(1);

    private Func<int>? connectionCountProvider;

    private bool disposed;

    private Timer? heartbeatTimer;

    private bool started;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HeartbeatManager" /> class.
    /// </summary>
    /// <param name="grainFactory">The grain factory for resolving the server directory grain.</param>
    /// <param name="options">Configuration options for heartbeat intervals.</param>
    /// <param name="logger">Logger instance for heartbeat operations.</param>
    public HeartbeatManager(
        IAqueductGrainFactory grainFactory,
        IOptions<AqueductOptions> options,
        ILogger<HeartbeatManager> logger
    )
    {
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ServerId = Guid.NewGuid().ToString("N");
    }

    /// <inheritdoc />
    public string ServerId { get; }

    private IAqueductGrainFactory GrainFactory { get; }

    private ILogger<HeartbeatManager> Logger { get; }

    private IOptions<AqueductOptions> Options { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        heartbeatTimer?.Dispose();
        startLock.Dispose();

        // Fire-and-forget unregistration - we can't await in Dispose
        // The server directory grain will clean up stale servers via heartbeat timeout
        ISignalRServerDirectoryGrain directoryGrain = GrainFactory.GetServerDirectoryGrain();
        _ = directoryGrain.UnregisterServerAsync(ServerId);
    }

    /// <inheritdoc />
    public async Task StartAsync(
        Func<int> connectionCountProvider,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connectionCountProvider);
        await startLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (started)
            {
                return;
            }

            this.connectionCountProvider = connectionCountProvider;

            // Register with server directory
            ISignalRServerDirectoryGrain directoryGrain = GrainFactory.GetServerDirectoryGrain();
            await directoryGrain.RegisterServerAsync(ServerId).ConfigureAwait(false);
            Logger.HeartbeatStarted(ServerId);

            // Start heartbeat timer
            Timer newTimer = new(
                HeartbeatCallback,
                null,
                TimeSpan.Zero,
                TimeSpan.FromMinutes(Options.Value.HeartbeatIntervalMinutes));
            try
            {
                Timer? existingTimer = Interlocked.Exchange(ref heartbeatTimer, newTimer);
                if (existingTimer != null)
                {
                    await existingTimer.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                await newTimer.DisposeAsync().ConfigureAwait(false);
                throw;
            }

            started = true;
        }
        finally
        {
            startLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (heartbeatTimer != null)
        {
            await heartbeatTimer.DisposeAsync().ConfigureAwait(false);
            heartbeatTimer = null;
        }

        ISignalRServerDirectoryGrain directoryGrain = GrainFactory.GetServerDirectoryGrain();
        await directoryGrain.UnregisterServerAsync(ServerId).ConfigureAwait(false);
        Logger.HeartbeatStopped(ServerId);
        started = false;
    }

    private async Task HeartbeatAsync()
    {
        try
        {
            int connectionCount = connectionCountProvider?.Invoke() ?? 0;
            ISignalRServerDirectoryGrain directoryGrain = GrainFactory.GetServerDirectoryGrain();
            await directoryGrain.HeartbeatAsync(ServerId, connectionCount).ConfigureAwait(false);
        }
        catch (OrleansException ex)
        {
            Logger.HeartbeatFailed(ServerId, ex);
        }
    }

    private void HeartbeatCallback(
        object? state
    )
    {
        // Fire-and-forget: explicitly discard to satisfy VSTHRD110
        _ = Task.Run(HeartbeatAsync);
    }
}