using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Sagas.TransferFunds.Events;


namespace Spring.Domain.Sagas.TransferFunds.Reducers;

/// <summary>
///     Reducer for <see cref="TransferCompleted" /> events.
/// </summary>
/// <remarks>
///     <para>
///         This reducer handles the final <see cref="TransferCompleted" /> event.
///         The saga state is not modified beyond this point as the transfer is complete.
///     </para>
/// </remarks>
internal sealed class TransferCompletedReducer : EventReducerBase<TransferCompleted, TransferFundsSagaState>
{
    /// <inheritdoc />
    protected override TransferFundsSagaState ReduceCore(
        TransferFundsSagaState state,
        TransferCompleted @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);

        // Transfer is complete - no additional state changes needed
        // The saga infrastructure handles phase transitions
        return state ?? new();
    }
}
