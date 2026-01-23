using Cascade.Domain.Aggregates.Conversation.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Aggregates.Conversation.Reducers;

/// <summary>
///     Reduces the <see cref="MessageSent" /> event to produce a new <see cref="ConversationAggregate" />.
/// </summary>
internal sealed class MessageSentEventReducer : EventReducerBase<MessageSent, ConversationAggregate>
{
    /// <inheritdoc />
    protected override ConversationAggregate ReduceCore(
        ConversationAggregate state,
        MessageSent eventData
    ) =>
        state with
        {
            Messages = state.Messages.Add(
                new()
                {
                    MessageId = eventData.MessageId,
                    Content = eventData.Content,
                    SentBy = eventData.SentBy,
                    SentAt = eventData.SentAt,
                }),
        };
}