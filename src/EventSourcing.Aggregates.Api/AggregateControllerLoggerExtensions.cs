using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Aggregates.Api;

/// <summary>
///     High-performance logging extensions for aggregate controller operations.
/// </summary>
internal static partial class AggregateControllerLoggerExtensions
{
    /// <summary>
    ///     Logs when a command execution throws an exception.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Command '{CommandType}' on entity '{EntityId}' of type {AggregateType} threw exception")]
    public static partial void CommandException(
        this ILogger logger,
        string entityId,
        string commandType,
        Exception exception,
        string aggregateType
    );

    /// <summary>
    ///     Logs when a command execution fails.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Command '{CommandType}' on entity '{EntityId}' of type {AggregateType} failed: {ErrorMessage}")]
    public static partial void CommandFailed(
        this ILogger logger,
        string entityId,
        string commandType,
        string errorMessage,
        string aggregateType
    );

    /// <summary>
    ///     Logs when a command execution succeeds.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Command '{CommandType}' on entity '{EntityId}' of type {AggregateType} succeeded")]
    public static partial void CommandSucceeded(
        this ILogger logger,
        string entityId,
        string commandType,
        string aggregateType
    );

    /// <summary>
    ///     Logs when a command is about to be executed.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Executing command '{CommandType}' on entity '{EntityId}' of type {AggregateType}")]
    public static partial void ExecutingCommand(
        this ILogger logger,
        string entityId,
        string commandType,
        string aggregateType
    );
}