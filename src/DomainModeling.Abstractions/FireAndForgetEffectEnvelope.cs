using Orleans;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Envelope containing all data needed to execute a fire-and-forget effect.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
/// <typeparam name="TAggregate">The aggregate state type.</typeparam>
/// <remarks>
///     <para>
///         This envelope is serialized and sent to the worker grain for execution.
///         It contains the event data, a snapshot of the aggregate state, and metadata
///         for logging and metrics.
///     </para>
///     <para>
///         The <see cref="EffectTypeName" /> property identifies which effect implementation
///         to resolve from DI when the worker grain processes this envelope.
///     </para>
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Aggregates.Abstractions.FireAndForgetEffectEnvelope`2")]
public sealed record FireAndForgetEffectEnvelope<TEvent, TAggregate>
    where TEvent : class
    where TAggregate : class
{
    /// <summary>
    ///     Gets the aggregate state at the time the event was persisted.
    /// </summary>
    [Id(1)]
    public TAggregate? AggregateState { get; init; }

    /// <summary>
    ///     Gets the brook key identifying the aggregate instance.
    /// </summary>
    [Id(2)]
    public string BrookKey { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the full type name of the effect to execute.
    /// </summary>
    /// <remarks>
    ///     This is used by the worker grain to resolve the correct effect implementation
    ///     from the dependency injection container.
    /// </remarks>
    [Id(4)]
    public string EffectTypeName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the event data that triggered this effect.
    /// </summary>
    [Id(0)]
    public TEvent? EventData { get; init; }

    /// <summary>
    ///     Gets the position of the event in the brook.
    /// </summary>
    [Id(3)]
    public long EventPosition { get; init; }
}