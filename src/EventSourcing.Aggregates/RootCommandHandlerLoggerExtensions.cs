using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     High-performance logging helpers for <see cref="RootCommandHandler{TState}" />.
/// </summary>
internal static class RootCommandHandlerLoggerExtensions
{
    private static readonly Action<ILogger, string, string, Exception?> RootCommandHandlerHandlingMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new(1, nameof(RootCommandHandlerHandling)),
            "Handling command {CommandType} against state {StateType}");

    private static readonly Action<ILogger, string, string, Exception?> RootCommandHandlerHandlerMatchedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new(2, nameof(RootCommandHandlerHandlerMatched)),
            "Handler {HandlerType} matched command {CommandType}");

    private static readonly Action<ILogger, string, string, Exception?> RootCommandHandlerNoHandlerMatchedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new(3, nameof(RootCommandHandlerNoHandlerMatched)),
            "No handler matched for state {StateType} and command {CommandType}");

    /// <summary>
    ///     Logs that a handler matched and processed the command.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="handlerType">The handler type name.</param>
    /// <param name="commandType">The command type name.</param>
    public static void RootCommandHandlerHandlerMatched(
        this ILogger logger,
        string handlerType,
        string commandType
    ) =>
        RootCommandHandlerHandlerMatchedMessage(logger, handlerType, commandType, null);

    /// <summary>
    ///     Logs that command handling is starting for a state and command combination.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="stateType">The state type name.</param>
    /// <param name="commandType">The command type name.</param>
    public static void RootCommandHandlerHandling(
        this ILogger logger,
        string stateType,
        string commandType
    ) =>
        RootCommandHandlerHandlingMessage(logger, commandType, stateType, null);

    /// <summary>
    ///     Logs when no handler matches an incoming command for a state.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="stateType">The state type name.</param>
    /// <param name="commandType">The command type name.</param>
    public static void RootCommandHandlerNoHandlerMatched(
        this ILogger logger,
        string stateType,
        string commandType
    ) =>
        RootCommandHandlerNoHandlerMatchedMessage(logger, stateType, commandType, null);
}