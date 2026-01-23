using System;

using Cascade.Domain.Aggregates.Conversation.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.Message.Reducers;

/// <summary>
///     Reduces the <see cref="MessageDeleted" /> event to mark
///     the <see cref="MessageProjection" /> as deleted.
/// </summary>
internal sealed class MessageDeletedEventReducer : EventReducerBase<MessageDeleted, MessageProjection>
{
    /// <inheritdoc />
    protected override MessageProjection ReduceCore(
        MessageProjection state,
        MessageDeleted eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);

        // Only apply if this is our message
        if (state.MessageId != eventData.MessageId)
        {
            return state;
        }

        return state with
        {
            IsDeleted = true,
        };
    }
}