using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Projections.Reducers;

/// <summary>
///     High-performance logging extensions for <see cref="RootReducer{TModel}" /> operations.
/// </summary>
internal static class RootReducerLoggerExtensions
{
    private static readonly Action<ILogger, string, int, Exception?> NullDomainEventMessage =
        LoggerMessage.Define<string, int>(
            LogLevel.Warning,
            new(1001, nameof(ReducerReceivedNullEvent)),
            "RootReducer skipped reduction for model {ModelType} because the domain event was null. Registered reducers: {ReducerCount}.");

    private static readonly Action<ILogger, string, string, Exception?> NoReducersRegisteredMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new(1002, nameof(NoReducersRegistered)),
            "RootReducer found no reducers for model {ModelType}; event {EventType} will be ignored.");

    private static readonly Action<ILogger, string, string, int, Exception?> ReducerExecutionStartingMessage =
        LoggerMessage.Define<string, string, int>(
            LogLevel.Debug,
            new(1003, nameof(ReducerExecutionStarting)),
            "RootReducer reducing model {ModelType} with event {EventType} using {ReducerCount} reducers.");

    private static readonly Action<ILogger, string, string, int, long, Exception?> ReducerExecutionCompletedMessage =
        LoggerMessage.Define<string, string, int, long>(
            LogLevel.Debug,
            new(1004, nameof(ReducerExecutionCompleted)),
            "RootReducer completed reduction for model {ModelType} with event {EventType} using {ReducerCount} reducers in {ElapsedMilliseconds} ms.");

    private static readonly Action<ILogger, string, string, string, Exception?> ReducerInvocationStartingMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Trace,
            new(1005, nameof(ReducerInvocationStarting)),
            "RootReducer invoking reducer {ReducerType} for model {ModelType} and event {EventType}.");

    private static readonly Action<ILogger, string, string, string, Exception?> ReducerInvocationCompletedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Trace,
            new(1006, nameof(ReducerInvocationCompleted)),
            "RootReducer completed reducer {ReducerType} for model {ModelType} and event {EventType}.");

    private static readonly Action<ILogger, string, string, string, Exception> ReducerInvocationFailedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new(1007, nameof(ReducerInvocationFailed)),
            "RootReducer reducer {ReducerType} threw while processing model {ModelType} with event {EventType}.");

    /// <summary>
    ///     Logs that no reducers were registered for the provided model type.
    /// </summary>
    /// <typeparam name="TModel">The model type being reduced.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="modelType">The runtime model type.</param>
    /// <param name="eventType">The runtime event type.</param>
    public static void NoReducersRegistered<TModel>(
        this ILogger<RootReducer<TModel>> logger,
        string modelType,
        string eventType
    )
        where TModel : notnull, new()
    {
        NoReducersRegisteredMessage(logger, modelType, eventType, null);
    }

    /// <summary>
    ///     Logs that the reducer pipeline has completed.
    /// </summary>
    /// <typeparam name="TModel">The model type being reduced.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="modelType">The runtime model type.</param>
    /// <param name="eventType">The runtime event type.</param>
    /// <param name="reducerCount">The number of reducers executed.</param>
    /// <param name="elapsedMilliseconds">The total execution time.</param>
    public static void ReducerExecutionCompleted<TModel>(
        this ILogger<RootReducer<TModel>> logger,
        string modelType,
        string eventType,
        int reducerCount,
        long elapsedMilliseconds
    )
        where TModel : notnull, new()
    {
        ReducerExecutionCompletedMessage(logger, modelType, eventType, reducerCount, elapsedMilliseconds, null);
    }

    /// <summary>
    ///     Logs that the reducer pipeline is starting.
    /// </summary>
    /// <typeparam name="TModel">The model type being reduced.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="modelType">The runtime model type.</param>
    /// <param name="eventType">The runtime event type.</param>
    /// <param name="reducerCount">The number of reducers that will run.</param>
    public static void ReducerExecutionStarting<TModel>(
        this ILogger<RootReducer<TModel>> logger,
        string modelType,
        string eventType,
        int reducerCount
    )
        where TModel : notnull, new()
    {
        ReducerExecutionStartingMessage(logger, modelType, eventType, reducerCount, null);
    }

    /// <summary>
    ///     Logs that an individual reducer finished without throwing.
    /// </summary>
    /// <typeparam name="TModel">The model type being reduced.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="modelType">The runtime model type.</param>
    /// <param name="eventType">The runtime event type.</param>
    /// <param name="reducerType">The reducer type being invoked.</param>
    public static void ReducerInvocationCompleted<TModel>(
        this ILogger<RootReducer<TModel>> logger,
        string modelType,
        string eventType,
        string reducerType
    )
        where TModel : notnull, new()
    {
        ReducerInvocationCompletedMessage(logger, modelType, eventType, reducerType, null);
    }

    /// <summary>
    ///     Logs that an individual reducer threw an exception.
    /// </summary>
    /// <typeparam name="TModel">The model type being reduced.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="modelType">The runtime model type.</param>
    /// <param name="eventType">The runtime event type.</param>
    /// <param name="reducerType">The reducer type being invoked.</param>
    /// <param name="exception">The exception thrown by the reducer.</param>
    public static void ReducerInvocationFailed<TModel>(
        this ILogger<RootReducer<TModel>> logger,
        string modelType,
        string eventType,
        string reducerType,
        Exception exception
    )
        where TModel : notnull, new()
    {
        ReducerInvocationFailedMessage(logger, modelType, eventType, reducerType, exception);
    }

    /// <summary>
    ///     Logs that an individual reducer is about to run.
    /// </summary>
    /// <typeparam name="TModel">The model type being reduced.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="modelType">The runtime model type.</param>
    /// <param name="eventType">The runtime event type.</param>
    /// <param name="reducerType">The reducer type being invoked.</param>
    public static void ReducerInvocationStarting<TModel>(
        this ILogger<RootReducer<TModel>> logger,
        string modelType,
        string eventType,
        string reducerType
    )
        where TModel : notnull, new()
    {
        ReducerInvocationStartingMessage(logger, modelType, eventType, reducerType, null);
    }

    /// <summary>
    ///     Logs that a null domain event was provided to the reducer pipeline.
    /// </summary>
    /// <typeparam name="TModel">The model type being reduced.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="modelType">The runtime model type.</param>
    /// <param name="reducerCount">The number of registered reducers.</param>
    public static void ReducerReceivedNullEvent<TModel>(
        this ILogger<RootReducer<TModel>> logger,
        string modelType,
        int reducerCount
    )
        where TModel : notnull, new()
    {
        NullDomainEventMessage(logger, modelType, reducerCount, null);
    }
}