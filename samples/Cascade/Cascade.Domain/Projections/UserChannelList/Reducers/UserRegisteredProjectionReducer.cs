// Copyright (c) Gibbs-Morris. All rights reserved.

using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.UserChannelList.Reducers;

/// <summary>
///     Reduces the <see cref="UserRegistered" /> event to produce an initial
///     <see cref="UserChannelListProjection" />.
/// </summary>
internal sealed class UserRegisteredProjectionReducer : Reducer<UserRegistered, UserChannelListProjection>
{
    /// <inheritdoc />
    protected override UserChannelListProjection ReduceCore(
        UserChannelListProjection state,
        UserRegistered eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return new()
        {
            UserId = eventData.UserId,
        };
    }
}