using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections.SignalR.Grains.State;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections.SignalR.Grains;

/// <summary>
///     Orleans grain implementation that tracks active SignalR servers for failure detection.
/// </summary>
/// <remarks>
///     This grain maintains a registry of active SignalR servers. There is typically
///     one instance keyed by "default" that all servers register with.
/// </remarks>
[Alias("Mississippi.EventSourcing.UxProjections.SignalR.UxServerDirectoryGrain")]
internal sealed class UxServerDirectoryGrain : IUxServerDirectoryGrain, IGrainBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UxServerDirectoryGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="state">Persistent state for storing server directory.</param>
    /// <param name="logger">Logger instance for grain operations.</param>
    public UxServerDirectoryGrain(
        IGrainContext grainContext,
        [PersistentState("serverDirectory", "UxSignalRStore")]
        IPersistentState<UxServerDirectoryState> state,
        ILogger<UxServerDirectoryGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        State = state ?? throw new ArgumentNullException(nameof(state));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger<UxServerDirectoryGrain> Logger { get; }

    private IPersistentState<UxServerDirectoryState> State { get; }

    /// <inheritdoc />
    public Task<ImmutableList<string>> GetDeadServersAsync(TimeSpan timeout)
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow - timeout;

        ImmutableList<string> deadServers = State.State.ActiveServers.Values
            .Where(s => s.LastHeartbeat < cutoff)
            .Select(s => s.ServerId)
            .ToImmutableList();

        if (deadServers.Count > 0)
        {
            Logger.DeadServersFound(deadServers.Count, timeout);
        }

        return Task.FromResult(deadServers);
    }

    /// <inheritdoc />
    public async Task HeartbeatAsync(string serverId, int connectionCount)
    {
        ArgumentException.ThrowIfNullOrEmpty(serverId);

        if (!State.State.ActiveServers.TryGetValue(serverId, out ServerInfo? existing))
        {
            Logger.HeartbeatFromUnknownServer(serverId);
            return;
        }

        ServerInfo updated = existing with
        {
            LastHeartbeat = DateTimeOffset.UtcNow,
            ConnectionCount = connectionCount,
        };

        State.State = State.State with
        {
            ActiveServers = State.State.ActiveServers.SetItem(serverId, updated),
        };

        await State.WriteStateAsync();

        Logger.ServerHeartbeat(serverId, connectionCount);
    }

    /// <inheritdoc />
    public Task OnActivateAsync(CancellationToken token)
    {
        Logger.ServerDirectoryActivated(State.State.ActiveServers.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RegisterServerAsync(string serverId)
    {
        ArgumentException.ThrowIfNullOrEmpty(serverId);

        Logger.RegisteringServer(serverId);

        ServerInfo serverInfo = new()
        {
            ServerId = serverId,
            LastHeartbeat = DateTimeOffset.UtcNow,
            ConnectionCount = 0,
        };

        State.State = State.State with
        {
            ActiveServers = State.State.ActiveServers.SetItem(serverId, serverInfo),
        };

        await State.WriteStateAsync();

        Logger.ServerRegistered(serverId, State.State.ActiveServers.Count);
    }

    /// <inheritdoc />
    public async Task UnregisterServerAsync(string serverId)
    {
        ArgumentException.ThrowIfNullOrEmpty(serverId);

        Logger.UnregisteringServer(serverId);

        if (!State.State.ActiveServers.ContainsKey(serverId))
        {
            Logger.ServerNotFound(serverId);
            return;
        }

        State.State = State.State with
        {
            ActiveServers = State.State.ActiveServers.Remove(serverId),
        };

        await State.WriteStateAsync();

        Logger.ServerUnregistered(serverId, State.State.ActiveServers.Count);
    }
}
