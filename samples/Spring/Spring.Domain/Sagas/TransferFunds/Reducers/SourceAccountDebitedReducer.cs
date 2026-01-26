using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Sagas.TransferFunds.Events;

namespace Spring.Domain.Sagas.TransferFunds.Reducers;

/// <summary>
///     Reducer for <see cref="SourceAccountDebited" /> events.
/// </summary>
internal sealed class SourceAccountDebitedReducer : EventReducerBase<SourceAccountDebited, TransferFundsSagaState>
{
    /// <inheritdoc />
    protected override TransferFundsSagaState ReduceCore(
        TransferFundsSagaState state,
        SourceAccountDebited @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(state);
        return state with
        {
            SourceDebited = true,
        };
    }
}
