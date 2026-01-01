// <copyright file="UserRegisteredReducer.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.User.Reducers;

/// <summary>
///     Reducer for <see cref="UserRegistered" /> events.
/// </summary>
internal sealed class UserRegisteredReducer : Reducer<UserRegistered, UserAggregate>
{
    /// <inheritdoc />
    protected override UserAggregate ReduceCore(
        UserAggregate state,
        UserRegistered @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            IsRegistered = true,
            UserId = @event.UserId,
            DisplayName = @event.DisplayName,
            RegisteredAt = @event.RegisteredAt,
        };
    }
}