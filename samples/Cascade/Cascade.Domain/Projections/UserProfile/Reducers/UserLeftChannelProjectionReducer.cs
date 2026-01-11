using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.UserProfile.Reducers;

/// <summary>
///     Reduces the <see cref="UserLeftChannel" /> event to remove a channel
///     from the <see cref="UserProfileProjection" />.
/// </summary>
internal sealed class UserLeftChannelProjectionReducer : ReducerBase<UserLeftChannel, UserProfileProjection>
{
    /// <inheritdoc />
    protected override UserProfileProjection ReduceCore(
        UserProfileProjection state,
        UserLeftChannel eventData
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(eventData);

        // Only process if channel exists
        if (!state.ChannelIds.Contains(eventData.ChannelId))
        {
            // Must return new instance per ReducerBase requirements
            return state with { };
        }

        return state with
        {
            ChannelIds = state.ChannelIds.Remove(eventData.ChannelId),
            ChannelCount = state.ChannelCount - 1,
        };
    }
}