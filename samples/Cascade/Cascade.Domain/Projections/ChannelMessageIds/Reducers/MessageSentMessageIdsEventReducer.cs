using System;

using Cascade.Domain.Conversation.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.ChannelMessageIds.Reducers;

/// <summary>
///     Reduces the <see cref="MessageSent" /> event to add a message ID
///     to the <see cref="ChannelMessageIdsProjection" />.
/// </summary>
internal sealed class MessageSentMessageIdsEventReducer : EventReducerBase<MessageSent, ChannelMessageIdsProjection>
{
    /// <inheritdoc />
    protected override ChannelMessageIdsProjection ReduceCore(
        ChannelMessageIdsProjection state,
        MessageSent eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            MessageIds = state.MessageIds.Add(eventData.MessageId),
            TotalCount = state.TotalCount + 1,
        };
    }
}