using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Reducers;

/// <summary>
///     High-performance logging helpers for <see cref="DelegateReducer{TEvent, TProjection}" />.
/// </summary>
internal static partial class DelegateReducerLoggerExtensions
{
    /// <summary>
    ///     Logs an immutability violation when a delegate reducer returns the incoming projection instance.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="eventType">The event type name.</param>
    [LoggerMessage(
        3,
        LogLevel.Error,
        "Delegate reducer returned the incoming projection instance for projection {ProjectionType} when handling event {EventType}. Reducers must return a new instance (use copy/with expressions). ")]
    public static partial void DelegateReducerProjectionInstanceReused(
        this ILogger logger,
        string projectionType,
        string eventType
    );

    /// <summary>
    ///     Logs successful reduction by a delegate reducer.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="eventType">The event type name.</param>
    [LoggerMessage(2, LogLevel.Debug, "Delegate reducer produced projection {ProjectionType} from event {EventType}")]
    public static partial void DelegateReducerReduced(
        this ILogger logger,
        string projectionType,
        string eventType
    );

    /// <summary>
    ///     Logs that a delegate reducer is starting reduction.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="eventType">The event type name.</param>
    [LoggerMessage(
        1,
        LogLevel.Debug,
        "Reducing projection {ProjectionType} with event {EventType} using delegate reducer")]
    public static partial void DelegateReducerReducing(
        this ILogger logger,
        string projectionType,
        string eventType
    );
}