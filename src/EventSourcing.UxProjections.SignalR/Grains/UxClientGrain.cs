using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.UxProjections.SignalR.Grains.State;
using Mississippi.EventSourcing.UxProjections.SignalR.Messages;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


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
    /// <param name="options">Configuration options for the Orleans backplane.</param>
    /// <param name="logger">Logger instance for grain operations.</param>
    public UxClientGrain(
        IGrainContext grainContext,
        IOptions<OrleansBackplaneOptions> options,
        ILogger<UxClientGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger<UxClientGrain> Logger { get; }

    private IOptions<OrleansBackplaneOptions> Options { get; }

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
    public async Task SendMessageAsync(
        string methodName,
        object?[] args
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(methodName);
        ArgumentNullException.ThrowIfNull(args);
        string connectionId = ExtractConnectionId();
        Logger.SendingMessage(connectionId, methodName);

        // Get the server ID for this connection
        if (string.IsNullOrEmpty(state.ServerId))
        {
            Logger.ClientNotConnected(connectionId);
            return;
        }

        // Publish message to the server's stream for delivery
        StreamId serverStreamId = StreamId.Create(Options.Value.ServerStreamNamespace, state.ServerId);
        IAsyncStream<ServerMessage> stream = this.GetStreamProvider(Options.Value.StreamProviderName)
            .GetStream<ServerMessage>(serverStreamId);
        ServerMessage message = new()
        {
            ConnectionId = connectionId,
            MethodName = methodName,
            Args = args,
        };
        await stream.OnNextAsync(message);
    }

    private string ExtractConnectionId()
    {
        // Grain key format: "ConnectionId" (or "HubName:ConnectionId" depending on usage)
        string key = this.GetPrimaryKeyString();
        int separatorIndex = key.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex >= 0 ? key[(separatorIndex + 1)..] : key;
    }
}