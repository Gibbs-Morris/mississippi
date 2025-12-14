using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Reducers;

/// <summary>
///     High-performance logging helpers for <see cref="DelegateReducer{TEvent, TProjection}" />.
/// </summary>
internal static class DelegateReducerLoggerExtensions
{
    private static readonly Action<ILogger, string, string, Exception?> DelegateReducerReducingMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new(1, nameof(DelegateReducerReducing)),
            "Reducing projection {ProjectionType} with event {EventType} using delegate reducer");

    private static readonly Action<ILogger, string, string, Exception?> DelegateReducerReducedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new(2, nameof(DelegateReducerReduced)),
            "Delegate reducer produced projection {ProjectionType} from event {EventType}");

    private static readonly Action<ILogger, string, string, Exception> DelegateReducerProjectionInstanceReusedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new(3, nameof(DelegateReducerProjectionInstanceReused)),
            "Delegate reducer returned the incoming projection instance for projection {ProjectionType} when handling event {EventType}. Reducers must return a new instance (use copy/with expressions). ");

    /// <summary>
    ///     Logs an immutability violation when a delegate reducer returns the incoming projection instance.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="eventType">The event type name.</param>
    public static void DelegateReducerProjectionInstanceReused(
        this ILogger logger,
        string projectionType,
        string eventType
    ) =>
        DelegateReducerProjectionInstanceReusedMessage(logger, projectionType, eventType, null!);

    /// <summary>
    ///     Logs successful reduction by a delegate reducer.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="eventType">The event type name.</param>
    public static void DelegateReducerReduced(
        this ILogger logger,
        string projectionType,
        string eventType
    ) =>
        DelegateReducerReducedMessage(logger, projectionType, eventType, null);

    /// <summary>
    ///     Logs that a delegate reducer is starting reduction.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="eventType">The event type name.</param>
    public static void DelegateReducerReducing(
        this ILogger logger,
        string projectionType,
        string eventType
    ) =>
        DelegateReducerReducingMessage(logger, projectionType, eventType, null);
}