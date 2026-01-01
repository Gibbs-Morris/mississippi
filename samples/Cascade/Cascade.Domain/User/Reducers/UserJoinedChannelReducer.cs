// <copyright file="UserJoinedChannelReducer.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.User.Reducers;

/// <summary>
///     Reducer for <see cref="UserJoinedChannel" /> events.
/// </summary>
internal sealed class UserJoinedChannelReducer : Reducer<UserJoinedChannel, UserState>
{
    /// <inheritdoc />
    protected override UserState ReduceCore(
        UserState state,
        UserJoinedChannel @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            ChannelIds = (state?.ChannelIds ?? []).Add(@event.ChannelId),
        };
    }
}