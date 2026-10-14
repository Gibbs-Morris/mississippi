using System;

using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Events;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.State;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Reducers;

/// <summary>Applies a recorded removal immutably without consulting command policy or sources.</summary>
/// <typeparam name="TAggregate">The named aggregate snapshot type.</typeparam>
/// <typeparam name="TSource">The source projection type.</typeparam>
internal sealed class ReferenceRemovedReducer<TAggregate, TSource> : EventReducerBase<ReferenceRemoved, TAggregate>
    where TAggregate : class
{
    /// <summary>Initializes a new instance of the <see cref="ReferenceRemovedReducer{TAggregate, TSource}" /> class.</summary>
    /// <param name="stateAccess">The pure snapshot callbacks.</param>
    public ReferenceRemovedReducer(
        ReferenceSetStateAccess<TAggregate, TSource> stateAccess
    ) =>
        StateAccess = stateAccess;

    private ReferenceSetStateAccess<TAggregate, TSource> StateAccess { get; }

    /// <inheritdoc />
    protected override TAggregate ReduceCore(
        TAggregate state,
        ReferenceRemoved eventData
    )
    {
        ReferenceSetState<TSource> references = StateAccess.Read(state);
        return StateAccess.Write(
            state,
            references with
            {
                Members = references.Members.WithComparers(StringComparer.Ordinal).Remove(eventData.SourceId),
            });
    }
}