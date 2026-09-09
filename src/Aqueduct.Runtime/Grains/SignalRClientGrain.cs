using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Abstractions.Keys;
using Mississippi.Aqueduct.Abstractions.Messages;
using Mississippi.Aqueduct.Runtime.Diagnostics;
using Mississippi.Aqueduct.Runtime.Grains.State;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.Aqueduct.Runtime.Grains;

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
///         <c>AqueductHubLifetimeManager</c>.
///     </para>
/// </remarks>
[Alias("Mississippi.Aqueduct.Runtime.Grains.SignalRClientGrain")]
internal sealed class SignalRClientGrain
    : ISignalRClientGrain,
      IGrainBase
{
    private readonly HashSet<string> groups = new(StringComparer.Ordinal);

    private SignalRClientState state = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRClientGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="grainFactory">The factory for resolving the client's group grains.</param>
    /// <param name="options">Configuration options for the Orleans-SignalR bridge.</param>
    /// <param name="logger">Logger instance for grain operations.</param>
    /// <param name="timeProvider">Time provider for timestamps. If null, uses <see cref="System.TimeProvider.System" />.</param>
    public SignalRClientGrain(
        IGrainContext grainContext,
        IGrainFactory grainFactory,
        IOptions<AqueductOptions> options,
        ILogger<SignalRClientGrain> logger,
        TimeProvider? timeProvider = null
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private IGrainFactory GrainFactory { get; }

    private ILogger<SignalRClientGrain> Logger { get; }

    private IOptions<AqueductOptions> Options { get; }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public async Task AddToGroupAsync(
        string groupName
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(groupName);
        string connectionId = ExtractConnectionId();
        Stopwatch operationTimer = Stopwatch.StartNew();
        Logger.ClientGroupChanging(connectionId, state.HubName, groupName, "join");
        bool isConnected = !string.IsNullOrEmpty(state.ServerId);
        if (isConnected)
        {
            // Retain cleanup ownership even when the remote add has an uncertain outcome.
            groups.Add(groupName);
            await GetGroupGrain(groupName)
                .AddConnectionAsync(connectionId)
                .ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
        }

        Logger.ClientGroupChanged(
            connectionId,
            groupName,
            "join",
            isConnected,
            groups.Count,
            operationTimer.Elapsed.TotalMilliseconds);
    }

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
            ConnectedAt = TimeProvider.GetUtcNow(),
        };
        AqueductMetrics.RecordClientConnect(hubName);
        Logger.ClientConnected(connectionId, hubName, serverId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        string connectionId = ExtractConnectionId();
        Logger.ClientDisconnecting(connectionId);
        if (!string.IsNullOrEmpty(state.ServerId))
        {
            AqueductMetrics.RecordClientDisconnect(state.HubName);
        }

        // Stop delivery and reject later joins before awaiting remote cleanup.
        state = state with
        {
            ServerId = string.Empty,
        };
        string[] joinedGroups = groups.ToArray();
        await Task.WhenAll(joinedGroups.Select(RemoveFromGroupAsync))
            .ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

        // Failed removals remain tracked and retryable; only complete cleanup deactivates.
        state = new();
        Logger.ClientDisconnected(connectionId);
        this.DeactivateOnIdle();
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
    public async Task RemoveFromGroupAsync(
        string groupName
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(groupName);
        string connectionId = ExtractConnectionId();
        Stopwatch operationTimer = Stopwatch.StartNew();
        Logger.ClientGroupChanging(connectionId, state.HubName, groupName, "remove");
        bool hasHub = !string.IsNullOrEmpty(state.HubName);
        if (hasHub)
        {
            await GetGroupGrain(groupName)
                .RemoveConnectionAsync(connectionId)
                .ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            groups.Remove(groupName);
        }

        Logger.ClientGroupChanged(
            connectionId,
            groupName,
            "remove",
            hasHub,
            groups.Count,
            operationTimer.Elapsed.TotalMilliseconds);
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
        Stopwatch sw = Stopwatch.StartNew();
        StreamId serverStreamId = StreamId.Create(Options.Value.ServerStreamNamespace, state.ServerId);
        IAsyncStream<ServerMessage> stream = this.GetStreamProvider(Options.Value.StreamProviderName)
            .GetStream<ServerMessage>(serverStreamId);
        ServerMessage message = new()
        {
            ConnectionId = connectionId,
            MethodName = method,
            Args = args.AsSpan().ToArray(),
        };
        await stream.OnNextAsync(message).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
        sw.Stop();
        AqueductMetrics.RecordClientMessageSent(state.HubName, method, sw.Elapsed.TotalMilliseconds);
    }

    private string ExtractConnectionId()
    {
        // Grain key format: "ConnectionId" (or "HubName:ConnectionId" depending on usage)
        string key = this.GetPrimaryKeyString();
        int separatorIndex = key.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex >= 0 ? key[(separatorIndex + 1)..] : key;
    }

    private ISignalRGroupGrain GetGroupGrain(
        string groupName
    ) =>
        GrainFactory.GetGrain<ISignalRGroupGrain>(new SignalRGroupKey(state.HubName, groupName));
}