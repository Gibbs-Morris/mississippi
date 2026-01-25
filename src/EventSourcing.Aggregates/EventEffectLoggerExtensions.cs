using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     High-performance logging extensions for event effect execution.
/// </summary>
internal static partial class EventEffectLoggerExtensions
{
    /// <summary>
    ///     Logs when an effect completes.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="eventType">The event type name.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    [LoggerMessage(
        101,
        LogLevel.Debug,
        "Effect {EffectType} completed for event {EventType} on aggregate {AggregateKey} in {DurationMs}ms")]
    public static partial void EffectCompleted(
        this ILogger logger,
        string effectType,
        string eventType,
        string aggregateKey,
        long durationMs
    );

    /// <summary>
    ///     Logs when an effect fails.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="eventType">The event type name.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(102, LogLevel.Error, "Effect {EffectType} failed for event {EventType} on aggregate {AggregateKey}")]
    public static partial void EffectFailed(
        this ILogger logger,
        Exception exception,
        string effectType,
        string eventType,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when effect iteration limit is reached.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="maxIterations">The maximum iterations allowed.</param>
    [LoggerMessage(
        100,
        LogLevel.Warning,
        "Effect iteration limit ({MaxIterations}) reached for aggregate {AggregateKey}. " +
        "Possible infinite loop in effect chain. Remaining yielded events will not be processed.")]
    public static partial void EffectIterationLimitReached(
        this ILogger logger,
        string aggregateKey,
        int maxIterations
    );

    /// <summary>
    ///     Logs when an effect is slow (over 1 second).
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(
        103,
        LogLevel.Warning,
        "Effect {EffectType} took {DurationMs}ms (>1000ms) for aggregate {AggregateKey}. " +
        "Consider optimizing or using background processing.")]
    public static partial void EffectSlow(
        this ILogger logger,
        string effectType,
        long durationMs,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when an effect starts.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="eventType">The event type name.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(
        104,
        LogLevel.Debug,
        "Effect {EffectType} starting for event {EventType} on aggregate {AggregateKey}")]
    public static partial void EffectStarting(
        this ILogger logger,
        string effectType,
        string eventType,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when an effect yields an event.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="yieldedEventType">The yielded event type name.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(
        105,
        LogLevel.Debug,
        "Effect {EffectType} yielded event {YieldedEventType} for aggregate {AggregateKey}")]
    public static partial void EffectYieldedEvent(
        this ILogger logger,
        string effectType,
        string yieldedEventType,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when the root event effect dispatcher is dispatching an event.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="eventType">The event type name.</param>
    [LoggerMessage(
        106,
        LogLevel.Debug,
        "RootEventEffect dispatching event {EventType} for aggregate type {AggregateType}")]
    public static partial void RootEventEffectDispatching(
        this ILogger logger,
        string aggregateType,
        string eventType
    );

    /// <summary>
    ///     Logs when an event effect fails during enumeration.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="eventType">The event type name.</param>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="exception">The exception that occurred.</param>
    [LoggerMessage(
        107,
        LogLevel.Error,
        "Event effect {EffectType} failed while processing event {EventType} for aggregate {AggregateType}. " +
        "Continuing with remaining effects.")]
    public static partial void EventEffectFailed(
        this ILogger logger,
        string effectType,
        string eventType,
        string aggregateType,
        Exception exception
    );
}