// <copyright file="OrleansHubLifetimeManager.cs" company="Gibbs-Morris">
// Proprietary and Confidential.
// All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.AspNetCore.SignalR.Orleans.Grains;
using Mississippi.AspNetCore.SignalR.Orleans.Messages;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.AspNetCore.SignalR.Orleans;

/// <summary>
///     A custom <see cref="HubLifetimeManager{THub}" /> implementation that uses Orleans
///     grains as the backplane for distributed SignalR message routing.
/// </summary>
/// <typeparam name="THub">The type of hub being managed.</typeparam>
/// <remarks>
///     <para>
///         This lifetime manager replaces the default SignalR manager and routes all
///         connection, group, and message operations through Orleans grains. This enables
///         cross-server SignalR message delivery without requiring an external backplane
///         like Redis or Azure SignalR.
///     </para>
///     <para>
///         The manager uses three types of grains:
///         <list type="bullet">
///             <item>
///                 <see cref="ISignalRClientGrain" /> - Tracks individual connections and their hosting servers.
///             </item>
///             <item>
///                 <see cref="ISignalRGroupGrain" /> - Manages group membership and broadcasts to groups.
///             </item>
///             <item>
///                 <see cref="ISignalRServerDirectoryGrain" /> - Tracks active servers for failure detection.
///             </item>
///         </list>
///     </para>
///     <para>
///         Orleans streams are used for server-targeted messages and broadcasts.
///         Each server subscribes to its own stream and the hub's all-clients stream.
///     </para>
/// </remarks>
public sealed class OrleansHubLifetimeManager<THub>
    : HubLifetimeManager<THub>,
      ILifecycleParticipant<ISiloLifecycle>,
      IDisposable
    where THub : Hub
{
    private readonly ConcurrentDictionary<string, HubConnectionContext> connections = new();

    private readonly string hubName;

    private readonly SemaphoreSlim streamSetupLock = new(1);

    private IAsyncStream<AllMessage>? allStream;

    private bool disposed;

    private Timer? heartbeatTimer;

    private IDisposable? lifecycleSubscription;

    private volatile bool streamsInitialized;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OrleansHubLifetimeManager{THub}" /> class.
    /// </summary>
    /// <param name="clusterClient">The Orleans cluster client for grain operations.</param>
    /// <param name="options">Configuration options for the backplane.</param>
    /// <param name="logger">Logger instance for backplane operations.</param>
    public OrleansHubLifetimeManager(
        IClusterClient clusterClient,
        IOptions<OrleansSignalROptions> options,
        ILogger<OrleansHubLifetimeManager<THub>> logger
    )
    {
        ClusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ServerId = Guid.NewGuid().ToString("N");
        hubName = DeriveHubName();
    }

    private IClusterClient ClusterClient { get; }

    private ILogger<OrleansHubLifetimeManager<THub>> Logger { get; }

    private IOptions<OrleansSignalROptions> Options { get; }

    private string ServerId { get; }

    private static string DeriveHubName()
    {
        Type hubType = typeof(THub);

        // Check for strongly-typed hub interface
        Type? interfaceType = hubType.BaseType?.GenericTypeArguments.FirstOrDefault();
        if ((interfaceType != null) &&
            interfaceType.IsInterface &&
            (interfaceType.Name.Length > 1) &&
            (interfaceType.Name[0] == 'I'))
        {
            return interfaceType.Name[1..];
        }

        return hubType.Name;
    }

    /// <inheritdoc />
    public override async Task AddToGroupAsync(
        string connectionId,
        string groupName,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionId);
        ArgumentException.ThrowIfNullOrEmpty(groupName);
        Logger.AddingToGroup(connectionId, groupName, hubName);
        ISignalRGroupGrain groupGrain = GetGroupGrain(groupName);
        await groupGrain.AddConnectionAsync(connectionId).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        heartbeatTimer?.Dispose();
        lifecycleSubscription?.Dispose();
        streamSetupLock.Dispose();

        // Fire-and-forget unregistration - we can't await in Dispose
        // The server directory grain will clean up stale servers via heartbeat timeout
        ISignalRServerDirectoryGrain directoryGrain = GetServerDirectoryGrain();
        _ = directoryGrain.UnregisterServerAsync(ServerId);
    }

    /// <inheritdoc />
    public override async Task OnConnectedAsync(
        HubConnectionContext connection
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        await EnsureStreamSetupAsync().ConfigureAwait(false);
        connections[connection.ConnectionId] = connection;
        ISignalRClientGrain clientGrain = GetClientGrain(connection.ConnectionId);
        await clientGrain.ConnectAsync(hubName, ServerId).ConfigureAwait(false);
        Logger.ConnectionRegistered(connection.ConnectionId, hubName, ServerId);
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(
        HubConnectionContext connection
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        Logger.ConnectionDisconnecting(connection.ConnectionId, hubName);
        _ = connections.TryRemove(connection.ConnectionId, out HubConnectionContext? _);
        ISignalRClientGrain clientGrain = GetClientGrain(connection.ConnectionId);
        await clientGrain.DisconnectAsync().ConfigureAwait(false);
        Logger.ConnectionUnregistered(connection.ConnectionId, hubName);
    }

    /// <inheritdoc />
    public void Participate(
        ISiloLifecycle lifecycle
    )
    {
        ArgumentNullException.ThrowIfNull(lifecycle);

        // Dispose any previous subscription (shouldn't happen, but satisfies analyzer)
        lifecycleSubscription?.Dispose();
        lifecycleSubscription = lifecycle.Subscribe(
            nameof(OrleansHubLifetimeManager<THub>),
            ServiceLifecycleStage.Active,
            async ct => await EnsureStreamSetupAsync(ct).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public override async Task RemoveFromGroupAsync(
        string connectionId,
        string groupName,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionId);
        ArgumentException.ThrowIfNullOrEmpty(groupName);
        Logger.RemovingFromGroup(connectionId, groupName, hubName);
        ISignalRGroupGrain groupGrain = GetGroupGrain(groupName);
        await groupGrain.RemoveConnectionAsync(connectionId).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task SendAllAsync(
        string methodName,
        object?[] args,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(methodName);
        await EnsureStreamSetupAsync(cancellationToken).ConfigureAwait(false);
        AllMessage message = new()
        {
            MethodName = methodName,
            Args = args ?? [],
        };
        await allStream!.OnNextAsync(message).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task SendAllExceptAsync(
        string methodName,
        object?[] args,
        IReadOnlyList<string> excludedConnectionIds,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(methodName);
        await EnsureStreamSetupAsync(cancellationToken).ConfigureAwait(false);
        AllMessage message = new()
        {
            MethodName = methodName,
            Args = args ?? [],
            ExcludedConnectionIds = excludedConnectionIds,
        };
        await allStream!.OnNextAsync(message).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task SendConnectionAsync(
        string connectionId,
        string methodName,
        object?[] args,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionId);
        ArgumentException.ThrowIfNullOrEmpty(methodName);

        // Try local connection first
        if (connections.TryGetValue(connectionId, out HubConnectionContext? connection))
        {
            await SendLocalAsync(connection, methodName, args ?? []).ConfigureAwait(false);
            return;
        }

        // Route via client grain for remote connections
        ISignalRClientGrain clientGrain = GetClientGrain(connectionId);
        await clientGrain.SendMessageAsync(methodName, [.. args ?? []]).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task SendConnectionsAsync(
        IReadOnlyList<string> connectionIds,
        string methodName,
        object?[] args,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connectionIds);
        IEnumerable<Task> tasks =
            connectionIds.Select(id => SendConnectionAsync(id, methodName, args, cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task SendGroupAsync(
        string groupName,
        string methodName,
        object?[] args,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(groupName);
        ArgumentException.ThrowIfNullOrEmpty(methodName);
        Logger.SendingToGroup(groupName, methodName, hubName);
        ISignalRGroupGrain groupGrain = GetGroupGrain(groupName);
        await groupGrain.SendMessageAsync(methodName, [.. args ?? []]).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task SendGroupExceptAsync(
        string groupName,
        string methodName,
        object?[] args,
        IReadOnlyList<string> excludedConnectionIds,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(groupName);
        ArgumentException.ThrowIfNullOrEmpty(methodName);

        // Get group members and filter
        ISignalRGroupGrain groupGrain = GetGroupGrain(groupName);
        ImmutableHashSet<string> members = await groupGrain.GetConnectionsAsync().ConfigureAwait(false);
        HashSet<string> excluded = new(excludedConnectionIds ?? []);
        IEnumerable<Task> tasks = members.Where(id => !excluded.Contains(id))
            .Select(id => SendConnectionAsync(id, methodName, args, cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task SendGroupsAsync(
        IReadOnlyList<string> groupNames,
        string methodName,
        object?[] args,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(groupNames);
        IEnumerable<Task> tasks = groupNames.Select(g => SendGroupAsync(g, methodName, args, cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task SendUserAsync(
        string userId,
        string methodName,
        object?[] args,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(methodName);

        // Users are tracked via groups named by user ID
        string userGroupName = $"user:{userId}";
        await SendGroupAsync(userGroupName, methodName, args, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task SendUsersAsync(
        IReadOnlyList<string> userIds,
        string methodName,
        object?[] args,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(userIds);
        IEnumerable<Task> tasks = userIds.Select(u => SendUserAsync(u, methodName, args, cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task EnsureStreamSetupAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (streamsInitialized)
        {
            return;
        }

        await streamSetupLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (streamsInitialized)
            {
                return;
            }

            Logger.InitializingBackplane(hubName, ServerId);
            string streamProviderName = Options.Value.StreamProviderName;
            IStreamProvider streamProvider = ClusterClient.GetStreamProvider(streamProviderName);

            // Subscribe to server-specific stream
            StreamId serverStreamId = StreamId.Create(Options.Value.ServerStreamNamespace, ServerId);
            IAsyncStream<ServerMessage> serverStream = streamProvider.GetStream<ServerMessage>(serverStreamId);
            await serverStream.SubscribeAsync(OnServerMessageAsync).ConfigureAwait(false);

            // Subscribe to hub broadcast stream
            StreamId allStreamId = StreamId.Create(Options.Value.AllClientsStreamNamespace, hubName);
            allStream = streamProvider.GetStream<AllMessage>(allStreamId);
            await allStream.SubscribeAsync(OnAllMessageAsync).ConfigureAwait(false);

            // Register with server directory
            ISignalRServerDirectoryGrain directoryGrain = GetServerDirectoryGrain();
            await directoryGrain.RegisterServerAsync(ServerId).ConfigureAwait(false);

            // Start heartbeat timer - created once within the lock, no race possible
            Timer newTimer = new(
                HeartbeatCallback,
                null,
                TimeSpan.Zero,
                TimeSpan.FromMinutes(Options.Value.HeartbeatIntervalMinutes));
            Timer? existingTimer = Interlocked.Exchange(ref heartbeatTimer, newTimer);
            if (existingTimer != null)
            {
                await existingTimer.DisposeAsync().ConfigureAwait(false);
            }

            streamsInitialized = true;
            Logger.BackplaneInitialized(hubName, ServerId);
        }
        finally
        {
            streamSetupLock.Release();
        }
    }

    private ISignalRClientGrain GetClientGrain(
        string connectionId
    ) =>
        ClusterClient.GetGrain<ISignalRClientGrain>($"{hubName}:{connectionId}");

    private ISignalRGroupGrain GetGroupGrain(
        string groupName
    ) =>
        ClusterClient.GetGrain<ISignalRGroupGrain>($"{hubName}:{groupName}");

    private ISignalRServerDirectoryGrain GetServerDirectoryGrain() =>
        ClusterClient.GetGrain<ISignalRServerDirectoryGrain>("default");

    private async Task HeartbeatAsync()
    {
        try
        {
            ISignalRServerDirectoryGrain directoryGrain = GetServerDirectoryGrain();
            await directoryGrain.HeartbeatAsync(ServerId, connections.Count).ConfigureAwait(false);
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

    private async Task OnAllMessageAsync(
        AllMessage message,
        StreamSequenceToken? token
    )
    {
        foreach (HubConnectionContext connection in connections.Values)
        {
            if (connection.ConnectionAborted.IsCancellationRequested)
            {
                continue;
            }

            bool isExcluded = message.ExcludedConnectionIds?.Contains(connection.ConnectionId) ?? false;
            if (!isExcluded)
            {
                await SendLocalAsync(connection, message.MethodName, message.Args).ConfigureAwait(false);
            }
        }
    }

    private async Task OnServerMessageAsync(
        ServerMessage message,
        StreamSequenceToken? token
    )
    {
        if (connections.TryGetValue(message.ConnectionId, out HubConnectionContext? connection))
        {
            await SendLocalAsync(connection, message.MethodName, message.Args).ConfigureAwait(false);
        }
    }

    private async Task SendLocalAsync(
        HubConnectionContext connection,
        string methodName,
        IReadOnlyList<object?> args
    )
    {
        Logger.SendingLocalMessage(connection.ConnectionId, methodName, hubName);
        object?[] argsArray = args as object?[] ?? args.ToArray();
        InvocationMessage invocation = new(methodName, argsArray);
        await connection.WriteAsync(invocation).ConfigureAwait(false);
    }
}