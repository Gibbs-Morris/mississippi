using System;

using Cascade.Domain.Aggregates.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.ChannelSummary.Reducers;

/// <summary>
///     Reduces the <see cref="MemberRemoved" /> event to decrement member count
///     in the <see cref="ChannelSummaryProjection" />.
/// </summary>
internal sealed class MemberRemovedSummaryEventReducer : EventReducerBase<MemberRemoved, ChannelSummaryProjection>
{
    /// <inheritdoc />
    protected override ChannelSummaryProjection ReduceCore(
        ChannelSummaryProjection state,
        MemberRemoved eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            MemberCount = Math.Max(0, state.MemberCount - 1),
        };
    }
}