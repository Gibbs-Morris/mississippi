using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Aqueduct.Gateway;

/// <summary>
///     Logger extensions for <see cref="StreamSubscriptionManager" />.
/// </summary>
internal static partial class StreamSubscriptionManagerLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Initializing Orleans streams for hub '{HubName}' (serverId: {ServerId})")]
    public static partial void InitializingStreams(
        this ILogger logger,
        string hubName,
        string serverId
    );

    /// <summary>Logs a failed stream initialization with its hub and server context.</summary>
    /// <param name="logger">The manager's logger.</param>
    /// <param name="hubName">The hub being initialized.</param>
    /// <param name="serverId">The gateway server identifier.</param>
    /// <param name="exception">The initialization failure.</param>
    [LoggerMessage(
        EventId = 3,
        EventName = "StreamInitializationFailed",
        Level = LogLevel.Error,
        Message = "Orleans stream initialization failed for hub '{HubName}' (serverId: {ServerId})")]
    public static partial void StreamInitializationFailed(
        this ILogger logger,
        string hubName,
        string serverId,
        Exception exception
    );

    /// <summary>Logs failed compensation while the manager retains subscription ownership.</summary>
    /// <param name="logger">The manager's logger.</param>
    /// <param name="hubName">The hub being initialized.</param>
    /// <param name="serverId">The gateway server identifier.</param>
    /// <param name="exception">The compensation failure.</param>
    [LoggerMessage(
        EventId = 4,
        EventName = "StreamSubscriptionCleanupFailed",
        Level = LogLevel.Error,
        Message =
            "Orleans server subscription cleanup failed for hub '{HubName}' (serverId: {ServerId}); ownership retained for retry")]
    public static partial void StreamSubscriptionCleanupFailed(
        this ILogger logger,
        string hubName,
        string serverId,
        Exception exception
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Orleans streams initialized for hub '{HubName}' (serverId: {ServerId})")]
    public static partial void StreamsInitialized(
        this ILogger logger,
        string hubName,
        string serverId
    );
}