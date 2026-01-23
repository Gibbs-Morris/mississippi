using System;

using Cascade.Domain.Aggregates.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.UserProfile.Reducers;

/// <summary>
///     Reduces the <see cref="UserWentOnline" /> event to update the online status
///     in the <see cref="UserProfileProjection" />.
/// </summary>
internal sealed class UserWentOnlineProjectionEventReducer : EventReducerBase<UserWentOnline, UserProfileProjection>
{
    /// <inheritdoc />
    protected override UserProfileProjection ReduceCore(
        UserProfileProjection state,
        UserWentOnline eventData
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            IsOnline = true,
            LastOnlineAt = eventData.Timestamp,
        };
    }
}