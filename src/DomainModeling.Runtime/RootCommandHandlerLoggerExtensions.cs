using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     High-performance logging helpers for <see cref="RootCommandHandler{TSnapshot}" />.
/// </summary>
internal static partial class RootCommandHandlerLoggerExtensions
{
    /// <summary>
    ///     Logs that a handler matched and processed the command.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="handlerType">The handler type name.</param>
    /// <param name="commandType">The command type name.</param>
    [LoggerMessage(2, LogLevel.Debug, "Handler {HandlerType} matched command {CommandType}")]
    public static partial void RootCommandHandlerHandlerMatched(
        this ILogger logger,
        string handlerType,
        string commandType
    );

    /// <summary>
    ///     Logs that command handling is starting for a state and command combination.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="commandType">The command type name.</param>
    /// <param name="stateType">The state type name.</param>
    [LoggerMessage(1, LogLevel.Debug, "Handling command {CommandType} against state {StateType}")]
    public static partial void RootCommandHandlerHandling(
        this ILogger logger,
        string commandType,
        string stateType
    );

    /// <summary>
    ///     Logs when no handler matches an incoming command for a state.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="stateType">The state type name.</param>
    /// <param name="commandType">The command type name.</param>
    [LoggerMessage(3, LogLevel.Debug, "No handler matched for state {StateType} and command {CommandType}")]
    public static partial void RootCommandHandlerNoHandlerMatched(
        this ILogger logger,
        string stateType,
        string commandType
    );
}