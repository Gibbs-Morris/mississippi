using Microsoft.Extensions.Logging;

namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     High-performance logging extensions for aggregate grains.
/// </summary>
internal static partial class AggregateGrainLoggerExtensions
{
    /// <summary>
    ///     Logs when a command is received for processing.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The type of command received.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(1, LogLevel.Debug, "Received command {CommandType} for aggregate {AggregateKey}")]
    public static partial void CommandReceived(
        this ILogger logger,
        string commandType,
        string aggregateKey);

    /// <summary>
    ///     Logs when a command has been successfully executed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The type of command executed.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(2, LogLevel.Debug, "Executed command {CommandType} for aggregate {AggregateKey}")]
    public static partial void CommandExecuted(
        this ILogger logger,
        string commandType,
        string aggregateKey);

    /// <summary>
    ///     Logs when a command execution fails.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The type of command that failed.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    [LoggerMessage(3, LogLevel.Warning, "Command {CommandType} failed for aggregate {AggregateKey}: {ErrorCode} - {ErrorMessage}")]
    public static partial void CommandFailed(
        this ILogger logger,
        string commandType,
        string aggregateKey,
        string errorCode,
        string errorMessage);

    /// <summary>
    ///     Logs when aggregate state hydration begins.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="position">The position to replay from.</param>
    [LoggerMessage(4, LogLevel.Debug, "Hydrating state for aggregate {AggregateKey}, replaying from position {Position}")]
    public static partial void HydratingState(
        this ILogger logger,
        string aggregateKey,
        long position);
}