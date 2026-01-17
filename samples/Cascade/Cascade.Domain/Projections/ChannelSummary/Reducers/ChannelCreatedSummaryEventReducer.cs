using System;

using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.ChannelSummary.Reducers;

/// <summary>
///     Reduces the <see cref="ChannelCreated" /> event to initialize
///     the <see cref="ChannelSummaryProjection" />.
/// </summary>
internal sealed class ChannelCreatedSummaryEventReducer : EventReducerBase<ChannelCreated, ChannelSummaryProjection>
{
    /// <inheritdoc />
    protected override ChannelSummaryProjection ReduceCore(
        ChannelSummaryProjection state,
        ChannelCreated eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            ChannelId = eventData.ChannelId,
            Name = eventData.Name,
            CreatedAt = eventData.CreatedAt,
            CreatedBy = eventData.CreatedBy,
            MemberCount = 1, // Creator is automatically a member
            IsArchived = false,
        };
    }
}