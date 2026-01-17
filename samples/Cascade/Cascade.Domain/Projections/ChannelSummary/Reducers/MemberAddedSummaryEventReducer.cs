using System;

using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.ChannelSummary.Reducers;

/// <summary>
///     Reduces the <see cref="MemberAdded" /> event to increment member count
///     in the <see cref="ChannelSummaryProjection" />.
/// </summary>
internal sealed class MemberAddedSummaryEventReducer : EventReducerBase<MemberAdded, ChannelSummaryProjection>
{
    /// <inheritdoc />
    protected override ChannelSummaryProjection ReduceCore(
        ChannelSummaryProjection state,
        MemberAdded eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            MemberCount = state.MemberCount + 1,
        };
    }
}