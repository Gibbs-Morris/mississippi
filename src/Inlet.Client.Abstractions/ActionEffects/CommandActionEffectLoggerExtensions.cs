using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Inlet.Client.Abstractions.ActionEffects;

/// <summary>
///     Logs response-decoding failures without recording command inputs or response bodies.
/// </summary>
internal static partial class CommandActionEffectLoggerExtensions
{
    /// <summary>
    ///     Logs the original exception with safe command and response context.
    /// </summary>
    /// <param name="logger">The command action logger.</param>
    /// <param name="exception">The original response-decoding exception.</param>
    /// <param name="commandId">The lifecycle correlation identifier.</param>
    /// <param name="commandType">The command action type.</param>
    /// <param name="statusCode">The HTTP response status.</param>
    [LoggerMessage(
        EventId = 1,
        EventName = "InvalidCommandResponse",
        Level = LogLevel.Warning,
        Message = "Command {CommandId} ({CommandType}) received an invalid response with HTTP status {StatusCode}.")]
    public static partial void InvalidCommandResponse(
        this ILogger logger,
        Exception exception,
        string commandId,
        string commandType,
        int statusCode
    );
}