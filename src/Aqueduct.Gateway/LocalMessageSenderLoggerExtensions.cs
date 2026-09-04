using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Aqueduct.Gateway;

/// <summary>
///     Logger extensions for <see cref="LocalMessageSender" />.
/// </summary>
internal static partial class LocalMessageSenderLoggerExtensions
{
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Trace,
        Message = "Local message to connection '{ConnectionId}' method '{MethodName}' canceled after connection abort")]
    public static partial void LocalMessageCanceled(
        this ILogger logger,
        string connectionId,
        string methodName,
        Exception exception
    );

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