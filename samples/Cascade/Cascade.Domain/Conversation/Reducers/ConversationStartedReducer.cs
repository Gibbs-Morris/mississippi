using System.Collections.Immutable;

using Cascade.Domain.Conversation.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Conversation.Reducers;

/// <summary>
///     Reduces the <see cref="ConversationStarted" /> event to produce a new <see cref="ConversationAggregate" />.
/// </summary>
internal sealed class ConversationStartedReducer : ReducerBase<ConversationStarted, ConversationAggregate>
{
    /// <inheritdoc />
    protected override ConversationAggregate ReduceCore(
        ConversationAggregate state,
        ConversationStarted eventData
    ) =>
        new()
        {
            IsStarted = true,
            ConversationId = eventData.ConversationId,
            ChannelId = eventData.ChannelId,
            StartedAt = eventData.StartedAt,
            Messages = ImmutableList<Message>.Empty,
        };
}