using System;

using Cascade.Domain.Conversation.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.ChannelMessages.Reducers;

/// <summary>
///     Reduces the <see cref="ConversationStarted" /> event to produce an initial
///     <see cref="ChannelMessagesProjection" />.
/// </summary>
internal sealed class ConversationStartedProjectionReducer : ReducerBase<ConversationStarted, ChannelMessagesProjection>
{
    /// <inheritdoc />
    protected override ChannelMessagesProjection ReduceCore(
        ChannelMessagesProjection state,
        ConversationStarted eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return new()
        {
            ChannelId = eventData.ChannelId,
            MessageCount = 0,
        };
    }
}