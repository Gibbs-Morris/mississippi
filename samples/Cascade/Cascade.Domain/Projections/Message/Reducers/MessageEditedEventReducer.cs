using System;

using Cascade.Domain.Conversation.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.Message.Reducers;

/// <summary>
///     Reduces the <see cref="MessageEdited" /> event to update
///     the <see cref="MessageProjection" />.
/// </summary>
internal sealed class MessageEditedEventReducer : EventReducerBase<MessageEdited, MessageProjection>
{
    /// <inheritdoc />
    protected override MessageProjection ReduceCore(
        MessageProjection state,
        MessageEdited eventData
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
            Content = eventData.NewContent,
            EditedAt = eventData.EditedAt,
        };
    }
}