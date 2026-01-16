using Microsoft.Extensions.Logging;


namespace Mississippi.Aqueduct;

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
}
