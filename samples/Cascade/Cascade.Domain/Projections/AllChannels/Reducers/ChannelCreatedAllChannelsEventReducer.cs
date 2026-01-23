using System;

using Cascade.Domain.Aggregates.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.AllChannels.Reducers;

/// <summary>
///     Reduces the <see cref="ChannelCreated" /> event to add a channel
///     to the <see cref="AllChannelsProjection" />.
/// </summary>
/// <remarks>
///     This event reducer is DEPRECATED. The new pattern uses AllChannelIdsProjection + ChannelSummaryProjection
///     for efficient viewport-based subscriptions.
/// </remarks>
internal sealed class ChannelCreatedAllChannelsEventReducer : EventReducerBase<ChannelCreated, AllChannelsProjection>
{
    /// <inheritdoc />
    protected override AllChannelsProjection ReduceCore(
        AllChannelsProjection state,
        ChannelCreated eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        EmbeddedChannelSummary summary = new()
        {
            ChannelId = eventData.ChannelId,
            Name = eventData.Name,
            CreatedAt = eventData.CreatedAt,
            CreatedBy = eventData.CreatedBy,
            IsArchived = false,
            MemberCount = 1, // Creator is automatically a member
        };
        return state with
        {
            Channels = state.Channels.SetItem(eventData.ChannelId, summary),
        };
    }
}