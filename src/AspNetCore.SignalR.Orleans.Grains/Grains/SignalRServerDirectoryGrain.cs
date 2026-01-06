using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.AspNetCore.SignalR.Orleans.Grains.State;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.AspNetCore.SignalR.Orleans.Grains;

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
///         running <c>OrleansHubLifetimeManager</c> call this grain to register themselves
///         and send periodic heartbeats.
///     </para>
/// </remarks>
[Alias("Mississippi.AspNetCore.SignalR.Orleans.SignalRServerDirectoryGrain")]
internal sealed class SignalRServerDirectoryGrain
    : ISignalRServerDirectoryGrain,
      IGrainBase
{
    private SignalRServerDirectoryState state = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRServerDirectoryGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="logger">Logger instance for grain operations.</param>
    public SignalRServerDirectoryGrain(
        IGrainContext grainContext,
        ILogger<SignalRServerDirectoryGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger<SignalRServerDirectoryGrain> Logger { get; }

    /// <inheritdoc />
    public Task<ImmutableList<string>> GetDeadServersAsync(
        TimeSpan timeout
    )
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow - timeout;
        ImmutableList<string> deadServers = state.ActiveServers.Values.Where(s => s.LastHeartbeat < cutoff)
            .Select(s => s.ServerId)
            .ToImmutableList();
        if (deadServers.Count > 0)
        {
            Logger.DeadServersFound(deadServers.Count, timeout);
        }

        return Task.FromResult(deadServers);
    }

    /// <inheritdoc />
    public Task HeartbeatAsync(
        string serverId,
        int connectionCount
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(serverId);
        if (!state.ActiveServers.TryGetValue(serverId, out SignalRServerInfo? existing))
        {
            Logger.HeartbeatFromUnknownServer(serverId);
            return Task.CompletedTask;
        }

        SignalRServerInfo updated = existing with
        {
            LastHeartbeat = DateTimeOffset.UtcNow,
            ConnectionCount = connectionCount,
        };
        state = state with
        {
            ActiveServers = state.ActiveServers.SetItem(serverId, updated),
        };
        Logger.ServerHeartbeat(serverId, connectionCount);
        return Task.CompletedTask;
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
        Logger.RegisteringServer(serverId);
        SignalRServerInfo serverInfo = new()
        {
            ServerId = serverId,
            LastHeartbeat = DateTimeOffset.UtcNow,
            ConnectionCount = 0,
        };
        state = state with
        {
            ActiveServers = state.ActiveServers.SetItem(serverId, serverInfo),
        };
        Logger.ServerRegistered(serverId, state.ActiveServers.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnregisterServerAsync(
        string serverId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(serverId);
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
}