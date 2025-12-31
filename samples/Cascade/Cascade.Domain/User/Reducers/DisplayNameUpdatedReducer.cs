// <copyright file="DisplayNameUpdatedReducer.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.User.Reducers;

/// <summary>
///     Reducer for <see cref="DisplayNameUpdated" /> events.
/// </summary>
internal sealed class DisplayNameUpdatedReducer : Reducer<DisplayNameUpdated, UserState>
{
    /// <inheritdoc />
    protected override UserState ReduceCore(
        UserState state,
        DisplayNameUpdated @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            DisplayName = @event.NewDisplayName,
        };
    }
}
