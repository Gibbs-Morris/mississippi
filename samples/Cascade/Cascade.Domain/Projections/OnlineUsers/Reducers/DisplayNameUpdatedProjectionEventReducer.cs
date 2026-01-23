using System;

using Cascade.Domain.Aggregates.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.OnlineUsers.Reducers;

/// <summary>
///     Reduces the <see cref="DisplayNameUpdated" /> event to update the display name
///     in the <see cref="OnlineUsersProjection" />.
/// </summary>
internal sealed class DisplayNameUpdatedProjectionEventReducer
    : EventReducerBase<DisplayNameUpdated, OnlineUsersProjection>
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