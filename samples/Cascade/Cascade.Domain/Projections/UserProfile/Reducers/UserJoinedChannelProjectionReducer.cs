using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.UserProfile.Reducers;

/// <summary>
///     Reduces the <see cref="UserJoinedChannel" /> event to add a channel
///     to the <see cref="UserProfileProjection" />.
/// </summary>
internal sealed class UserJoinedChannelProjectionReducer : ReducerBase<UserJoinedChannel, UserProfileProjection>
{
    /// <inheritdoc />
    protected override UserProfileProjection ReduceCore(
        UserProfileProjection state,
        UserJoinedChannel eventData
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(eventData);

        // Prevent duplicate channel IDs
        if (state.ChannelIds.Contains(eventData.ChannelId))
        {
            // Must return new instance per ReducerBase requirements
            return state with { };
        }

        return state with
        {
            ChannelIds = state.ChannelIds.Add(eventData.ChannelId),
            ChannelCount = state.ChannelCount + 1,
        };
    }
}