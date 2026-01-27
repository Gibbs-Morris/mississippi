using System;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Orleans;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Typed registration entry for fire-and-forget effects.
/// </summary>
/// <typeparam name="TEffect">The effect implementation type.</typeparam>
/// <typeparam name="TEvent">The event type the effect handles.</typeparam>
/// <typeparam name="TAggregate">The aggregate state type.</typeparam>
/// <remarks>
///     S2436 suppressed: Three type parameters are required by design - effect, event, and aggregate
///     types are all needed for compile-time type safety and correct DI resolution.
/// </remarks>
#pragma warning disable S2436 // Types should not have too many generic parameters
internal sealed class FireAndForgetEffectRegistration<TEffect, TEvent, TAggregate>
#pragma warning restore S2436
    : IFireAndForgetEffectRegistration<TAggregate>
    where TEffect : class, IFireAndForgetEventEffect<TEvent, TAggregate>
    where TEvent : class
    where TAggregate : class
{
    /// <inheritdoc />
    public string EffectTypeName { get; } = typeof(TEffect).FullName ?? typeof(TEffect).Name;

    /// <inheritdoc />
    public Type EventType { get; } = typeof(TEvent);

    /// <inheritdoc />
    public void Dispatch(
        IGrainFactory grainFactory,
        object eventData,
        TAggregate aggregateState,
        string brookKey,
        long eventPosition
    )
    {
        ArgumentNullException.ThrowIfNull(grainFactory);
        ArgumentNullException.ThrowIfNull(eventData);
        ArgumentNullException.ThrowIfNull(aggregateState);
        ArgumentNullException.ThrowIfNull(brookKey);
        if (eventData is not TEvent typedEvent)
        {
            return;
        }

        FireAndForgetEffectEnvelope<TEvent, TAggregate> envelope = new()
        {
            EventData = typedEvent,
            AggregateState = aggregateState,
            BrookKey = brookKey,
            EventPosition = eventPosition,
            EffectTypeName = EffectTypeName,
        };

        // Get the worker grain keyed by aggregate type name for routing
        string grainKey = typeof(TAggregate).FullName ?? typeof(TAggregate).Name;
        IFireAndForgetEffectWorkerGrain<TEvent, TAggregate> workerGrain =
            grainFactory.GetGrain<IFireAndForgetEffectWorkerGrain<TEvent, TAggregate>>(grainKey);

        // Fire-and-forget: do not await the [OneWay] call
        _ = workerGrain.ExecuteAsync(envelope);
    }
}