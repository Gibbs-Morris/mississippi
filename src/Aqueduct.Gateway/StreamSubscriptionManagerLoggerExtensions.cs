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

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Orleans streams initialized for hub '{HubName}' (serverId: {ServerId})")]
    public static partial void StreamsInitialized(
        this ILogger logger,
        string hubName,
        string serverId
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Orleans stream subscription was canceled before a handle was created (serverId: {ServerId})")]
    public static partial void SubscriptionCleanupCanceled(
        this ILogger logger,
        string serverId,
        Exception exception
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Failed to clean up canceled Orleans stream subscription (serverId: {ServerId})")]
    public static partial void SubscriptionCleanupFailed(
        this ILogger logger,
        string serverId,
        Exception exception
    );
}