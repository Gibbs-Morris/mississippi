using System;

using Cascade.Domain.Aggregates.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.AllChannelIds.Reducers;

/// <summary>
///     Reduces the <see cref="ChannelCreated" /> event to add a channel ID
///     to the <see cref="AllChannelIdsProjection" />.
/// </summary>
internal sealed class ChannelCreatedIdsEventReducer : EventReducerBase<ChannelCreated, AllChannelIdsProjection>
{
    /// <inheritdoc />
    protected override AllChannelIdsProjection ReduceCore(
        AllChannelIdsProjection state,
        ChannelCreated eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            ChannelIds = state.ChannelIds.Add(eventData.ChannelId),
            TotalCount = state.TotalCount + 1,
        };
    }
}