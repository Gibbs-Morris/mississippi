using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Reducers;

/// <summary>
///     High-performance logging helpers for <see cref="RootReducer{TProjection}" />.
/// </summary>
internal static partial class RootReducerLoggerExtensions
{
    /// <summary>
    ///     Logs when no event reducer matches an incoming event for a projection.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="eventType">The event type name.</param>
    [LoggerMessage(
        3,
        LogLevel.Debug,
        "No event reducer matched for projection {ProjectionType} and event {EventType}; returning unchanged state")]
    public static partial void RootReducerNoReducerMatched(
        this ILogger logger,
        string projectionType,
        string eventType
    );

    /// <summary>
    ///     Logs an immutability violation when an event reducer returns the incoming projection instance.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="reducerType">The event reducer type name.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="eventType">The event type name.</param>
    [LoggerMessage(
        4,
        LogLevel.Error,
        "Reducer {ReducerType} returned the incoming projection instance for projection {ProjectionType} when handling event {EventType}. Reducers must return a new instance (use copy/with expressions). ")]
    public static partial void RootReducerProjectionInstanceReused(
        this ILogger logger,
        string reducerType,
        string projectionType,
        string eventType
    );

    /// <summary>
    ///     Logs that an event reducer matched and is being applied.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="reducerType">The event reducer type name.</param>
    /// <param name="eventType">The event type name.</param>
    [LoggerMessage(2, LogLevel.Debug, "Reducer {ReducerType} applied event {EventType}")]
    public static partial void RootReducerReducerMatched(
        this ILogger logger,
        string reducerType,
        string eventType
    );

    /// <summary>
    ///     Logs that reduction is starting for a projection and event combination.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="eventType">The event type name.</param>
    [LoggerMessage(1, LogLevel.Debug, "Reducing projection {ProjectionType} with event {EventType}")]
    public static partial void RootReducerReducing(
        this ILogger logger,
        string projectionType,
        string eventType
    );
}