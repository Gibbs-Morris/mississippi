using System;

using Cascade.Domain.Conversation.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.Message.Reducers;

/// <summary>
///     Reduces the <see cref="MessageSent" /> event to create
///     the <see cref="MessageProjection" />.
/// </summary>
internal sealed class MessageSentEventReducer : EventReducerBase<MessageSent, MessageProjection>
{
    /// <inheritdoc />
    protected override MessageProjection ReduceCore(
        MessageProjection state,
        MessageSent eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            MessageId = eventData.MessageId,
            Content = eventData.Content,
            SentBy = eventData.SentBy,
            SentAt = eventData.SentAt,
        };
    }
}