using System;

using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.ChannelMemberList.Reducers;

/// <summary>
///     Reduces the <see cref="ChannelCreated" /> event to produce an initial
///     <see cref="ChannelMemberListProjection" />.
/// </summary>
internal sealed class ChannelCreatedProjectionReducer : Reducer<ChannelCreated, ChannelMemberListProjection>
{
    /// <inheritdoc />
    protected override ChannelMemberListProjection ReduceCore(
        ChannelMemberListProjection state,
        ChannelCreated eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);

        // The creator is automatically added as the first member
        MemberInfo creator = new()
        {
            UserId = eventData.CreatedBy,
            JoinedAt = eventData.CreatedAt,
        };
        return new()
        {
            ChannelId = eventData.ChannelId,
            ChannelName = eventData.Name,
            Members = [creator],
            MemberCount = 1,
        };
    }
}