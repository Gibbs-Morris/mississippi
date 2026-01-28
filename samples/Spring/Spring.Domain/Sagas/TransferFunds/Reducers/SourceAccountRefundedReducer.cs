using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Sagas.TransferFunds.Events;


namespace Spring.Domain.Sagas.TransferFunds.Reducers;

/// <summary>
///     Reducer for <see cref="SourceAccountRefunded" /> events.
/// </summary>
internal sealed class SourceAccountRefundedReducer : EventReducerBase<SourceAccountRefunded, TransferFundsSagaState>
{
    /// <inheritdoc />
    protected override TransferFundsSagaState ReduceCore(
        TransferFundsSagaState state,
        SourceAccountRefunded @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(state);
        return state with
        {
            SourceRefunded = true,
            FailureReason = @event.Reason,
        };
    }
}