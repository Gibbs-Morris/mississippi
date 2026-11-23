using System;

using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Events;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.State;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Reducers;

/// <summary>Applies a recorded addition immutably without consulting command policy or sources.</summary>
/// <typeparam name="TAggregate">The named aggregate snapshot type.</typeparam>
/// <typeparam name="TSource">The source projection type.</typeparam>
internal sealed class ReferenceAddedReducer<TAggregate, TSource> : EventReducerBase<ReferenceAdded, TAggregate>
    where TAggregate : class
{
    /// <summary>Initializes a new instance of the <see cref="ReferenceAddedReducer{TAggregate, TSource}" /> class.</summary>
    /// <param name="stateAccess">The pure snapshot callbacks.</param>
    public ReferenceAddedReducer(
        ReferenceSetStateAccess<TAggregate, TSource> stateAccess
    ) =>
        StateAccess = stateAccess;

    private ReferenceSetStateAccess<TAggregate, TSource> StateAccess { get; }

    /// <inheritdoc />
    protected override TAggregate ReduceCore(
        TAggregate state,
        ReferenceAdded eventData
    )
    {
        ReferenceSetState<TSource> references = StateAccess.Read(state);
        return StateAccess.Write(
            state,
            references with
            {
                Members = references.Members.WithComparers(StringComparer.Ordinal)
                    .SetItem(eventData.SourceId, eventData.ObservedSourceVersion),
            });
    }
}