using System;

using Cascade.Domain.Aggregates.Conversation.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.ChannelMessages.Reducers;

/// <summary>
///     Reduces the <see cref="MessageSent" /> event to add a message
///     to the <see cref="ChannelMessagesProjection" />.
/// </summary>
internal sealed class MessageSentProjectionEventReducer : EventReducerBase<MessageSent, ChannelMessagesProjection>
{
    /// <inheritdoc />
    protected override ChannelMessagesProjection ReduceCore(
        ChannelMessagesProjection state,
        MessageSent eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        MessageItem message = new()
        {
            MessageId = eventData.MessageId,
            Content = eventData.Content,
            SentBy = eventData.SentBy,
            SentAt = eventData.SentAt,
            IsDeleted = false,
        };
        return state with
        {
            Messages = state.Messages.Add(message),
            MessageCount = state.MessageCount + 1,
        };
    }
}