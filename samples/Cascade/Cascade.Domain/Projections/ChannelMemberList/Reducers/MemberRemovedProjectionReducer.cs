using System;

using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.ChannelMemberList.Reducers;

/// <summary>
///     Reduces the <see cref="MemberRemoved" /> event to remove a member
///     from the <see cref="ChannelMemberListProjection" />.
/// </summary>
internal sealed class MemberRemovedProjectionReducer : Reducer<MemberRemoved, ChannelMemberListProjection>
{
    /// <inheritdoc />
    protected override ChannelMemberListProjection ReduceCore(
        ChannelMemberListProjection state,
        MemberRemoved eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            Members = state.Members.RemoveAll(m => m.UserId == eventData.UserId),
            MemberCount = state.MemberCount - 1,
        };
    }
}