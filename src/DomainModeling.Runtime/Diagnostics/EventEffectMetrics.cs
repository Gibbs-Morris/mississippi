using System.Diagnostics;
using System.Diagnostics.Metrics;


namespace Mississippi.EventSourcing.Aggregates.Diagnostics;

/// <summary>
///     OpenTelemetry metrics for event effect execution.
/// </summary>
internal static class EventEffectMetrics
{
    /// <summary>
    ///     The meter name for event effect metrics.
    /// </summary>
    internal const string MeterName = "Mississippi.EventSourcing.Aggregates.Effects";

    private const string AggregateTypeTag = "aggregate.type";

    private const string EffectTypeTag = "effect.type";

    private const string EventTypeTag = "event.type";

    private static readonly Meter EffectMeter = new(MeterName);

    private static readonly Counter<long> EffectCount = EffectMeter.CreateCounter<long>(
        "effect.execution.count",
        "executions",
        "Number of effect executions.");

    private static readonly Histogram<double> EffectDuration = EffectMeter.CreateHistogram<double>(
        "effect.execution.duration",
        "ms",
        "Time to execute an effect.");

    private static readonly Counter<long> EffectErrors = EffectMeter.CreateCounter<long>(
        "effect.execution.errors",
        "errors",
        "Number of effect failures.");

    private static readonly Counter<long> EffectEventsYielded = EffectMeter.CreateCounter<long>(
        "effect.events.yielded",
        "events",
        "Number of events yielded by effects.");

    private static readonly Counter<long> IterationLimitReached = EffectMeter.CreateCounter<long>(
        "effect.iteration.limit",
        "occurrences",
        "Number of times the effect iteration limit was reached.");

    private static readonly Counter<long> SlowEffects = EffectMeter.CreateCounter<long>(
        "effect.execution.slow",
        "occurrences",
        "Number of effects that took longer than 1 second.");

    /// <summary>
    ///     Records an effect failure.
    /// </summary>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="eventType">The event type name.</param>
    internal static void RecordEffectError(
        string aggregateType,
        string effectType,
        string eventType
    )
    {
        TagList tags = default;
        tags.Add(AggregateTypeTag, aggregateType);
        tags.Add(EffectTypeTag, effectType);
        tags.Add(EventTypeTag, eventType);
        EffectErrors.Add(1, tags);
    }

    /// <summary>
    ///     Records an effect execution.
    /// </summary>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="eventType">The event type name.</param>
    /// <param name="durationMs">The duration of the effect execution in milliseconds.</param>
    /// <param name="success">Whether the effect completed successfully.</param>
    internal static void RecordEffectExecution(
        string aggregateType,
        string effectType,
        string eventType,
        double durationMs,
        bool success
    )
    {
        TagList tags = default;
        tags.Add(AggregateTypeTag, aggregateType);
        tags.Add(EffectTypeTag, effectType);
        tags.Add(EventTypeTag, eventType);
        tags.Add("result", success ? "success" : "failure");
        EffectCount.Add(1, tags);
        EffectDuration.Record(durationMs, tags);
    }

    /// <summary>
    ///     Records that an effect yielded an event.
    /// </summary>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="yieldedEventType">The yielded event type name.</param>
    internal static void RecordEventYielded(
        string aggregateType,
        string effectType,
        string yieldedEventType
    )
    {
        TagList tags = default;
        tags.Add(AggregateTypeTag, aggregateType);
        tags.Add(EffectTypeTag, effectType);
        tags.Add("yielded.event.type", yieldedEventType);
        EffectEventsYielded.Add(1, tags);
    }

    /// <summary>
    ///     Records that an effect iteration limit was reached.
    /// </summary>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    internal static void RecordIterationLimitReached(
        string aggregateType,
        string aggregateKey
    )
    {
        TagList tags = default;
        tags.Add(AggregateTypeTag, aggregateType);
        tags.Add("aggregate.key", aggregateKey);
        IterationLimitReached.Add(1, tags);
    }

    /// <summary>
    ///     Records a slow effect execution (over 1 second).
    /// </summary>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="eventType">The event type name.</param>
    internal static void RecordSlowEffect(
        string aggregateType,
        string effectType,
        string eventType
    )
    {
        TagList tags = default;
        tags.Add(AggregateTypeTag, aggregateType);
        tags.Add(EffectTypeTag, effectType);
        tags.Add(EventTypeTag, eventType);
        SlowEffects.Add(1, tags);
    }
}