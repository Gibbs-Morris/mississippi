using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Reducers;

/// <summary>
///     High-performance logging helpers for <see cref="RootReducer{TProjection}" />.
/// </summary>
internal static class RootReducerLoggerExtensions
{
    private static readonly Action<ILogger, string, string, Exception?> RootReducerReducingMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new(1, nameof(RootReducerReducing)),
            "Reducing projection {ProjectionType} with event {EventType}");

    private static readonly Action<ILogger, string, string, Exception?> RootReducerReducerMatchedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new(2, nameof(RootReducerReducerMatched)),
            "Reducer {ReducerType} applied event {EventType}");

    private static readonly Action<ILogger, string, string, Exception?> RootReducerNoReducerMatchedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new(3, nameof(RootReducerNoReducerMatched)),
            "No reducer matched for projection {ProjectionType} and event {EventType}; returning unchanged state");

    private static readonly Action<ILogger, string, string, string, Exception>
        RootReducerProjectionInstanceReusedMessage = LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new(4, nameof(RootReducerProjectionInstanceReused)),
            "Reducer {ReducerType} returned the incoming projection instance for projection {ProjectionType} when handling event {EventType}. Reducers must return a new instance (use copy/with expressions). ");

    /// <summary>
    ///     Logs when no reducer matches an incoming event for a projection.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="eventType">The event type name.</param>
    public static void RootReducerNoReducerMatched(
        this ILogger logger,
        string projectionType,
        string eventType
    ) =>
        RootReducerNoReducerMatchedMessage(logger, projectionType, eventType, null);

    /// <summary>
    ///     Logs an immutability violation when a reducer returns the incoming projection instance.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="reducerType">The reducer type name.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="eventType">The event type name.</param>
    public static void RootReducerProjectionInstanceReused(
        this ILogger logger,
        string reducerType,
        string projectionType,
        string eventType
    ) =>
        RootReducerProjectionInstanceReusedMessage(logger, reducerType, projectionType, eventType, null!);

    /// <summary>
    ///     Logs that a reducer matched and is being applied.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="reducerType">The reducer type name.</param>
    /// <param name="eventType">The event type name.</param>
    public static void RootReducerReducerMatched(
        this ILogger logger,
        string reducerType,
        string eventType
    ) =>
        RootReducerReducerMatchedMessage(logger, reducerType, eventType, null);

    /// <summary>
    ///     Logs that reduction is starting for a projection and event combination.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="eventType">The event type name.</param>
    public static void RootReducerReducing(
        this ILogger logger,
        string projectionType,
        string eventType
    ) =>
        RootReducerReducingMessage(logger, projectionType, eventType, null);
}