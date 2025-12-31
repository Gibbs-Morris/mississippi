// <copyright file="UserWentOfflineReducer.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.User.Reducers;

/// <summary>
///     Reducer for <see cref="UserWentOffline" /> events.
/// </summary>
internal sealed class UserWentOfflineReducer : Reducer<UserWentOffline, UserState>
{
    /// <inheritdoc />
    protected override UserState ReduceCore(
        UserState state,
        UserWentOffline @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            IsOnline = false,
            LastSeenAt = @event.Timestamp,
        };
    }
}
