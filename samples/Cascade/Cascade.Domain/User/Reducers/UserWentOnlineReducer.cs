// <copyright file="UserWentOnlineReducer.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.User.Reducers;

/// <summary>
///     Reducer for <see cref="UserWentOnline" /> events.
/// </summary>
internal sealed class UserWentOnlineReducer : Reducer<UserWentOnline, UserState>
{
    /// <inheritdoc />
    protected override UserState ReduceCore(
        UserState state,
        UserWentOnline @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            IsOnline = true,
            LastSeenAt = @event.Timestamp,
        };
    }
}
