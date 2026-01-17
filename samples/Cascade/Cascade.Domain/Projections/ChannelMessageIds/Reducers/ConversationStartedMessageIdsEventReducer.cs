using System;

using Cascade.Domain.Conversation.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.ChannelMessageIds.Reducers;

/// <summary>
///     Reduces the <see cref="ConversationStarted" /> event to initialize
///     the <see cref="ChannelMessageIdsProjection" />.
/// </summary>
internal sealed class ConversationStartedMessageIdsEventReducer
    : EventReducerBase<ConversationStarted, ChannelMessageIdsProjection>
{
    /// <inheritdoc />
    protected override ChannelMessageIdsProjection ReduceCore(
        ChannelMessageIdsProjection state,
        ConversationStarted eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            ChannelId = eventData.ChannelId,
        };
    }
}