using System;

using Cascade.Domain.Aggregates.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.OnlineUsers.Reducers;

/// <summary>
///     Reduces the <see cref="UserRegistered" /> event to produce an initial
///     <see cref="OnlineUsersProjection" />.
/// </summary>
internal sealed class UserRegisteredProjectionEventReducer : EventReducerBase<UserRegistered, OnlineUsersProjection>
{
    /// <inheritdoc />
    protected override OnlineUsersProjection ReduceCore(
        OnlineUsersProjection state,
        UserRegistered eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return new()
        {
            UserId = eventData.UserId,
            DisplayName = eventData.DisplayName,
            IsOnline = false,
            LastStatusChange = null,
        };
    }
}