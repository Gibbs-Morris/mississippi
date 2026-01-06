using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.AspNetCore.SignalR.Orleans.Grains;

/// <summary>
///     Logger extensions for <see cref="SignalRServerDirectoryGrain" />.
/// </summary>
internal static partial class SignalRServerDirectoryGrainLoggerExtensions
{
    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Warning,
        Message = "Found {DeadServerCount} dead servers (timeout: {Timeout})")]
    public static partial void DeadServersFound(
        this ILogger logger,
        int deadServerCount,
        TimeSpan timeout
    );

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Heartbeat from unknown server '{ServerId}'")]
    public static partial void HeartbeatFromUnknownServer(
        this ILogger logger,
        string serverId
    );

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Registering server '{ServerId}'")]
    public static partial void RegisteringServer(
        this ILogger logger,
        string serverId
    );

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Server directory grain activated with {ServerCount} servers")]
    public static partial void ServerDirectoryActivated(
        this ILogger logger,
        int serverCount
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Server '{ServerId}' heartbeat with {ConnectionCount} connections")]
    public static partial void ServerHeartbeat(
        this ILogger logger,
        string serverId,
        int connectionCount
    );

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Server '{ServerId}' not found in directory")]
    public static partial void ServerNotFound(
        this ILogger logger,
        string serverId
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Server '{ServerId}' registered (now {ServerCount} servers)")]
    public static partial void ServerRegistered(
        this ILogger logger,
        string serverId,
        int serverCount
    );

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "Server '{ServerId}' unregistered (now {ServerCount} servers)")]
    public static partial void ServerUnregistered(
        this ILogger logger,
        string serverId,
        int serverCount
    );

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Unregistering server '{ServerId}'")]
    public static partial void UnregisteringServer(
        this ILogger logger,
        string serverId
    );
}