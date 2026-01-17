using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.OnlineUsers.Reducers;

/// <summary>
///     Reduces the <see cref="UserWentOffline" /> event to update the online status
///     in the <see cref="OnlineUsersProjection" />.
/// </summary>
internal sealed class UserWentOfflineProjectionEventReducer : EventReducerBase<UserWentOffline, OnlineUsersProjection>
{
    /// <inheritdoc />
    protected override OnlineUsersProjection ReduceCore(
        OnlineUsersProjection state,
        UserWentOffline eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            IsOnline = false,
            LastStatusChange = eventData.Timestamp,
        };
    }
}