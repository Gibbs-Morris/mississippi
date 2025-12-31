using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections.SignalR.Grains.State;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections.SignalR.Grains;

/// <summary>
///     Orleans grain implementation that tracks a single SignalR connection.
/// </summary>
/// <remarks>
///     This grain is keyed by "{HubName}:{ConnectionId}" and persists connection
///     metadata to support failure recovery and message routing.
/// </remarks>
[Alias("Mississippi.EventSourcing.UxProjections.SignalR.UxClientGrain")]
internal sealed class UxClientGrain : IUxClientGrain, IGrainBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UxClientGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="state">Persistent state for storing client information.</param>
    /// <param name="logger">Logger instance for grain operations.</param>
    public UxClientGrain(
        IGrainContext grainContext,
        [PersistentState("client", "UxSignalRStore")]
        IPersistentState<UxClientState> state,
        ILogger<UxClientGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        State = state ?? throw new ArgumentNullException(nameof(state));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger<UxClientGrain> Logger { get; }

    private IPersistentState<UxClientState> State { get; }

    /// <inheritdoc />
    public async Task ConnectAsync(string hubName, string serverId)
    {
        ArgumentException.ThrowIfNullOrEmpty(hubName);
        ArgumentException.ThrowIfNullOrEmpty(serverId);

        string connectionId = ExtractConnectionId();
        Logger.ClientConnecting(connectionId, hubName, serverId);

        State.State = new UxClientState
        {
            ConnectionId = connectionId,
            HubName = hubName,
            ServerId = serverId,
            ConnectedAt = DateTimeOffset.UtcNow,
        };

        await State.WriteStateAsync();

        Logger.ClientConnected(connectionId, hubName, serverId);
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        string connectionId = ExtractConnectionId();
        Logger.ClientDisconnecting(connectionId);

        await State.ClearStateAsync();

        Logger.ClientDisconnected(connectionId);

        this.DeactivateOnIdle();
    }

    /// <inheritdoc />
    public Task<string?> GetServerIdAsync()
    {
        string? serverId = string.IsNullOrEmpty(State.State.ServerId)
            ? null
            : State.State.ServerId;
        return Task.FromResult(serverId);
    }

    /// <inheritdoc />
    public Task OnActivateAsync(CancellationToken token)
    {
        string connectionId = ExtractConnectionId();
        Logger.ClientGrainActivated(connectionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendMessageAsync(string methodName, object?[] args)
    {
        ArgumentException.ThrowIfNullOrEmpty(methodName);
        ArgumentNullException.ThrowIfNull(args);

        string connectionId = ExtractConnectionId();
        Logger.SendingMessage(connectionId, methodName);

        // Note: Actual message delivery is handled by the HubLifetimeManager.
        // This grain stores state; the message is routed through Orleans streams.
        return Task.CompletedTask;
    }

    private string ExtractConnectionId()
    {
        // Grain key format: "ConnectionId" (or "HubName:ConnectionId" depending on usage)
        string key = this.GetPrimaryKeyString();
        int separatorIndex = key.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex >= 0
            ? key[(separatorIndex + 1)..]
            : key;
    }
}
