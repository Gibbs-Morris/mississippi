using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Abstractions.Keys;
using Mississippi.Aqueduct.Abstractions.Messages;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.Aqueduct.Gateway;

/// <summary>
///     A custom <see cref="HubLifetimeManager{THub}" /> implementation that uses Aqueduct
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
///     <para>
///         Connections with a nonempty user identifier automatically join a reserved user group.
///         Group names beginning with "__aqueduct_user__" are reserved for this routing and cannot be
///         used by ordinary group operations. Lossless identifier encodings are hashed into opaque routing tokens
///         before reaching downstream logs. The encoded input remains subject to the existing
///         composite grain-key length limit.
///     </para>
/// </remarks>
public sealed class AqueductHubLifetimeManager<THub>
    : HubLifetimeManager<THub>,
      ILifecycleParticipant<ISiloLifecycle>,
      IDisposable
    where THub : Hub
{
    private readonly string hubName;

    private volatile bool backplaneInitialized;

    private bool disposed;

    private IDisposable? lifecycleSubscription;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AqueductHubLifetimeManager{THub}" /> class.
    /// </summary>
    /// <param name="serverIdProvider">The provider for the server's unique identifier.</param>
    /// <param name="grainFactory">The grain factory for resolving SignalR grains.</param>
    /// <param name="connectionRegistry">The registry for tracking local connections.</param>
    /// <param name="messageSender">The service for sending messages to local connections.</param>
    /// <param name="heartbeatManager">The manager for server heartbeat operations.</param>
    /// <param name="streamSubscriptionManager">The manager for Orleans stream subscriptions.</param>
    /// <param name="logger">Logger instance for backplane operations.</param>
    public AqueductHubLifetimeManager(
        IServerIdProvider serverIdProvider,
        IAqueductGrainFactory grainFactory,
        IConnectionRegistry connectionRegistry,
        ILocalMessageSender messageSender,
        IHeartbeatManager heartbeatManager,
        IStreamSubscriptionManager streamSubscriptionManager,
        ILogger<AqueductHubLifetimeManager<THub>> logger
    )
    {
        ArgumentNullException.ThrowIfNull(serverIdProvider);
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        ConnectionRegistry = connectionRegistry ?? throw new ArgumentNullException(nameof(connectionRegistry));
        MessageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
        HeartbeatManager = heartbeatManager ?? throw new ArgumentNullException(nameof(heartbeatManager));
        StreamSubscriptionManager = streamSubscriptionManager ??
                                    throw new ArgumentNullException(nameof(streamSubscriptionManager));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ServerId = serverIdProvider.ServerId;
        hubName = DeriveHubName();
    }

    private IConnectionRegistry ConnectionRegistry { get; }

    private IAqueductGrainFactory GrainFactory { get; }

    private IHeartbeatManager HeartbeatManager { get; }

    private ILogger<AqueductHubLifetimeManager<THub>> Logger { get; }

    private ILocalMessageSender MessageSender { get; }

    private string ServerId { get; }

    private IStreamSubscriptionManager StreamSubscriptionManager { get; }

    private static string DeriveHubName() => typeof(THub).Name;

    /// <inheritdoc />
    public override async Task AddToGroupAsync(
        string connectionId,
        string groupName,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionId);
        ArgumentException.ThrowIfNullOrEmpty(groupName);
        AqueductUserGroupNamespace.ThrowIfReserved(groupName);
        Logger.AddingToGroup(connectionId, groupName, hubName);
        ISignalRClientGrain clientGrain = GetClientGrain(connectionId);
        await clientGrain.AddToGroupAsync(groupName).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        lifecycleSubscription?.Dispose();
    }

    /// <inheritdoc />
    public override async Task OnConnectedAsync(
        HubConnectionContext connection
    )
    {
        ArgumentNullException.ThrowIfNull(connection);

        // Validate the encoded key before registering any connection state.
        string? userGroupName = string.IsNullOrEmpty(connection.UserIdentifier)
            ? null
            : GetUserGroupName(connection.UserIdentifier);
        if (userGroupName is not null)
        {
            _ = new SignalRGroupKey(hubName, userGroupName);
        }

        // Shared backplane initialization must outlive an individual connection.
        await EnsureStreamSetupAsync(CancellationToken.None).ConfigureAwait(false);
        ConnectionRegistry.TryAdd(connection.ConnectionId, connection);
        ISignalRClientGrain clientGrain = GetClientGrain(connection.ConnectionId);
        await clientGrain.ConnectAsync(hubName, ServerId).ConfigureAwait(false);
        if (userGroupName is not null)
        {
            await clientGrain.AddToGroupAsync(userGroupName).ConfigureAwait(false);
        }

        Logger.ConnectionRegistered(connection.ConnectionId, hubName, ServerId);
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(
        HubConnectionContext connection
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        Logger.ConnectionDisconnecting(connection.ConnectionId, hubName);
        _ = ConnectionRegistry.TryRemove(connection.ConnectionId);
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
            nameof(AqueductHubLifetimeManager<THub>),
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
        AqueductUserGroupNamespace.ThrowIfReserved(groupName);
        Logger.RemovingFromGroup(connectionId, groupName, hubName);
        ISignalRClientGrain clientGrain = GetClientGrain(connectionId);
        await clientGrain.RemoveFromGroupAsync(groupName).ConfigureAwait(false);
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
        await StreamSubscriptionManager.PublishToAllAsync(message).ConfigureAwait(false);
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
        await StreamSubscriptionManager.PublishToAllAsync(message).ConfigureAwait(false);
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
        HubConnectionContext? connection = ConnectionRegistry.GetConnection(connectionId);
        if (connection != null)
        {
            await MessageSender.SendAsync(connection, methodName, args ?? []).ConfigureAwait(false);
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
        string userGroupName = GetUserGroupName(userId);
        Logger.SendingToGroup(userGroupName, methodName, hubName);
        ISignalRGroupGrain groupGrain = GrainFactory.GetGroupGrain(hubName, userGroupName);
        await groupGrain.SendMessageAsync(methodName, [.. args ?? []]).ConfigureAwait(false);
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
        IEnumerable<Task> tasks = userIds.Distinct(StringComparer.Ordinal)
            .Select(u => SendUserAsync(u, methodName, args, cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task EnsureStreamSetupAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (backplaneInitialized && StreamSubscriptionManager.IsInitialized)
        {
            return;
        }

        Logger.InitializingBackplane(hubName, ServerId);

        // Initialize stream subscriptions via the manager
        await StreamSubscriptionManager.EnsureInitializedAsync(
                hubName,
                OnServerMessageAsync,
                OnAllMessageAsync,
                cancellationToken)
            .ConfigureAwait(false);

        // Start heartbeat manager
        await HeartbeatManager.StartAsync(() => ConnectionRegistry.Count, cancellationToken).ConfigureAwait(false);
        backplaneInitialized = true;
        Logger.BackplaneInitialized(hubName, ServerId);
    }

    private ISignalRClientGrain GetClientGrain(
        string connectionId
    ) =>
        GrainFactory.GetClientGrain(hubName, connectionId);

    private ISignalRGroupGrain GetGroupGrain(
        string groupName
    )
    {
        AqueductUserGroupNamespace.ThrowIfReserved(groupName);
        return GrainFactory.GetGroupGrain(hubName, groupName);
    }

    /// <summary>Derives an opaque routing token from a lossless user-identifier encoding.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A group name accepted by the default key factory.</returns>
    private string GetUserGroupName(
        string userId
    )
    {
        StringBuilder encoded = new(AqueductUserGroupNamespace.Prefix);
        int segmentStart = 0;
        int index = 0;
        while (index < userId.Length)
        {
            char value = userId[index];
            if (!char.IsSurrogate(value))
            {
                index++;
                continue;
            }

            if (char.IsHighSurrogate(value) && ((index + 1) < userId.Length) && char.IsLowSurrogate(userId[index + 1]))
            {
                index += 2;
                continue;
            }

            encoded.Append(Uri.EscapeDataString(userId[segmentStart..index]));

            // URI escaping only emits %XX; %uXXXX keeps unmatched code units distinct.
            encoded.Append("%u");
            encoded.Append(((int)value).ToString("X4", CultureInfo.InvariantCulture));
            index++;
            segmentStart = index;
        }

        encoded.Append(Uri.EscapeDataString(userId[segmentStart..]));
        string encodedIdentifier = encoded.ToString();
        _ = new SignalRGroupKey(hubName, encodedIdentifier);
        return AqueductUserGroupNamespace.Prefix +
               Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(encodedIdentifier)));
    }

    private async Task OnAllMessageAsync(
        AllMessage message
    )
    {
        foreach (HubConnectionContext connection in ConnectionRegistry.GetAll())
        {
            if (connection.ConnectionAborted.IsCancellationRequested)
            {
                continue;
            }

            bool isExcluded = message.ExcludedConnectionIds?.Contains(connection.ConnectionId) ?? false;
            if (!isExcluded)
            {
                await MessageSender.SendAsync(connection, message.MethodName, message.Args).ConfigureAwait(false);
            }
        }
    }

    private async Task OnServerMessageAsync(
        ServerMessage message
    )
    {
        HubConnectionContext? connection = ConnectionRegistry.GetConnection(message.ConnectionId);
        if (connection != null)
        {
            await MessageSender.SendAsync(connection, message.MethodName, message.Args).ConfigureAwait(false);
        }
    }
}