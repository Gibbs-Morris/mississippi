using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.UxProjections.SignalR;

/// <summary>
///     Logger extensions for <see cref="OrleansHubLifetimeManager{THub}" />.
/// </summary>
internal static partial class OrleansHubLifetimeManagerLoggerExtensions
{
    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Adding connection '{ConnectionId}' to group '{GroupName}' on hub '{HubName}'")]
    public static partial void AddingToGroup(
        this ILogger logger,
        string connectionId,
        string groupName,
        string hubName
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Orleans backplane initialized for hub '{HubName}' (serverId: {ServerId})")]
    public static partial void BackplaneInitialized(
        this ILogger logger,
        string hubName,
        string serverId
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Connection '{ConnectionId}' disconnecting from hub '{HubName}'")]
    public static partial void ConnectionDisconnecting(
        this ILogger logger,
        string connectionId,
        string hubName
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Connection '{ConnectionId}' registered with hub '{HubName}' on server '{ServerId}'")]
    public static partial void ConnectionRegistered(
        this ILogger logger,
        string connectionId,
        string hubName,
        string serverId
    );

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Debug,
        Message = "Connection '{ConnectionId}' unregistered from hub '{HubName}'")]
    public static partial void ConnectionUnregistered(
        this ILogger logger,
        string connectionId,
        string hubName
    );

    [LoggerMessage(EventId = 9, Level = LogLevel.Warning, Message = "Heartbeat failed for server '{ServerId}'")]
    public static partial void HeartbeatFailed(
        this ILogger logger,
        string serverId,
        Exception exception
    );

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Initializing Orleans backplane for hub '{HubName}' (serverId: {ServerId})")]
    public static partial void InitializingBackplane(
        this ILogger logger,
        string hubName,
        string serverId
    );

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "Removing connection '{ConnectionId}' from group '{GroupName}' on hub '{HubName}'")]
    public static partial void RemovingFromGroup(
        this ILogger logger,
        string connectionId,
        string groupName,
        string hubName
    );

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Trace,
        Message = "Sending local message to connection '{ConnectionId}' method '{MethodName}' on hub '{HubName}'")]
    public static partial void SendingLocalMessage(
        this ILogger logger,
        string connectionId,
        string methodName,
        string hubName
    );

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Debug,
        Message = "Sending to group '{GroupName}' method '{MethodName}' on hub '{HubName}'")]
    public static partial void SendingToGroup(
        this ILogger logger,
        string groupName,
        string methodName,
        string hubName
    );
}