using System;

using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.AllChannels.Reducers;

/// <summary>
///     Reduces the <see cref="MemberAdded" /> event to update member count
///     in the <see cref="AllChannelsProjection" />.
/// </summary>
internal sealed class MemberAddedAllChannelsEventReducer : EventReducerBase<MemberAdded, AllChannelsProjection>
{
    /// <inheritdoc />
    protected override AllChannelsProjection ReduceCore(
        AllChannelsProjection state,
        MemberAdded eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);

        // MemberAdded doesn't include ChannelId, so we need to find the channel
        // This eventReducer runs in the context of a specific channel's brook
        // For cross-aggregate projections, we'd need event enrichment
        // For now, this eventReducer won't update member counts since MemberAdded lacks ChannelId
        // The initial count from ChannelCreated (1 for creator) is maintained

        // Return state unchanged - member count updates would require event enrichment
        return state;
    }
}