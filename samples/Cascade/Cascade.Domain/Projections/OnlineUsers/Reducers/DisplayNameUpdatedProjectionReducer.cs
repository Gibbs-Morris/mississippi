// Copyright (c) Gibbs-Morris. All rights reserved.

using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.OnlineUsers.Reducers;

/// <summary>
///     Reduces the <see cref="DisplayNameUpdated" /> event to update the display name
///     in the <see cref="OnlineUsersProjection" />.
/// </summary>
internal sealed class DisplayNameUpdatedProjectionReducer : Reducer<DisplayNameUpdated, OnlineUsersProjection>
{
    /// <inheritdoc />
    protected override OnlineUsersProjection ReduceCore(
        OnlineUsersProjection state,
        DisplayNameUpdated eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            DisplayName = eventData.NewDisplayName,
        };
    }
}