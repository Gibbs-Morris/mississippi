using Microsoft.Extensions.Logging;


namespace Mississippi.Aqueduct;

/// <summary>
///     High-performance logger extensions for <see cref="AqueductNotifier" />.
/// </summary>
internal static partial class AqueductNotifierLoggerExtensions
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Notifier sending message '{Method}' to all clients for hub '{HubName}'")]
    internal static partial void NotifierSendingToAll(
        this ILogger logger,
        string hubName,
        string method
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Notifier sending message '{Method}' to connection '{ConnectionId}' for hub '{HubName}'")]
    internal static partial void NotifierSendingToConnection(
        this ILogger logger,
        string connectionId,
        string hubName,
        string method
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Notifier sending message '{Method}' to group '{GroupName}' for hub '{HubName}'")]
    internal static partial void NotifierSendingToGroup(
        this ILogger logger,
        string groupName,
        string hubName,
        string method
    );
}