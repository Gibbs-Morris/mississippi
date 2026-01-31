using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Sagas.TransferFunds.Events;


namespace Spring.Domain.Sagas.TransferFunds.Reducers;

/// <summary>
///     Reducer for <see cref="SourceDebited" /> events.
/// </summary>
/// <remarks>
///     <para>
///         Sets <see cref="TransferFundsSagaState.SourceDebited" /> to <c>true</c>
///         when a <see cref="SourceDebited" /> event is applied.
///     </para>
/// </remarks>
internal sealed class SourceDebitedReducer : EventReducerBase<SourceDebited, TransferFundsSagaState>
{
    /// <inheritdoc />
    protected override TransferFundsSagaState ReduceCore(
        TransferFundsSagaState state,
        SourceDebited @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            SourceDebited = true,
        };
    }
}
