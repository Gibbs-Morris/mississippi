using System;

using Cascade.Domain.Aggregates.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.UserProfile.Reducers;

/// <summary>
///     Reduces the <see cref="UserWentOffline" /> event to update the online status
///     in the <see cref="UserProfileProjection" />.
/// </summary>
internal sealed class UserWentOfflineProjectionEventReducer : EventReducerBase<UserWentOffline, UserProfileProjection>
{
    /// <inheritdoc />
    protected override UserProfileProjection ReduceCore(
        UserProfileProjection state,
        UserWentOffline eventData
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            IsOnline = false,
        };
    }
}