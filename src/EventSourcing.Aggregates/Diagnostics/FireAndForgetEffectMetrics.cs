using System.Diagnostics;
using System.Diagnostics.Metrics;


namespace Mississippi.EventSourcing.Aggregates.Diagnostics;

/// <summary>
///     OpenTelemetry metrics for fire-and-forget event effect execution.
/// </summary>
internal static class FireAndForgetEffectMetrics
{
    /// <summary>
    ///     The meter name for fire-and-forget effect metrics.
    /// </summary>
    internal const string MeterName = "Mississippi.EventSourcing.Aggregates.FireAndForgetEffects";

    private const string AggregateTypeTag = "aggregate.type";

    private const string EffectTypeTag = "effect.type";

    private const string ErrorTypeTag = "error.type";

    private const string EventTypeTag = "event.type";

    private static readonly Meter EffectMeter = new(MeterName, "1.0.0");

    private static readonly Counter<long> DispatchCounter = EffectMeter.CreateCounter<long>(
        "fire_forget_effect.dispatched",
        "dispatches",
        "Number of fire-and-forget effects dispatched.");

    private static readonly Histogram<double> DurationHistogram = EffectMeter.CreateHistogram<double>(
        "fire_forget_effect.duration",
        "ms",
        "Duration of fire-and-forget effect execution.");

    private static readonly Counter<long> FailureCounter = EffectMeter.CreateCounter<long>(
        "fire_forget_effect.failure",
        "failures",
        "Number of fire-and-forget effects that failed.");

    private static readonly Counter<long> SuccessCounter = EffectMeter.CreateCounter<long>(
        "fire_forget_effect.success",
        "successes",
        "Number of fire-and-forget effects completed successfully.");

    /// <summary>
    ///     Records a fire-and-forget effect dispatch.
    /// </summary>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="eventType">The event type name.</param>
    /// <param name="effectType">The effect type name.</param>
    internal static void RecordDispatch(
        string aggregateType,
        string eventType,
        string effectType
    )
    {
        TagList tags = default;
        tags.Add(AggregateTypeTag, aggregateType);
        tags.Add(EventTypeTag, eventType);
        tags.Add(EffectTypeTag, effectType);
        DispatchCounter.Add(1, tags);
    }

    /// <summary>
    ///     Records the duration of a fire-and-forget effect execution.
    /// </summary>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="eventType">The event type name.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="success">Whether the effect completed successfully.</param>
    internal static void RecordDuration(
        string aggregateType,
        string eventType,
        string effectType,
        double durationMs,
        bool success
    )
    {
        TagList tags = default;
        tags.Add(AggregateTypeTag, aggregateType);
        tags.Add(EventTypeTag, eventType);
        tags.Add(EffectTypeTag, effectType);
        tags.Add("success", success);
        DurationHistogram.Record(durationMs, tags);
    }

    /// <summary>
    ///     Records a fire-and-forget effect failure.
    /// </summary>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="eventType">The event type name.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="errorType">The type of error that occurred.</param>
    internal static void RecordFailure(
        string aggregateType,
        string eventType,
        string effectType,
        string errorType
    )
    {
        TagList tags = default;
        tags.Add(AggregateTypeTag, aggregateType);
        tags.Add(EventTypeTag, eventType);
        tags.Add(EffectTypeTag, effectType);
        tags.Add(ErrorTypeTag, errorType);
        FailureCounter.Add(1, tags);
    }

    /// <summary>
    ///     Records a successful fire-and-forget effect execution.
    /// </summary>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="eventType">The event type name.</param>
    /// <param name="effectType">The effect type name.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    internal static void RecordSuccess(
        string aggregateType,
        string eventType,
        string effectType,
        double durationMs
    )
    {
        TagList tags = default;
        tags.Add(AggregateTypeTag, aggregateType);
        tags.Add(EventTypeTag, eventType);
        tags.Add(EffectTypeTag, effectType);
        SuccessCounter.Add(1, tags);
        RecordDuration(aggregateType, eventType, effectType, durationMs, true);
    }
}