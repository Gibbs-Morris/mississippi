// Copyright (c) Gibbs-Morris. All rights reserved.

using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.UserChannelList.Reducers;

/// <summary>
///     Reduces the <see cref="UserJoinedChannel" /> event to add a channel
///     to the <see cref="UserChannelListProjection" />.
/// </summary>
internal sealed class UserJoinedChannelProjectionReducer : Reducer<UserJoinedChannel, UserChannelListProjection>
{
    /// <inheritdoc />
    protected override UserChannelListProjection ReduceCore(
        UserChannelListProjection state,
        UserJoinedChannel eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        ChannelMembership membership = new()
        {
            ChannelId = eventData.ChannelId,
            JoinedAt = eventData.JoinedAt,
        };
        return state with
        {
            Channels = state.Channels.Add(membership),
        };
    }
}