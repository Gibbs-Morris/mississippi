using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Aqueduct;

/// <summary>
///     Logger extensions for <see cref="HeartbeatManager" />.
/// </summary>
internal static partial class HeartbeatManagerLoggerExtensions
{
    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Heartbeat failed for server '{ServerId}'")]
    public static partial void HeartbeatFailed(
        this ILogger logger,
        string serverId,
        Exception exception
    );

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Heartbeat manager started for server '{ServerId}'")]
    public static partial void HeartbeatStarted(
        this ILogger logger,
        string serverId
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Heartbeat manager stopped for server '{ServerId}'")]
    public static partial void HeartbeatStopped(
        this ILogger logger,
        string serverId
    );
}