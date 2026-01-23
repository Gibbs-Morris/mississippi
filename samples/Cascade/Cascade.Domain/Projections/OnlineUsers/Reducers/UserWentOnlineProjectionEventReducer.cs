using System;

using Cascade.Domain.Aggregates.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.OnlineUsers.Reducers;

/// <summary>
///     Reduces the <see cref="UserWentOnline" /> event to update the online status
///     in the <see cref="OnlineUsersProjection" />.
/// </summary>
internal sealed class UserWentOnlineProjectionEventReducer : EventReducerBase<UserWentOnline, OnlineUsersProjection>
{
    /// <inheritdoc />
    protected override OnlineUsersProjection ReduceCore(
        OnlineUsersProjection state,
        UserWentOnline eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            IsOnline = true,
            LastStatusChange = eventData.Timestamp,
        };
    }
}