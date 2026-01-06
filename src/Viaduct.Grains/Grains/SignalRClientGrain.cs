using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Viaduct.Grains.State;
using Mississippi.Viaduct.Messages;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.Viaduct.Grains;

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
///     <para>
///         <b>Deployment Note:</b> This grain runs on Orleans silo hosts. It publishes
///         messages to server streams which are consumed by ASP.NET pods running
///         <c>OrleansHubLifetimeManager</c>.
///     </para>
/// </remarks>
[Alias("Mississippi.Viaduct.SignalRClientGrain")]
internal sealed class SignalRClientGrain
    : ISignalRClientGrain,
      IGrainBase
{
    private SignalRClientState state = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRClientGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="options">Configuration options for the Orleans-SignalR bridge.</param>
    /// <param name="logger">Logger instance for grain operations.</param>
    public SignalRClientGrain(
        IGrainContext grainContext,
        IOptions<OrleansSignalROptions> options,
        ILogger<SignalRClientGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger<SignalRClientGrain> Logger { get; }

    private IOptions<OrleansSignalROptions> Options { get; }

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
        string method,
        ImmutableArray<object?> args
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(method);
        string connectionId = ExtractConnectionId();
        Logger.SendingMessage(connectionId, method);

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
            MethodName = method,
            Args = [.. args],
        };
        await stream.OnNextAsync(message).ConfigureAwait(false);
    }

    private string ExtractConnectionId()
    {
        // Grain key format: "ConnectionId" (or "HubName:ConnectionId" depending on usage)
        string key = this.GetPrimaryKeyString();
        int separatorIndex = key.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex >= 0 ? key[(separatorIndex + 1)..] : key;
    }
}