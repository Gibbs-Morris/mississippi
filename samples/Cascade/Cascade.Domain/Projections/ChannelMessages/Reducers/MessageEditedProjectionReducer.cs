using System;

using Cascade.Domain.Conversation.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.ChannelMessages.Reducers;

/// <summary>
///     Reduces the <see cref="MessageEdited" /> event to update a message
///     in the <see cref="ChannelMessagesProjection" />.
/// </summary>
internal sealed class MessageEditedProjectionReducer : ReducerBase<MessageEdited, ChannelMessagesProjection>
{
    /// <inheritdoc />
    protected override ChannelMessagesProjection ReduceCore(
        ChannelMessagesProjection state,
        MessageEdited eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        int index = state.Messages.FindIndex(m => m.MessageId == eventData.MessageId);
        if (index < 0)
        {
            return state;
        }

        MessageItem existing = state.Messages[index];
        MessageItem updated = existing with
        {
            Content = eventData.NewContent,
            EditedAt = eventData.EditedAt,
        };
        return state with
        {
            Messages = state.Messages.SetItem(index, updated),
        };
    }
}