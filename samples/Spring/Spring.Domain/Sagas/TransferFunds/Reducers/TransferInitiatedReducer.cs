using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Sagas.TransferFunds.Events;

namespace Spring.Domain.Sagas.TransferFunds.Reducers;

/// <summary>
///     Reducer for <see cref="TransferInitiated" /> events.
/// </summary>
internal sealed class TransferInitiatedReducer : EventReducerBase<TransferInitiated, TransferFundsSagaState>
{
    /// <inheritdoc />
    protected override TransferFundsSagaState ReduceCore(
        TransferFundsSagaState state,
        TransferInitiated @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return new TransferFundsSagaState
        {
            SourceAccountId = @event.SourceAccountId,
            DestinationAccountId = @event.DestinationAccountId,
            Amount = @event.Amount,
            InitiatedAt = @event.InitiatedAt,
            SourceDebited = false,
            DestinationCredited = false,
            SourceRefunded = false,
            FailureReason = null,
        };
    }
}
