using System;

using Orleans;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Registration entry for fire-and-forget effects, enabling typed dispatch from aggregates.
/// </summary>
/// <remarks>
///     <para>
///         This interface provides the type information needed to dispatch fire-and-forget
///         effects from the aggregate grain without requiring the grain to know the specific
///         effect or event types at compile time.
///     </para>
///     <para>
///         Implementations are registered in DI via
///         <c>AddFireAndForgetEventEffect&lt;TEffect, TEvent, TAggregate&gt;()</c> and discovered
///         by the aggregate grain to dispatch effects after events are persisted.
///     </para>
/// </remarks>
/// <typeparam name="TAggregate">The aggregate state type.</typeparam>
public interface IFireAndForgetEffectRegistration<in TAggregate>
    where TAggregate : class
{
    /// <summary>
    ///     Gets the full type name of the effect implementation.
    /// </summary>
    string EffectTypeName { get; }

    /// <summary>
    ///     Gets the event type this registration handles.
    /// </summary>
    Type EventType { get; }

    /// <summary>
    ///     Dispatches the effect to the worker grain.
    /// </summary>
    /// <param name="grainFactory">The grain factory for resolving the worker grain.</param>
    /// <param name="eventData">The event that triggered the effect.</param>
    /// <param name="aggregateState">The aggregate state after the event was applied.</param>
    /// <param name="brookKey">The brook key identifying the aggregate instance.</param>
    /// <param name="eventPosition">The position of the event in the brook.</param>
    void Dispatch(
        IGrainFactory grainFactory,
        object eventData,
        TAggregate aggregateState,
        string brookKey,
        long eventPosition
    );
}