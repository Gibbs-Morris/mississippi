using System;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Reducer for <see cref="SagaFailed" />.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public sealed class SagaFailedReducer<TSaga> : EventReducerBase<SagaFailed, TSaga>
    where TSaga : class, ISagaState
{
    /// <inheritdoc />
    protected override TSaga ReduceCore(
        TSaga state,
        SagaFailed eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaStateMutator.CreateUpdated(
            state,
            (
                map,
                instance
            ) => map.SetProperty(instance, nameof(ISagaState.Phase), SagaPhase.Failed));
    }
}