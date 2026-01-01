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
///     <para>
///         This grain is keyed by "{HubName}:{ConnectionId}" and maintains connection
///         metadata for message routing.
///     </para>
///     <para>
///         State is maintained in-memory for the lifetime of the connection. When the
///         connection disconnects, the grain is deactivated and state is lost. This is
///         intentional as connection state is ephemeral.
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.UxProjections.SignalR.UxClientGrain")]
internal sealed class UxClientGrain
    : IUxClientGrain,
      IGrainBase
{
    private UxClientState state = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="UxClientGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="logger">Logger instance for grain operations.</param>
    public UxClientGrain(
        IGrainContext grainContext,
        ILogger<UxClientGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger<UxClientGrain> Logger { get; }

    /// <inheritdoc />
    public Task ConnectAsync(
        string hubName,
        string serverId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(hubName);
        ArgumentException.ThrowIfNullOrEmpty(serverId);
        string connectionId = ExtractConnectionId();
        Logger.ClientConnecting(connectionId, hubName, serverId);
        state = new()
        {
            ConnectionId = connectionId,
            HubName = hubName,
            ServerId = serverId,
            ConnectedAt = DateTimeOffset.UtcNow,
        };
        Logger.ClientConnected(connectionId, hubName, serverId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisconnectAsync()
    {
        string connectionId = ExtractConnectionId();
        Logger.ClientDisconnecting(connectionId);
        state = new();
        Logger.ClientDisconnected(connectionId);
        this.DeactivateOnIdle();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetServerIdAsync()
    {
        string? serverId = string.IsNullOrEmpty(state.ServerId) ? null : state.ServerId;
        return Task.FromResult(serverId);
    }

    /// <inheritdoc />
    public Task OnActivateAsync(
        CancellationToken token
    )
    {
        string connectionId = ExtractConnectionId();
        Logger.ClientGrainActivated(connectionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendMessageAsync(
        string methodName,
        object?[] args
    )
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
        return separatorIndex >= 0 ? key[(separatorIndex + 1)..] : key;
    }
}