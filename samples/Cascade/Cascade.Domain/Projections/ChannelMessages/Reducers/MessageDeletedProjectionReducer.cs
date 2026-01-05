using System;

using Cascade.Domain.Conversation.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.ChannelMessages.Reducers;

/// <summary>
///     Reduces the <see cref="MessageDeleted" /> event to mark a message as deleted
///     in the <see cref="ChannelMessagesProjection" />.
/// </summary>
internal sealed class MessageDeletedProjectionReducer : Reducer<MessageDeleted, ChannelMessagesProjection>
{
    /// <inheritdoc />
    protected override ChannelMessagesProjection ReduceCore(
        ChannelMessagesProjection state,
        MessageDeleted eventData
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
            IsDeleted = true,
        };
        return state with
        {
            Messages = state.Messages.SetItem(index, updated),
        };
    }
}