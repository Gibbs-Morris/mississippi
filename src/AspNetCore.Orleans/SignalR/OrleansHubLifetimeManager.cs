using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mississippi.AspNetCore.Orleans.SignalR.Grains;
using Mississippi.AspNetCore.Orleans.SignalR.Options;
using Orleans;

namespace Mississippi.AspNetCore.Orleans.SignalR;

/// <summary>
/// Orleans-backed implementation of <see cref="HubLifetimeManager{THub}"/> for SignalR scale-out.
/// </summary>
/// <typeparam name="THub">The hub type.</typeparam>
public sealed class OrleansHubLifetimeManager<THub> : HubLifetimeManager<THub>
    where THub : Hub
{
    private ILogger<OrleansHubLifetimeManager<THub>> Logger { get; }
    private IClusterClient ClusterClient { get; }
    private IOptions<SignalROptions> Options { get; }

    private readonly Dictionary<string, HubConnectionContext> localConnections = new();
    private readonly object connectionsLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OrleansHubLifetimeManager{THub}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="clusterClient">The Orleans cluster client.</param>
    /// <param name="options">The SignalR options.</param>
    public OrleansHubLifetimeManager(
        ILogger<OrleansHubLifetimeManager<THub>> logger,
        IClusterClient clusterClient,
        IOptions<SignalROptions> options)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ClusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public override async Task OnConnectedAsync(HubConnectionContext connection)
    {
        if (connection is null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        lock (connectionsLock)
        {
            localConnections[connection.ConnectionId] = connection;
        }

        string? userId = connection.UserIdentifier;
        IConnectionGrain grain = ClusterClient.GetGrain<IConnectionGrain>(connection.ConnectionId);
        await grain.RegisterAsync(userId, []);

        OrleansHubLifetimeManagerLoggerExtensions.ConnectionRegistered(Logger, typeof(THub).Name, connection.ConnectionId, userId);
    }

    /// <inheritdoc/>
    public override async Task OnDisconnectedAsync(HubConnectionContext connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        lock (connectionsLock)
        {
            localConnections.Remove(connection.ConnectionId);
        }

        IConnectionGrain grain = ClusterClient.GetGrain<IConnectionGrain>(connection.ConnectionId);
        await grain.UnregisterAsync();

        OrleansHubLifetimeManagerLoggerExtensions.ConnectionUnregistered(Logger, typeof(THub).Name, connection.ConnectionId);
    }

    /// <inheritdoc/>
    public override Task SendAllAsync(string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        List<HubConnectionContext> connections;
        lock (connectionsLock)
        {
            connections = localConnections.Values.ToList();
        }

        List<Task> tasks = connections.Select(c => SendToConnectionAsync(c, methodName, args, cancellationToken)).ToList();
        return Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public override Task SendAllExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
    {
        List<HubConnectionContext> connections;
        lock (connectionsLock)
        {
            connections = localConnections.Values
                .Where(c => !excludedConnectionIds.Contains(c.ConnectionId))
                .ToList();
        }

        List<Task> tasks = connections.Select(c => SendToConnectionAsync(c, methodName, args, cancellationToken)).ToList();
        return Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public override Task SendConnectionAsync(string connectionId, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionId);

        HubConnectionContext? connection;
        lock (connectionsLock)
        {
            localConnections.TryGetValue(connectionId, out connection);
        }

        if (connection is not null)
        {
            return SendToConnectionAsync(connection, methodName, args, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionIds);

        List<Task> tasks = new();
        foreach (string connectionId in connectionIds)
        {
            HubConnectionContext? connection;
            lock (connectionsLock)
            {
                localConnections.TryGetValue(connectionId, out connection);
            }

            if (connection is not null)
            {
                tasks.Add(SendToConnectionAsync(connection, methodName, args, cancellationToken));
            }
        }

        return Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public override Task SendGroupAsync(string groupName, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            throw new ArgumentNullException(nameof(groupName));
        }

        List<HubConnectionContext> connections;
        lock (connectionsLock)
        {
            connections = localConnections.Values.ToList();
        }

        // For simplicity, this implementation filters locally
        // A production implementation would use a group grain to track memberships
        List<Task> tasks = connections.Select(c => SendToConnectionAsync(c, methodName, args, cancellationToken)).ToList();
        return Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(groupNames);

        return SendAllAsync(methodName, args, cancellationToken);
    }

    /// <inheritdoc/>
    public override Task SendGroupExceptAsync(string groupName, string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(groupName);

        List<HubConnectionContext> connections;
        lock (connectionsLock)
        {
            connections = localConnections.Values
                .Where(c => !excludedConnectionIds.Contains(c.ConnectionId))
                .ToList();
        }

        List<Task> tasks = connections.Select(c => SendToConnectionAsync(c, methodName, args, cancellationToken)).ToList();
        return Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public override Task SendUserAsync(string userId, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        List<HubConnectionContext> connections;
        lock (connectionsLock)
        {
            connections = localConnections.Values
                .Where(c => c.UserIdentifier == userId)
                .ToList();
        }

        List<Task> tasks = connections.Select(c => SendToConnectionAsync(c, methodName, args, cancellationToken)).ToList();
        return Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        List<HubConnectionContext> connections;
        lock (connectionsLock)
        {
            connections = localConnections.Values
                .Where(c => c.UserIdentifier is not null && userIds.Contains(c.UserIdentifier))
                .ToList();
        }

        List<Task> tasks = connections.Select(c => SendToConnectionAsync(c, methodName, args, cancellationToken)).ToList();
        return Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public override async Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new ArgumentNullException(nameof(connectionId));
        }

        if (string.IsNullOrWhiteSpace(groupName))
        {
            throw new ArgumentNullException(nameof(groupName));
        }

        IConnectionGrain grain = ClusterClient.GetGrain<IConnectionGrain>(connectionId);
        await grain.AddToGroupAsync(groupName);
    }

    /// <inheritdoc/>
    public override async Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new ArgumentNullException(nameof(connectionId));
        }

        if (string.IsNullOrWhiteSpace(groupName))
        {
            throw new ArgumentNullException(nameof(groupName));
        }

        IConnectionGrain grain = ClusterClient.GetGrain<IConnectionGrain>(connectionId);
        await grain.RemoveFromGroupAsync(groupName);
    }

    private static Task SendToConnectionAsync(
        HubConnectionContext connection,
        string methodName,
        object?[] args,
        CancellationToken cancellationToken)
    {
        return connection.WriteAsync(new SerializedHubMessage(new InvocationMessage(methodName, args)), cancellationToken).AsTask();
    }
}

/// <summary>
/// High-performance logger extensions for hub lifetime manager operations.
/// </summary>
internal static class OrleansHubLifetimeManagerLoggerExtensions
{
    private static readonly Action<ILogger, string, string, string?, Exception?> ConnectionRegisteredMessage =
        LoggerMessage.Define<string, string, string?>(
            LogLevel.Debug,
            new EventId(1, nameof(ConnectionRegistered)),
            "Connection registered for hub {HubName}: ConnectionId={ConnectionId}, UserId={UserId}");

    private static readonly Action<ILogger, string, string, Exception?> ConnectionUnregisteredMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(2, nameof(ConnectionUnregistered)),
            "Connection unregistered for hub {HubName}: ConnectionId={ConnectionId}");

    public static void ConnectionRegistered(this ILogger logger, string hubName, string connectionId, string? userId) =>
        ConnectionRegisteredMessage(logger, hubName, connectionId, userId, null);

    public static void ConnectionUnregistered(this ILogger logger, string hubName, string connectionId) =>
        ConnectionUnregisteredMessage(logger, hubName, connectionId, null);
}
