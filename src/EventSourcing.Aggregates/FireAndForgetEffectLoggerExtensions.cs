using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     High-performance logging extensions for fire-and-forget effect execution.
/// </summary>
internal static partial class FireAndForgetEffectLoggerExtensions
{
    /// <summary>
    ///     Logs when effect envelope is missing aggregate state.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="brookKey">The brook key.</param>
    [LoggerMessage(
        203,
        LogLevel.Error,
        "Fire-and-forget effect {EffectType} envelope missing aggregate state for brook {BrookKey}")]
    public static partial void EffectEnvelopeMissingAggregateState(
        this ILogger logger,
        string effectType,
        string brookKey
    );

    /// <summary>
    ///     Logs when effect envelope is missing event data.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="brookKey">The brook key.</param>
    [LoggerMessage(
        202,
        LogLevel.Error,
        "Fire-and-forget effect {EffectType} envelope missing event data for brook {BrookKey}")]
    public static partial void EffectEnvelopeMissingEventData(
        this ILogger logger,
        string effectType,
        string brookKey
    );

    /// <summary>
    ///     Logs when an effect cannot be found in DI.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="aggregateType">The aggregate type name.</param>
    [LoggerMessage(
        201,
        LogLevel.Error,
        "Fire-and-forget effect {EffectType} not found in DI for aggregate {AggregateType}")]
    public static partial void EffectNotFound(
        this ILogger logger,
        string effectType,
        string aggregateType
    );

    /// <summary>
    ///     Logs when a fire-and-forget effect is being executed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="eventType">The event type name.</param>
    /// <param name="brookKey">The brook key.</param>
    [LoggerMessage(
        200,
        LogLevel.Debug,
        "Executing fire-and-forget effect {EffectType} for event {EventType} on brook {BrookKey}")]
    public static partial void ExecutingFireAndForgetEffect(
        this ILogger logger,
        string effectType,
        string eventType,
        string brookKey
    );

    /// <summary>
    ///     Logs when a fire-and-forget effect completes successfully.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="eventType">The event type name.</param>
    /// <param name="brookKey">The brook key.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    [LoggerMessage(
        204,
        LogLevel.Debug,
        "Fire-and-forget effect {EffectType} completed for event {EventType} on brook {BrookKey} in {DurationMs}ms")]
    public static partial void FireAndForgetEffectCompleted(
        this ILogger logger,
        string effectType,
        string eventType,
        string brookKey,
        long durationMs
    );

    /// <summary>
    ///     Logs when a fire-and-forget effect is dispatched from the aggregate grain.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="eventType">The event type name.</param>
    /// <param name="brookKey">The brook key.</param>
    [LoggerMessage(
        206,
        LogLevel.Debug,
        "Dispatching fire-and-forget effect {EffectType} for event {EventType} on brook {BrookKey}")]
    public static partial void FireAndForgetEffectDispatched(
        this ILogger logger,
        string effectType,
        string eventType,
        string brookKey
    );

    /// <summary>
    ///     Logs when a fire-and-forget effect fails.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="eventType">The event type name.</param>
    /// <param name="brookKey">The brook key.</param>
    /// <param name="exception">The exception that occurred.</param>
    [LoggerMessage(
        205,
        LogLevel.Error,
        "Fire-and-forget effect {EffectType} failed for event {EventType} on brook {BrookKey}")]
    public static partial void FireAndForgetEffectFailed(
        this ILogger logger,
        string effectType,
        string eventType,
        string brookKey,
        Exception exception
    );
}