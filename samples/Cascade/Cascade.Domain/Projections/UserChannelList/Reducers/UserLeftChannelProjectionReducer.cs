// Copyright (c) Gibbs-Morris. All rights reserved.

using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.UserChannelList.Reducers;

/// <summary>
///     Reduces the <see cref="UserLeftChannel" /> event to remove a channel
///     from the <see cref="UserChannelListProjection" />.
/// </summary>
internal sealed class UserLeftChannelProjectionReducer : Reducer<UserLeftChannel, UserChannelListProjection>
{
    /// <inheritdoc />
    protected override UserChannelListProjection ReduceCore(
        UserChannelListProjection state,
        UserLeftChannel eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            Channels = state.Channels.RemoveAll(c => c.ChannelId == eventData.ChannelId),
        };
    }
}