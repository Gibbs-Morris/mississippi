using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Runtime.Diagnostics;
using Mississippi.Aqueduct.Runtime.Grains.State;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.Aqueduct.Runtime.Grains;

/// <summary>
///     Orleans grain implementation that tracks active SignalR servers for failure detection.
/// </summary>
/// <remarks>
///     <para>
///         This grain maintains a registry of active SignalR servers. There is typically
///         one instance keyed by "default" that all servers register with.
///     </para>
///     <para>
///         State is maintained in-memory. Servers re-register on startup and send periodic
///         heartbeats. Dead server detection uses heartbeat timestamps.
///     </para>
///     <para>
///         <b>Deployment Note:</b> This grain runs on Orleans silo hosts. ASP.NET pods
///         running <c>AqueductHubLifetimeManager</c> call this grain to register themselves
///         and send periodic heartbeats.
///     </para>
/// </remarks>
[Alias("Mississippi.Aqueduct.Runtime.Grains.SignalRServerDirectoryGrain")]
internal sealed class SignalRServerDirectoryGrain
    : ISignalRServerDirectoryGrain,
      ISignalRServerLivenessGrain,
      IGrainBase
{
    private SignalRServerDirectoryState state = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRServerDirectoryGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="options">The shared heartbeat timing that bounds directory recovery and stop markers.</param>
    /// <param name="logger">Logger instance for grain operations.</param>
    /// <param name="timeProvider">Time provider for timestamps. If null, uses <see cref="System.TimeProvider.System" />.</param>
    public SignalRServerDirectoryGrain(
        IGrainContext grainContext,
        IOptions<AqueductOptions> options,
        ILogger<SignalRServerDirectoryGrain> logger,
        TimeProvider? timeProvider = null
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        TimeProvider = timeProvider ?? TimeProvider.System;
        RecoveryStartedAt = TimeProvider.GetUtcNow();
        RecoveryTimeout = TimeSpan.FromMinutes(
            (double)Options.Value.HeartbeatIntervalMinutes * Options.Value.DeadServerTimeoutMultiplier);
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger<SignalRServerDirectoryGrain> Logger { get; }

    private IOptions<AqueductOptions> Options { get; }

    private DateTimeOffset RecoveryStartedAt { get; }

    private TimeSpan RecoveryTimeout { get; }

    private TimeProvider TimeProvider { get; }

    private Dictionary<string, DateTimeOffset> Unregistrations { get; } = [];

    /// <inheritdoc />
    public Task<ImmutableList<string>> GetDeadServersAsync(
        TimeSpan timeout
    )
    {
        DateTimeOffset cutoff = TimeProvider.GetUtcNow() - timeout;
        ImmutableList<string> deadServers = state.ActiveServers.Values.Where(s => s.LastHeartbeat < cutoff)
            .Select(s => s.ServerId)
            .ToImmutableList();
        if (deadServers.Count > 0)
        {
            AqueductMetrics.RecordDeadServers(deadServers.Count);
            Logger.DeadServersFound(deadServers.Count, timeout);
        }

        return Task.FromResult(deadServers);
    }

    /// <inheritdoc />
    public async Task HeartbeatAsync(
        string serverId,
        int connectionCount
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(serverId);
        RemoveExpiredUnregistrations();
        if (Unregistrations.ContainsKey(serverId))
        {
            Logger.HeartbeatFromUnknownServer(serverId);
            return;
        }

        if (!state.ActiveServers.TryGetValue(serverId, out SignalRServerInfo? existing))
        {
            await RegisterServerAsync(serverId).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            existing = state.ActiveServers[serverId];
        }

        SignalRServerInfo updated = existing with
        {
            LastHeartbeat = TimeProvider.GetUtcNow(),
            ConnectionCount = connectionCount,
        };
        state = state with
        {
            ActiveServers = state.ActiveServers.SetItem(serverId, updated),
        };
        AqueductMetrics.RecordServerHeartbeat();
        Logger.ServerHeartbeat(serverId, connectionCount);
    }

    /// <inheritdoc />
    public Task<bool> IsServerAliveAsync(
        string serverId,
        TimeSpan timeout
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(serverId);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        RemoveExpiredUnregistrations();
        DateTimeOffset now = TimeProvider.GetUtcNow();
        bool isAlive = state.ActiveServers.TryGetValue(serverId, out SignalRServerInfo? server)
            ? (now - server.LastHeartbeat) <= timeout
            : !Unregistrations.ContainsKey(serverId) &&
              ((now - RecoveryStartedAt) <= timeout) &&
              ((now - RecoveryStartedAt) <= RecoveryTimeout);
        return Task.FromResult(isAlive);
    }

    /// <inheritdoc />
    public Task OnActivateAsync(
        CancellationToken token
    )
    {
        Logger.ServerDirectoryActivated(state.ActiveServers.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RegisterServerAsync(
        string serverId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(serverId);
        RemoveExpiredUnregistrations();
        Unregistrations.Remove(serverId);
        Logger.RegisteringServer(serverId);
        SignalRServerInfo serverInfo = new()
        {
            ServerId = serverId,
            LastHeartbeat = TimeProvider.GetUtcNow(),
            ConnectionCount = 0,
        };
        state = state with
        {
            ActiveServers = state.ActiveServers.SetItem(serverId, serverInfo),
        };
        AqueductMetrics.RecordServerRegister();
        Logger.ServerRegistered(serverId, state.ActiveServers.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnregisterServerAsync(
        string serverId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(serverId);
        RemoveExpiredUnregistrations();
        Unregistrations[serverId] = TimeProvider.GetUtcNow();
        Logger.UnregisteringServer(serverId);
        if (!state.ActiveServers.ContainsKey(serverId))
        {
            Logger.ServerNotFound(serverId);
            return Task.CompletedTask;
        }

        state = state with
        {
            ActiveServers = state.ActiveServers.Remove(serverId),
        };
        Logger.ServerUnregistered(serverId, state.ActiveServers.Count);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bounds recent-stop metadata to one configured heartbeat timeout.
    /// </summary>
    private void RemoveExpiredUnregistrations()
    {
        DateTimeOffset now = TimeProvider.GetUtcNow();
        string[] expired = Unregistrations.Where(entry => (now - entry.Value) > RecoveryTimeout)
            .Select(entry => entry.Key)
            .ToArray();
        foreach (string serverId in expired)
        {
            Unregistrations.Remove(serverId);
        }
    }
}