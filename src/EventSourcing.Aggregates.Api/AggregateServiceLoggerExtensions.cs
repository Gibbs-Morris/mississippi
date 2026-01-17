using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Aggregates.Api;

/// <summary>
///     High-performance logging extensions for <see cref="AggregateServiceBase{TAggregate}" />.
/// </summary>
internal static partial class AggregateServiceLoggerExtensions
{
    /// <summary>
    ///     Logs that a command failed at the service layer.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="commandType">The command type name.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="aggregateType">The aggregate type name.</param>
    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Warning,
        Message = "Service command {CommandType} failed for {AggregateType} entity {EntityId}: {ErrorMessage}")]
    public static partial void ServiceCommandFailed(
        this ILogger logger,
        string entityId,
        string commandType,
        string errorMessage,
        string aggregateType
    );

    /// <summary>
    ///     Logs that a command succeeded at the service layer.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="commandType">The command type name.</param>
    /// <param name="aggregateType">The aggregate type name.</param>
    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Information,
        Message = "Service command {CommandType} succeeded for {AggregateType} entity {EntityId}")]
    public static partial void ServiceCommandSucceeded(
        this ILogger logger,
        string entityId,
        string commandType,
        string aggregateType
    );

    /// <summary>
    ///     Logs that a command is being executed at the service layer.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="commandType">The command type name.</param>
    /// <param name="aggregateType">The aggregate type name.</param>
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Debug,
        Message = "Service executing command {CommandType} for {AggregateType} entity {EntityId}")]
    public static partial void ServiceExecutingCommand(
        this ILogger logger,
        string entityId,
        string commandType,
        string aggregateType
    );
}