using System;

using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.ChannelMemberList.Reducers;

/// <summary>
///     Reduces the <see cref="MemberAdded" /> event to add a member
///     to the <see cref="ChannelMemberListProjection" />.
/// </summary>
internal sealed class MemberAddedProjectionReducer : ReducerBase<MemberAdded, ChannelMemberListProjection>
{
    /// <inheritdoc />
    protected override ChannelMemberListProjection ReduceCore(
        ChannelMemberListProjection state,
        MemberAdded eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        MemberInfo member = new()
        {
            UserId = eventData.UserId,
            JoinedAt = eventData.AddedAt,
        };
        return state with
        {
            Members = state.Members.Add(member),
            MemberCount = state.MemberCount + 1,
        };
    }
}