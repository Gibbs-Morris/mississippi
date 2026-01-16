using Microsoft.Extensions.Logging;


namespace Mississippi.Aqueduct;

/// <summary>
///     Logger extensions for <see cref="LocalMessageSender" />.
/// </summary>
internal static partial class LocalMessageSenderLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Trace,
        Message = "Sending local message to connection '{ConnectionId}' method '{MethodName}'")]
    public static partial void SendingLocalMessage(
        this ILogger logger,
        string connectionId,
        string methodName
    );
}