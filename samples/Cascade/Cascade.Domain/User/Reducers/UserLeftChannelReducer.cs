// <copyright file="UserLeftChannelReducer.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.User.Reducers;

/// <summary>
///     Reducer for <see cref="UserLeftChannel" /> events.
/// </summary>
internal sealed class UserLeftChannelReducer : Reducer<UserLeftChannel, UserAggregate>
{
    /// <inheritdoc />
    protected override UserAggregate ReduceCore(
        UserAggregate state,
        UserLeftChannel @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            ChannelIds = (state?.ChannelIds ?? []).Remove(@event.ChannelId),
        };
    }
}