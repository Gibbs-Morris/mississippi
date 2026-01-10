using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.UserProfile.Reducers;

/// <summary>
///     Reduces the <see cref="UserRegistered" /> event to produce an initial
///     <see cref="UserProfileProjection" />.
/// </summary>
internal sealed class UserRegisteredProjectionReducer : ReducerBase<UserRegistered, UserProfileProjection>
{
    /// <inheritdoc />
    protected override UserProfileProjection ReduceCore(
        UserProfileProjection state,
        UserRegistered eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return new()
        {
            UserId = eventData.UserId,
            DisplayName = eventData.DisplayName,
            IsOnline = false,
            RegisteredAt = eventData.RegisteredAt,
            LastOnlineAt = null,
            ChannelCount = 0,
        };
    }
}