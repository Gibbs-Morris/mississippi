using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     High-performance logging extensions for aggregate grains.
/// </summary>
internal static class AggregateGrainLoggerExtensions
{
    private static readonly Action<ILogger, string, string, Exception?> CommandExecutedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new(2, nameof(CommandExecuted)),
            "Executed command {CommandType} for aggregate {AggregateKey}");

    private static readonly Action<ILogger, string, string, string, string, Exception?> CommandFailedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new(3, nameof(CommandFailed)),
            "Command {CommandType} failed for aggregate {AggregateKey}: {ErrorCode} - {ErrorMessage}");

    private static readonly Action<ILogger, string, string, Exception?> CommandReceivedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new(1, nameof(CommandReceived)),
            "Received command {CommandType} for aggregate {AggregateKey}");

    private static readonly Action<ILogger, string, long, Exception?> HydratingStateMessage =
        LoggerMessage.Define<string, long>(
            LogLevel.Debug,
            new(4, nameof(HydratingState)),
            "Hydrating state for aggregate {AggregateKey}, replaying from position {Position}");

    /// <summary>
    ///     Logs when a command has been successfully executed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The type of command executed.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    public static void CommandExecuted(
        this ILogger logger,
        string commandType,
        string aggregateKey
    ) =>
        CommandExecutedMessage(logger, commandType, aggregateKey, null);

    /// <summary>
    ///     Logs when a command execution fails.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The type of command that failed.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    public static void CommandFailed(
        this ILogger logger,
        string commandType,
        string aggregateKey,
        string errorCode,
        string errorMessage
    ) =>
        CommandFailedMessage(logger, commandType, aggregateKey, errorCode, errorMessage, null);

    /// <summary>
    ///     Logs when a command is received for processing.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The type of command received.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    public static void CommandReceived(
        this ILogger logger,
        string commandType,
        string aggregateKey
    ) =>
        CommandReceivedMessage(logger, commandType, aggregateKey, null);

    /// <summary>
    ///     Logs when aggregate state hydration begins.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="position">The position to replay from.</param>
    public static void HydratingState(
        this ILogger logger,
        string aggregateKey,
        long position
    ) =>
        HydratingStateMessage(logger, aggregateKey, position, null);
}